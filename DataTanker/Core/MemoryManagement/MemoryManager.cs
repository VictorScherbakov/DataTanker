namespace DataTanker.MemoryManagement
{
    using System;

    using PageManagement;
    using BinaryFormat.Page;

    /// <summary>
    /// Implementation of IMemoryManager using specified FreeSpaceMap and PageManager instances.
    /// </summary>
    internal class MemoryManager : IMemoryManager
    {
        private readonly FreeSpaceMap _fsm;
        private readonly IPageManager _pageManager;

        private DbItemReference AllocateSinglePage(DbItem item)
        {
            DbItemReference result;
            IPage page;

            var fsmValue = EnumHelper.FsmValueFromSizeRange(item.SizeRange);

            var index = _fsm.GetFreePageIndex(fsmValue);

            bool spaceRemains;

            if(index != -1)
            {
                // place object to existing page
                page = _pageManager.FetchPage(index);
                
                result = PageFormatter.AddFixedSizeItem(page, item, out spaceRemains);

                if (!spaceRemains)
                    _fsm.Set(index, FsmValue.Full);
            }
            else
            {
                // allocate on a new page
                page = _pageManager.CreatePage();
                var header = new FixedSizeItemsPageHeader { SizeRange = item.SizeRange };
                
                PageFormatter.InitPage(page, header);
                result = PageFormatter.AddFixedSizeItem(page, item, out spaceRemains);

                _fsm.Set(result.PageIndex, spaceRemains ? fsmValue : FsmValue.Full);
            }

            _pageManager.UpdatePage(page);

            return result;
        }

        private DbItemReference AllocateMultiPage(DbItem item)
        {
            long bytesWritten = 0;
            long startPageIndex = -1;
            DbItemReference result = null;
            IPage page = null;
            IPage previousPage = null;

            while (bytesWritten < item.RawData.LongLength)
            {
                page = _pageManager.CreatePage();

                if (startPageIndex == -1)
                {
                    startPageIndex = page.Index;
                    result = new DbItemReference(page.Index, 0);
                }

                var header = new MultipageItemPageHeader
                                 {
                                     StartPageIndex = startPageIndex, 
                                     PreviousPageIndex = previousPage?.Index ?? -1,
                                     NextPageIndex = -1,
                                     SizeRange = SizeRange.MultiPage
                                 };

                PageFormatter.InitPage(page, header);
                bytesWritten += PageFormatter.WriteMultipageItemBlock(page, item, bytesWritten);

                if (previousPage != null)
                {
                    header = (MultipageItemPageHeader)PageFormatter.GetPageHeader(previousPage);
                    header.NextPageIndex = page.Index;
                    header.WriteToPage(previousPage);
                    _pageManager.UpdatePage(previousPage);
                }

                previousPage = page;
            }

            if (page != null) 
                _pageManager.UpdatePage(page);

            return result;
        }

        /// <summary>
        /// Allocates new db item with specified content and produces reference to it.
        /// </summary>
        /// <param name="content">Content of item to allocate</param>
        /// <returns>Reference to the allocated item</returns>
        public DbItemReference Allocate(byte[] content)
        {
            var item = new DbItem(content);
            return item.GetAllocationType(_pageManager.PageSize) == AllocationType.SinglePage 
                ? AllocateSinglePage(item) 
                : AllocateMultiPage(item);
        }

        /// <summary>
        /// Releases db item by its reference
        /// </summary>
        /// <param name="reference">Reference to item to release</param>
        public void Free(DbItemReference reference)
        {
            IPage page = _pageManager.FetchPage(reference.PageIndex);
            var header = PageFormatter.GetPageHeader(page);
            if(header.SizeRange == SizeRange.MultiPage)
            {
                while (page != null)
                {
                    _pageManager.RemovePage(page.Index);
                    var nextPageIndex = ((MultipageItemPageHeader)PageFormatter.GetPageHeader(page)).NextPageIndex;
                    page = nextPageIndex == -1 
                        ? null
                        : _pageManager.FetchPage(nextPageIndex);
                }        
            }
            else
            {
                if(PageFormatter.ReadFixedSizeItemsCount(page) == 1)
                {
                    _pageManager.RemovePage(page.Index);
                    _fsm.Set(page.Index, FsmValue.Full);
                }
                else
                {
                    PageFormatter.DeleteFixedSizeItem(page, reference.ItemIndex);
                    _pageManager.UpdatePage(page);
                    _fsm.Set(page.Index, EnumHelper.FsmValueFromSizeRange(header.SizeRange));
                }
            }
        }

        /// <summary>
        /// Reallocates already allocated db item with specified content and produces reference to it.
        /// </summary>
        /// <param name="reference">Reference to already allocated item</param>
        /// <param name="newContent">Content of item to reallocate</param>
        /// <returns>Reference to the reallocated item</returns>
        public DbItemReference Reallocate(DbItemReference reference, byte[] newContent)
        {
            var item = new DbItem(newContent);
            if (item.GetAllocationType(_pageManager.PageSize) == AllocationType.SinglePage)
            {
                IPage page = _pageManager.FetchPage(reference.PageIndex);
                if (PageFormatter.GetPageHeader(page).SizeRange == item.SizeRange)
                {
                    PageFormatter.RewriteFixedSizeItem(page, reference.ItemIndex, item);

                    _pageManager.UpdatePage(page);

                    return reference;
                }

                Free(reference);
                return AllocateSinglePage(item);
            }

            Free(reference);
            return AllocateMultiPage(item);
        }

        /// <summary>
        /// Gets DbItem instance by reference.
        /// </summary>
        /// <param name="reference">Reference to the requested db item</param>
        /// <returns></returns>
        public DbItem Get(DbItemReference reference)
        {
            if (!_pageManager.PageExists(reference.PageIndex))
                return null;

            IPage page = _pageManager.FetchPage(reference.PageIndex);

            var header = PageFormatter.GetPageHeader(page);
            if (header.SizeRange == SizeRange.MultiPage)
            {
                var length = PageFormatter.ReadMultipageItemLength(page);
                var content = new byte[length];
                long offset = 0;

                while (page != null)
                {
                    var readBytes = PageFormatter.ReadMultipageItemBlock(page, Math.Min(_pageManager.PageSize, (int)(length - offset)));    
                    readBytes.CopyTo(content, offset);
                    offset += readBytes.Length;
                    var nextPageIndex = ((MultipageItemPageHeader) header).NextPageIndex;

                    if (nextPageIndex != -1)
                    {
                        page = _pageManager.FetchPage(nextPageIndex);
                        header = PageFormatter.GetPageHeader(page);
                    }
                    else
                        page = null;
                }

                return new DbItem(content);
            }

            return PageFormatter.IsFixedSizeItemAllocated(page, reference.ItemIndex) 
                    ? PageFormatter.ReadFixedSizeItem(page, reference.ItemIndex) 
                    : null;
        }

        /// <summary>
        /// Gets the db item length (in bytes).
        /// </summary>
        /// <param name="reference">Reference to the requested db item</param>
        /// <returns>The length of requested db item</returns>
        public long GetLength(DbItemReference reference)
        {
            if (!_pageManager.PageExists(reference.PageIndex))
                return 0;

            IPage page = _pageManager.FetchPage(reference.PageIndex);

            var header = PageFormatter.GetPageHeader(page);
            if (header.SizeRange == SizeRange.MultiPage)
                return PageFormatter.ReadMultipageItemLength(page);

            return PageFormatter.IsFixedSizeItemAllocated(page, reference.ItemIndex)
                    ? PageFormatter.ReadFixedSizeItemLength(page, reference.ItemIndex)
                    : 0;
        }

        private byte[] GetLargeItemSegment(PageHeaderBase header, IPage page, long startIndex, long endIndex)
        {
            if (PageFormatter.ReadMultipageItemLength(page) <= endIndex) throw new ArgumentOutOfRangeException(nameof(endIndex));

            var length = endIndex - startIndex + 1;
            var result = new byte[length];
            long sourceOffset = 0;
            long destOffset = 0;

            // navigate through large item
            while (true)
            {
                var readBytes = PageFormatter.ReadMultipageItemBlock(page, Math.Min(_pageManager.PageSize, (int)(endIndex + 1 - sourceOffset)));
                sourceOffset += readBytes.Length;

                if (sourceOffset > startIndex)
                {
                    var ssi = sourceOffset - startIndex - 1 < readBytes.Length
                                 ? startIndex + readBytes.Length - sourceOffset
                                 : 0;

                    var l = readBytes.Length - Math.Max(0, sourceOffset - endIndex - 1) - ssi;

                    Array.Copy(readBytes, ssi, result, destOffset, l);

                    destOffset += l;

                    if (sourceOffset >= endIndex)
                        return result;
                }

                var nextPageIndex = ((MultipageItemPageHeader)header).NextPageIndex;

                if (nextPageIndex != -1)
                {
                    page = _pageManager.FetchPage(nextPageIndex);
                    header = PageFormatter.GetPageHeader(page);
                }
            }
        }

        private byte[] GetFixedSizeItemSegment(IPage page, DbItemReference reference, long startIndex, long endIndex)
        {
            if (!PageFormatter.IsFixedSizeItemAllocated(page, reference.ItemIndex))
                return new byte[] { };

            var item = PageFormatter.ReadFixedSizeItem(page, reference.ItemIndex);
            if (item.RawData.Length >= endIndex)
                throw new ArgumentOutOfRangeException(nameof(endIndex));

            var length = endIndex - startIndex + 1;
            var result = new byte[length];

            Array.Copy(item.RawData, startIndex, result, 0, length);

            return result;
        }

        /// <summary>
        /// Gets the segment of binary representation of db item.
        /// </summary>
        /// <param name="reference">Reference to the db item</param>
        /// <param name="startIndex">The start index in binary representation</param>
        /// <param name="endIndex">The end index in binary representation</param>
        /// <returns>The array of bytes containing specified segment of db item</returns>
        public byte[] GetItemSegment(DbItemReference reference, long startIndex, long endIndex)
        {
            if (startIndex < 0) throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (endIndex < 0) throw new ArgumentOutOfRangeException(nameof(endIndex));

            if(endIndex < startIndex)
                throw new ArgumentException("End index should be qreater than or equal to start index");

            if (!_pageManager.PageExists(reference.PageIndex))
                return new byte[] {};

            IPage page = _pageManager.FetchPage(reference.PageIndex);

            var header = PageFormatter.GetPageHeader(page);
            if (header.SizeRange == SizeRange.MultiPage)
                return GetLargeItemSegment(header, page, startIndex, endIndex);

            return GetFixedSizeItemSegment(page, reference, startIndex, endIndex);
        }

        public MemoryManager(FreeSpaceMap fsm, IPageManager pageManager)
        {
            _fsm = fsm;
            _pageManager = pageManager;
        }
    }
}
