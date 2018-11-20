namespace DataTanker.AccessMethods.RadixTree.Storage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Diagnostics;
    using System.Linq;

    using BinaryFormat.Page;
    using PageManagement;
    using MemoryManagement;

    /// <summary>
    /// Implementation of INodeStorage. 
    /// Instances of this class store nodes using specified object that implements IPageManager.
    /// </summary>
    internal class RadixTreeNodeStorage : INodeStorage
    {
        private readonly IPageManager _pageManager;
        private DbItemReference _rootNodeReference;
        private long _rootPageIndex;

        private readonly LinkedList<long> _recentPages = new LinkedList<long>();
        private const int _maxRecentPages = 6;

        // this is used to reduce node reallocations
        private static readonly short _gapSize = (short) ((DbItemReference.BytesLength + 2) * 2);

        internal static short GetNodeSize(short prefixLength, int entryCount)
        {
            if(entryCount < 0 || entryCount > 256) throw new ArgumentOutOfRangeException(nameof(entryCount));

            var size =
             (short)(PageFormatter.OnPagePointerSize + // overall size
                     PageFormatter.OnPagePointerSize + // prefix size
                     prefixLength +                    // prefix itself   
                     DbItemReference.BytesLength +     // value reference
                     DbItemReference.BytesLength +     // parent node reference
                     2 +                               // entryCount
                     entryCount * (DbItemReference.BytesLength + 1)); // child node reference + symbol

            size += (short)(_gapSize - (size % _gapSize));
            return size;
        }

        private byte[] GetNodeBytes(IRadixTreeNode node, short minimalSize)
        {
            var prefix = node.Prefix ?? new byte[0];
            var prefixLength = (short) prefix.Length;
            var valueReference = node.ValueReference ?? DbItemReference.Null;
            var parentReference = node.ParentNodeReference ?? DbItemReference.Null;

            var size = GetNodeSize(prefixLength, node.Entries.Count);

            var result = new byte[Math.Max(size, minimalSize)];

            using (var ms = new MemoryStream(result))
            {
                ms.Write(BitConverter.GetBytes(size), 0, PageFormatter.OnPagePointerSize);
                ms.Write(BitConverter.GetBytes(prefixLength), 0, PageFormatter.OnPagePointerSize);
                ms.Write(prefix, 0, prefixLength);
                valueReference.Write(ms);
                parentReference.Write(ms);
                ms.Write(BitConverter.GetBytes((short)node.Entries.Count), 0, sizeof(short));

                foreach (var entry in node.Entries)
                {
                    ms.WriteByte(entry.Key);
                    ms.Write(entry.Value.GetBytes(), 0, DbItemReference.BytesLength);
                }

                return ms.ToArray();
            }
        }

        private IRadixTreeNode NodeFromBytes(byte[] nodeBytes)
        {
            var result = new RadixTreeNode(0);
            using (var ms = new MemoryStream(nodeBytes, false))
            {
                var buffer = new byte[PageFormatter.OnPagePointerSize];

                ms.Read(buffer, 0, PageFormatter.OnPagePointerSize); // overall size

                ms.Read(buffer, 0, PageFormatter.OnPagePointerSize); // prefix length
                short prefixLength = BitConverter.ToInt16(buffer, 0);
                var prefix = new byte[prefixLength];
                ms.Read(prefix, 0, prefixLength); // prefix itself
                result.Prefix = prefix;
                result.ValueReference = DbItemReference.Read(ms);
                result.ParentNodeReference = DbItemReference.Read(ms);

                if (DbItemReference.IsNull(result.ValueReference)) result.ValueReference = null;
                if (DbItemReference.IsNull(result.ParentNodeReference)) result.ParentNodeReference = null;

                ms.Read(buffer, 0, sizeof(short)); 
                var nodeCount = BitConverter.ToInt16(buffer, 0);
                for (int i = 0; i < nodeCount; i++)
                {
                    var key = (byte) ms.ReadByte();
                    var value = DbItemReference.Read(ms);
                    result.Entries.Add(new KeyValuePair<byte, DbItemReference>(key, value));
                }

                return result;
            }
        }

        public int MaxPrefixLength 
        {
            get
            {
                int remainingSize = _pageManager.PageSize -
                                    GetNodeSize(0, 256) -
                                    RadixTreeNodesPageHeader.RadixTreeNodesHeaderLength -
                                    PageFormatter.OnPagePointerSize * 2;
                return remainingSize - (remainingSize % _gapSize);

            }
        }

        public IRadixTreeNode FetchRoot()
        {
            CheckRoot();
            return Fetch(_rootNodeReference);
        }

        public bool Update(IRadixTreeNode node)
        {
            var page = _pageManager.FetchPage(node.Reference.PageIndex);

            if (page.BackingObject == null)
            {
                var obj = RadixTreePageBackingObject.FromPage(page);
                page = new Page(_pageManager, node.Reference.PageIndex, () => Serialize(obj), obj);
            }

            var backingObject = (RadixTreePageBackingObject) page.BackingObject;
            var oldNodeSize = backingObject.GetNodeSize(node.Reference.ItemIndex);

            var newNodeSize = GetNodeSize((short)node.Prefix.Length, node.Entries.Count);

            var oldObjectSize = backingObject.Size;

            var newObjectSize = oldObjectSize + newNodeSize - oldNodeSize;
            if (newObjectSize > _pageManager.PageSize)
                return false;

            backingObject.Items[node.Reference.ItemIndex] = node;

            _pageManager.UpdatePage(page);
            return true;
        }

        private byte[] Serialize(RadixTreePageBackingObject backingObject)
        {
            var header = new RadixTreeNodesPageHeader();

            var page = new Page(_pageManager, backingObject.PageIndex, new byte[_pageManager.PageSize]);

            var items = backingObject.Items.Select(item =>
                                                       {
                                                           if (item is byte[] bytes) return bytes;
                                                           if (item == null) return new byte[0];
                                                           return GetNodeBytes((IRadixTreeNode) item, 0);
                                                       }).ToList();

            PageFormatter.FormatVariableSizeItemsPage(page, header, items);

            return page.Content;
        }

        private void UpdateRecentPage(long index)
        {
            if(index == _rootPageIndex) // the root page contains root node only, so don't use it in recent pages
                return;

            if (_recentPages.Contains(index))
            {
                _recentPages.Remove(index);
            }
            else
            {
                if (_recentPages.Count == _maxRecentPages)
                    _recentPages.RemoveLast();
            }

            _recentPages.AddFirst(index);
        }

        public IRadixTreeNode Fetch(DbItemReference reference)
        {
            if (DbItemReference.IsNull(reference))
                return null;

            var page = _pageManager.FetchPage(reference.PageIndex);

            if (page.BackingObject == null)
            {
                var obj1 = RadixTreePageBackingObject.FromPage(page);
                page = new Page(_pageManager, page.Index, () => Serialize(obj1), obj1);
            }

            var backingObject = (RadixTreePageBackingObject)page.BackingObject;

            var obj = backingObject.Items[reference.ItemIndex];

            if (!(obj is IRadixTreeNode result))
            {
                result = NodeFromBytes((byte[]) obj);
                result.Reference = (DbItemReference)reference.Clone();
                backingObject.Items[reference.ItemIndex] = result;
            }

            return result;
        }

        public IRadixTreeNode Create(int prefixSize, int childCapacity)
        {
            CheckRoot();

            IPage page;
            short itemIndex;
            var node = NewNode(DbItemReference.Null);

            foreach (var pageIndex in _recentPages)
            {
                page = _pageManager.FetchPage(pageIndex);

                if (page.BackingObject == null)
                {
                    var obj = RadixTreePageBackingObject.FromPage(page);
                    page = new Page(_pageManager, pageIndex, () => Serialize(obj), obj);
                }

                var backingObject = (RadixTreePageBackingObject)page.BackingObject;

                itemIndex = AddNode(backingObject, node, prefixSize, childCapacity);

                if (itemIndex != -1)
                {
                    UpdateRecentPage(page.Index);
                    node.Reference = new DbItemReference(page.Index, itemIndex);
                    _pageManager.UpdatePage(page);
                    return node;
                }
            }

            page = _pageManager.CreatePage();
            var obj1 = new RadixTreePageBackingObject(page.Index);
            long index1 = page.Index;
            page = new Page(_pageManager, index1, () => Serialize(obj1), obj1);

            itemIndex = AddNode((RadixTreePageBackingObject)page.BackingObject, node, prefixSize, childCapacity);
            if (itemIndex != -1)
            {
                UpdateRecentPage(page.Index);
                node.Reference = new DbItemReference(page.Index, itemIndex);
                _pageManager.UpdatePage(page);
                return node;
            }

            Debug.Fail("Should never get here");
            return null;
        }

        public short AddNode(RadixTreePageBackingObject backingObject, IRadixTreeNode node, int prefixSize, int childCapacity)
        {
            var size = backingObject.Size;
            var nodeSize = GetNodeSize((short)prefixSize, childCapacity);

            if (size + nodeSize > _pageManager.PageSize)
                return -1;

            for (short i = 0; i < backingObject.Items.Count; i++)
            {
                if (backingObject.Items[i] == null)
                {
                    backingObject.Items[i] = node;
                    return i;
                }
            }

            if (size + nodeSize + PageFormatter.OnPagePointerSize > _pageManager.PageSize)
                return -1;

            backingObject.Items.Add(node);

            return (short) (backingObject.Items.Count - 1);
        }

        private void CheckRoot()
        {
            if (_rootNodeReference == null)
            {
                var headerPage = _pageManager.FetchPage(0);

                _rootPageIndex = ((HeadingPageHeader) PageFormatter.GetPageHeader(headerPage)).AccessMethodPageIndex;
                var page = _pageManager.FetchPage(_rootPageIndex);

                var items = PageFormatter.ReadVariableSizeItems(page);
                if (items.Any())
                {
                    // root page already has an item
                    _rootNodeReference = new DbItemReference(_rootPageIndex, 0);
                }
                else
                {
                    var node = new RadixTreeNode(256);
                    PageFormatter.AddVariableSizeItem(page, GetNodeBytes(node, GetNodeSize(0, 256)));
                    _rootNodeReference = new DbItemReference(_rootPageIndex, 0);
                    _pageManager.UpdatePage(page);
                }
            }
        }

        public void Remove(DbItemReference reference)
        {
            if (_rootNodeReference.Equals(reference))
                throw new ArgumentOutOfRangeException(nameof(reference), "Unable to delete the root node");

            var page = _pageManager.FetchPage(reference.PageIndex);

            if (page.BackingObject == null)
            {
                var obj = RadixTreePageBackingObject.FromPage(page);
                page = new Page(_pageManager, reference.PageIndex, () => Serialize(obj), obj);
            }

            var backingObject = (RadixTreePageBackingObject)page.BackingObject;

            if (backingObject.Items.Count == reference.ItemIndex + 1)
                backingObject.Items.RemoveAt(reference.ItemIndex);
            else
                backingObject.Items[reference.ItemIndex] = null;

            if (backingObject.Items.Any(item => item != null))
            {
                _pageManager.UpdatePage(page);
            }
            else
            {
                _pageManager.RemovePage(page.Index);
                if (_recentPages.Contains(page.Index))
                    _recentPages.Remove(page.Index);
            }
        }

        private IRadixTreeNode NewNode(DbItemReference parentReference)
        {
            return new RadixTreeNode(0)
            {
                ParentNodeReference = parentReference,
            };
        }

        public RadixTreeNodeStorage(IPageManager pageManager)
        {
            _pageManager = pageManager;
        }
    }
}