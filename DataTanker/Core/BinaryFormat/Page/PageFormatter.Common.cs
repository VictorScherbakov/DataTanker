namespace DataTanker.BinaryFormat.Page
{
    using System;

    using PageManagement;

    /// <summary>
    /// Contains methods for control on-page structures.
    /// </summary>
    internal static partial class PageFormatter
    {
        private static readonly byte[] _deletedMarkerBytes = BitConverter.GetBytes((short)-1);

        public static readonly int OnPagePointerSize = 2;

        public static void InitPage(IPage page, PageHeaderBase header)
        {
            if (header.Length > page.Length)
                throw new PageFormatException("Unable to format page. Too large header.");

            // fill whole page with zero
            Array.Clear(page.Content, 0, page.Length);
    
            header.WriteToPage(page);
        }

        public static bool IsPageCorrect(IPage page)
        {
            throw new NotImplementedException();
        }

        public static PageHeaderBase GetPageHeader(IPage page)
        {
            PageHeaderBase ph;
            PageType pt = PageHeaderBase.GetPageType(page);
            switch (pt)
            { 
                case PageType.FreeSpaceMap:
                    ph = new FreeSpaceMapPageHeader();
                    ph.Read(page);
                    return ph;
                case PageType.Heading:
                    ph = new HeadingPageHeader();
                    ph.Read(page);
                    return ph;
                case PageType.MultipageItem: 
                    ph = new MultipageItemPageHeader();
                    ph.Read(page);
                    return ph;
                case PageType.FixedSizeItem:
                    ph = new FixedSizeItemsPageHeader();
                    ph.Read(page);
                    return ph;
                case PageType.BPlusTree:
                    ph = new BPlusTreeNodePageHeader();
                    ph.Read(page);
                    return ph;
                case PageType.RadixTree:
                    ph = new RadixTreeNodesPageHeader();
                    ph.Read(page);
                    return ph;
            }
            return null;
        }
    }
}