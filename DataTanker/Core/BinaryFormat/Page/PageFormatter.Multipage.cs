namespace DataTanker.BinaryFormat.Page
{
    using System;

    using MemoryManagement;
    using PageManagement;

    /// <summary>
    /// Contains methods for control on-page structures.
    /// </summary>
    internal static partial class PageFormatter
    {
        private static MultipageItemPageHeader GetMultipageItemPageHeader(IPage page)
        {
            PageHeaderBase result = GetPageHeader(page);
            if (result.SizeRange != SizeRange.MultiPage)
                throw new PageFormatException("Page is not dedicated to multipage items.");

            return (MultipageItemPageHeader)result;
        }

        public static int WriteMultipageItemBlock(IPage page, DbItem item, long offset)
        {
            if (offset < 0 || offset >= item.RawData.LongLength)
                throw new ArgumentOutOfRangeException(nameof(offset));

            MultipageItemPageHeader header = GetMultipageItemPageHeader(page);

            if (item.GetAllocationType(page.Length) == AllocationType.SinglePage)
                throw new PageFormatException("Unable to add item block on page. Item allocation type is single page.");

            // when the rawLength is 2772,it'a fake multipageItem as it can be stored in one page.
            int last = Math.Min(page.Length - header.Length - (offset == 0 ? 8 : 0), (int)(item.RawData.LongLength - offset)) - 1;
            int contentOffset = header.Length;

            if (offset == 0)
            {
                // we should write the length of the object
                var lengthBytes = BitConverter.GetBytes(item.RawData.LongLength);
                lengthBytes.CopyTo(page.Content, header.Length);
                contentOffset += 8;
            }

            for (long i = 0; i <= last; i++)
                page.Content[i + contentOffset] = item.RawData[i + offset];

            return last + 1;
        }

        public static long ReadMultipageItemLength(IPage page)
        {
            MultipageItemPageHeader header = GetMultipageItemPageHeader(page);

            if (header.PreviousPageIndex != -1)
                throw new PageFormatException("This page is not the start page of multipage item.");

            return BitConverter.ToInt64(page.Content, header.Length);
        }

        private static byte[] ReadBlock(IPage page, int length, int offset)
        {
            var result = new byte[length];

            for (int i = 0; i < result.Length; i++)
                result[i] = page.Content[i + offset];

            return result;
        }

        public static byte[] ReadMultipageItemBlock(IPage page, int length)
        {
            MultipageItemPageHeader header = GetMultipageItemPageHeader(page);

            var offset = header.Length;

            // skip object length marker if we are on the start page
            if (header.PreviousPageIndex == -1)
            {
                offset += 8;
            }

            return ReadBlock(page, Math.Min(length, page.Length - offset), offset);
        }

        public static byte[] ReadMultipageItemBlock(IPage page)
        {
            return ReadMultipageItemBlock(page, page.Length);
        }
    }
}
