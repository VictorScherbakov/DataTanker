namespace DataTanker.AccessMethods.RadixTree.Storage
{
    using System.Collections.Generic;
    using System;
    using System.Diagnostics;

    using PageManagement;
    using BinaryFormat.Page;

    internal class RadixTreePageBackingObject
    {
        private readonly long _pageIndex;

        public long PageIndex
        {
            get { return _pageIndex; }
        }

        public IList<object> Items { get; private set; }

        private short GetNodeSize(object item)
        {
            if (item == null)
                return 0;

            var bytes = item as byte[];
            if (bytes != null)
            {
                return (short)bytes.Length;
            }

            var radixTreeNode = item as IRadixTreeNode;
            if (radixTreeNode != null)
            {
                var prefixLength = (short)(radixTreeNode.Prefix == null ? 0 : radixTreeNode.Prefix.Length);
                return RadixTreeNodeStorage.GetNodeSize(prefixLength, radixTreeNode.Entries.Count);
            }

            Debug.Fail("Should never get here");

            return -1;
        }

        public short GetNodeSize(short index)
        {
            if(index < 0 || index >= Items.Count)
                throw new ArgumentOutOfRangeException();

            var item = Items[index];

            return GetNodeSize(item);
        }

        public short Size 
        {
            get
            {
                short result = 0;

                foreach (var item in Items)
                    result += GetNodeSize(item);

                result += (short)((Items.Count + 1) * PageFormatter.OnPagePointerSize + RadixTreeNodesPageHeader.RadixTreeNodesHeaderLength);
                return result;
            } 
        }

        public static RadixTreePageBackingObject FromPage(IPage page)
        {
            var result = new RadixTreePageBackingObject(page.Index);
            var items = PageFormatter.ReadVariableSizeItems(page);

            foreach (var item in items)
                result.Items.Add(item);

            return result;
        }

        public RadixTreePageBackingObject(long pageIndex)
        {
            _pageIndex = pageIndex;
            Items = new List<object>();
        }
    }


}