namespace DataTanker.BinaryFormat.Page
{
    using System;

    using PageManagement;

    /// <summary>
    /// Contains methods for control on-page structures.
    /// </summary>
    internal static partial class PageFormatter
    {
        private static FreeSpaceMapPageHeader GetFsmPageHeader(IPage page)
        {
            PageHeaderBase result = GetPageHeader(page);
            if (result.SizeRange != SizeRange.MultiPage ||
                result.PageType != PageType.FreeSpaceMap)
                throw new PageFormatException("Page is not dedicated to free-space-map.");

            return (FreeSpaceMapPageHeader)result;
        }

        public static long GetBasePageIndex(IPage page)
        {
            return GetFsmPageHeader(page).BasePageIndex;
        }

        public static void SetAllFsmValues(IPage page, FsmValue value)
        {
            FreeSpaceMapPageHeader header = GetFsmPageHeader(page);

            byte byteValue = (byte)((byte)value | ((byte)value << 4));

            for (int i = header.Length; i < page.Length; i++)
                page.Content[i] = byteValue;
        }

        public static int GetIndexOfFirstMatchingFsmValue(IPage page, FsmValue targetFsm)
        {
            FreeSpaceMapPageHeader header = GetFsmPageHeader(page);
            var content = page.Content;
            var length = content.Length;

            var fsmByte = (byte)targetFsm;

            const int full = ((byte)FsmValue.Full | ((byte)FsmValue.Full << 4));

            for (int i = header.Length; i < length; i++)
            {
                if (content[i] == full) continue;

                var first = (byte)(0x0F & content[i]);
                var second = (byte)((0xF0 & content[i]) >> 4);

                if (first == fsmByte)
                {
                    return (i - header.Length) * 2;
                }

                if (second == fsmByte)
                {
                    return (i - header.Length) * 2 + 1;
                }
            }

            return -1;
        }

        public static FsmValue GetFsmValue(IPage page, int index)
        {
            FreeSpaceMapPageHeader header = GetFsmPageHeader(page);

            int i = header.Length + index / 2;

            if (index % 2 == 0)
                return (FsmValue)(0x0F & page.Content[i]);

            return (FsmValue)((0x0F & page.Content[i] >> 4));
        }

        public static int GetFsmEntryCount(IPage page)
        {
            return (page.Content.Length - GetFsmPageHeader(page).Length) * 2;
        }

        public static void SetFsmValue(IPage page, int index, FsmValue newValue)
        {
            FreeSpaceMapPageHeader header = GetFsmPageHeader(page);

            if (index < 0 || index > (page.Length - header.Length) * 2)
                throw new ArgumentOutOfRangeException(nameof(index));

            int targetIndex = index / 2 + header.Length;
            byte b = page.Content[targetIndex];
            if (index % 2 == 0)
            {
                b = (byte)((b & 0xF0) | (byte)newValue);
            }
            else
            {
                b = (byte)((b & 0x0F) | ((byte)newValue << 4));
            }

            page.Content[targetIndex] = b;
        }
    }
}
