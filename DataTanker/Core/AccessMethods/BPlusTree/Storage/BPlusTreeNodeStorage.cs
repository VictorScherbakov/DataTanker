namespace DataTanker.AccessMethods.BPlusTree.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using PageManagement;
    using BinaryFormat.Page;
    using MemoryManagement;

    /// <summary>
    /// Implementation of INodeStorage. 
    /// Instances of this class store nodes using specified object that implements IPageManager.
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    internal class BPlusTreeNodeStorage<TKey> : INodeStorage<TKey>
        where TKey : IComparableKey
    {
        private const short _keyLengthSize = sizeof(Int16);

        private readonly IPageManager _pageManager;
        private readonly ISerializer<TKey> _keySerializer;
        private readonly int _maxKeySize;
        private int _nodeCapacity;

        private readonly DbItem[] _dbItems;

        private readonly SizeRange _nodeEntrySizeRange;

        private readonly bool _pageCacheEnabled;

        private long? _rootIndex;
        private readonly object _locker = new object();

        private KeyValuePair<TKey, DbItemReference> IndexEntryFromBytes(byte[] bytes)
        {
            short keyLength = BitConverter.ToInt16(bytes, 0);
            var keyBytes = new byte[keyLength];
            Buffer.BlockCopy(bytes, sizeof(Int16), keyBytes, 0, keyLength);
            return new KeyValuePair<TKey, DbItemReference>(_keySerializer.Deserialize(keyBytes), DbItemReference.FromBytes(bytes, _maxKeySize + _keyLengthSize));
        }

        private byte[] GetIndexEntryBytes(KeyValuePair<TKey, DbItemReference> entry)
        {
            var result = new byte[_keyLengthSize + _maxKeySize + DbItemReference.BytesLength];
            
            var serializedKey = _keySerializer.Serialize(entry.Key);
            if(serializedKey.Length > _maxKeySize)
                throw new DataTankerException("Binary representation of key is too long");

            BitConverter.GetBytes((short)serializedKey.Length).CopyTo(result, 0);
            Buffer.BlockCopy(serializedKey, 0, result, _keyLengthSize, serializedKey.Length);

            entry.Value.WriteBytes(result, result.Length - DbItemReference.BytesLength);

            return result;
        }

        private IBPlusTreeNode<TKey> PageToNode(IPage page)
        {
            var node = page.BackingObject as IBPlusTreeNode<TKey>;
            if (node != null)
                return node;

            var header = (BPlusTreeNodePageHeader)PageFormatter.GetPageHeader(page);
            List<DbItem> items = PageFormatter.ReadFixedSizeItems(page);

            var result = new BPlusTreeNode<TKey>(page.Index, items.Count)
                             {
                                 IsLeaf = header.IsLeaf,
                                 ParentNodeIndex = header.ParentPageIndex,
                                 PreviousNodeIndex = header.PreviousPageIndex,
                                 NextNodeIndex = header.NextPageIndex
                             };

            int cnt = items.Count;
            for (int i = 0; i < cnt; i++)
                result.Entries.Add(IndexEntryFromBytes(items[i].RawData));

            page.BackingObject = result;

            if (_nodeEntrySizeRange != header.SizeRange)
                throw new DataTankerException("Mismatch key size"); // TODO: specify possible size range

            return result;
        }

        private byte[] Serialize(IBPlusTreeNode<TKey> node)
        {
            var header = new BPlusTreeNodePageHeader
            {
                IsLeaf = node.IsLeaf,
                NextPageIndex = node.NextNodeIndex,
                PreviousPageIndex = node.PreviousNodeIndex,
                ParentPageIndex = node.ParentNodeIndex,
                SizeRange = _nodeEntrySizeRange
            };

            var page = new Page(_pageManager, node.Index, new byte[_pageManager.PageSize]);

            int cnt = node.Entries.Count;

            lock (_locker)
            {
                for (int i = 0; i < cnt; i++)
                {
                    var entry = node.Entries[i];
                    _dbItems[i].RawData = GetIndexEntryBytes(entry);
                }

                PageFormatter.FormatFixedSizeItemsPage(page, header, _dbItems.Take(cnt).ToArray());
            }

            return page.Content;
        }

        private IPage NodeToPage(IBPlusTreeNode<TKey> node)
        {
            return new Page(_pageManager, node.Index, () => Serialize(node), node);
        }

        /// <summary>
        /// Fetches a root node of the tree from storage.
        /// </summary>
        /// <returns>The root node of tree</returns>
        public IBPlusTreeNode<TKey> FetchRoot()
        {
            var headerPage = _pageManager.FetchPage(0);

            if(_rootIndex == null)
                _rootIndex = ((HeadingPageHeader)PageFormatter.GetPageHeader(headerPage)).AccessMethodPageIndex;

            return Fetch(_rootIndex.Value);
        }



        /// <summary>
        /// Updates a node in storage.
        /// </summary>
        /// <param name="node">A node to save</param>
        public void Update(IBPlusTreeNode<TKey> node)
        {
            UnpinNode(node.Index);
            var page = NodeToPage(node);
            _pageManager.UpdatePage(page);
        }

        /// <summary>
        /// Fetches a node by index.
        /// </summary>
        /// <param name="nodeIndex">An index of node</param>
        /// <returns>Fetched node</returns>
        public IBPlusTreeNode<TKey> Fetch(long nodeIndex)
        {
            if (nodeIndex == -1)
                return null;

            var node = PageToNode(_pageManager.FetchPage(nodeIndex));
            return node;
        }

        /// <summary>
        /// Sets the node with the specified index as new root.
        /// </summary>
        /// <param name="nodeIndex">An index of new root node</param>
        public void SetRoot(long nodeIndex)
        {
            var headerPage = _pageManager.FetchPage(0);
            var header = (HeadingPageHeader)PageFormatter.GetPageHeader(headerPage);
            header.AccessMethodPageIndex = nodeIndex;
            header.WriteToPage(headerPage);
            _rootIndex = nodeIndex;
            _pageManager.UpdatePage(headerPage);
        }

        /// <summary>
        /// Gets a maximum entry count that can be placed to the node.
        /// </summary>
        public int NodeCapacity
        {
            get
            {
                if(_nodeCapacity == 0)
                {
                    var itemSize = DbItem.GetMaxSize(DbItem.GetSizeRange(DbItemReference.BytesLength + _maxKeySize + _keyLengthSize));
                    var usefulSize = _pageManager.PageSize - new BPlusTreeNodePageHeader().DefaultSize - 2;
                    _nodeCapacity = usefulSize / (itemSize + 2);
                }

                return _nodeCapacity;
            }
        }

        /// <summary>
        /// Creates a new tree node in storage.
        /// </summary>
        /// <param name="isLeaf">Value indicating whether a created node is leaf node</param>
        /// <returns>Created node</returns>
        public IBPlusTreeNode<TKey> Create(bool isLeaf)
        {
            IPage p = _pageManager.CreatePage();
            return new BPlusTreeNode<TKey>(p.Index)
                       {
                           IsLeaf = isLeaf
                       };
        }

        /// <summary>
        /// Removes a node with the specified index from storage.
        /// </summary>
        /// <param name="nodeIndex">The index of node</param>
        public void Remove(long nodeIndex)
        {
            _pageManager.RemovePage(nodeIndex);
        }

        /// <summary>
        /// Pins the node
        /// </summary>
        public void PinNode(long index)
        {
            if(_pageCacheEnabled)
                ((ICachingPageManager)_pageManager).PinPage(index);
        }

        /// <summary>
        /// Unpins the node
        /// </summary>
        public void UnpinNode(long index)
        {
            if (_pageCacheEnabled)
                ((ICachingPageManager)_pageManager).UnpinPage(index);
        }

        public BPlusTreeNodeStorage(IPageManager pageManager, ISerializer<TKey> keySerializer, int maxKeySize)
        {
            if(maxKeySize <= 0)
                throw new ArgumentException("ComparableComparableKeyOf size should be positive", nameof(maxKeySize));

            _pageManager = pageManager ?? throw new ArgumentNullException(nameof(pageManager));
            _keySerializer = keySerializer ?? throw new ArgumentNullException(nameof(keySerializer));
            _maxKeySize = maxKeySize;
            _nodeEntrySizeRange = DbItem.GetSizeRange(_maxKeySize + DbItemReference.BytesLength);

            if (NodeCapacity <= 2)
                throw new ArgumentException("Too large key size", nameof(maxKeySize));

            _dbItems = new DbItem[NodeCapacity];
            var bytes = new byte[_maxKeySize + DbItemReference.BytesLength];
            for (int i = 0; i < NodeCapacity; i++)
                _dbItems[i] = new DbItem(bytes);

            _pageCacheEnabled = _pageManager is ICachingPageManager;
        }
    }
}