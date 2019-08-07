namespace DataTanker.AccessMethods.RadixTree.Storage
{
    using System.Collections.Generic;
    using System;
    using System.Diagnostics;

    using PageManagement;
    using BinaryFormat.Page;

    internal class RadixTreePageBackingObject
    {
        public long PageIndex { get; }

        public IList<object> Items { get; }

        private short GetNodeSize(object item)
        {
            if (item == null)
                return 0;

            if (item is byte[] bytes)
            {
                return (short)bytes.Length;
            }

            if (item is IRadixTreeNode radixTreeNode)
            {
                var prefixLength = (short)(radixTreeNode.Prefix?.Length ?? 0);
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
            PageIndex = pageIndex;
            Items = new List<object>();
        }
    }


}