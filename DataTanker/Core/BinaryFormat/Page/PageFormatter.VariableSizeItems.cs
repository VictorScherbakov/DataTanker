namespace DataTanker.BinaryFormat.Page
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using PageManagement;
    using Settings;

    /// <summary>
    /// Contains methods for control on-page structures.
    /// </summary>
    internal static partial class PageFormatter
    {
        public static short ReadItemMarkersLength(IPage page)
        {
            PageType pageType = PageHeaderBase.GetPageType(page);

            if (pageType != PageType.RadixTree)
                throw new PageFormatException("Page is not dedicated to variable size items.");

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);
            return BitConverter.ToInt16(page.Content, headerLength);
        }

        public static ushort GetFreeSpace(IPage page)
        {
            PageType pageType = PageHeaderBase.GetPageType(page);

            if (pageType != PageType.RadixTree)
                throw new PageFormatException("Page is not dedicated to variable size items.");

            return RadixTreeNodesPageHeader.ReadFreeSpace(page);
        }

        public static int GetMaximalFreeSpace(PageSize pageSize)
        {
            return
                (int)pageSize                                         // whole page
                - RadixTreeNodesPageHeader.RadixTreeNodesHeaderLength // minus header length
                - OnPagePointerSize * 2;                        // minus marker size needed to place one object and marker array length  
        }

        private static short[] ReadMarkers(IPage page, short headerLength)
        {
            var length = ReadItemMarkersLength(page);
            var result = new short[length];

            int offset = headerLength + OnPagePointerSize;
            var content = page.Content;

            for (int i = 0; i < length; i++)
            {
                result[i] = BitConverter.ToInt16(content, offset);
                offset += OnPagePointerSize;
            }
            return result;
        }

        public static byte[] ReadVariableSizeItem(IPage page, short itemIndex)
        {
            PageType pageType = PageHeaderBase.GetPageType(page);
            if (pageType != PageType.RadixTree)
                throw new PageFormatException("Page is not dedicated to variable size items.");


            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);

            var lengthMarkers = ReadMarkers(page, headerLength);

            if (itemIndex >= lengthMarkers.Length)
                throw new PageFormatException("Wrong item index.");

            int offset = 0;
            int length = 0;
            for (int i = 0; i <= itemIndex; i++)
            {
                length = lengthMarkers[i];
                offset += Math.Abs(length);
            }

            if (length <= 0)
                throw new PageFormatException("Item has been deleted.");

            var result = new byte[length];
            Buffer.BlockCopy(page.Content, page.Content.Length - offset, result, 0, length);

            return result;
        }

        public static List<byte[]> ReadVariableSizeItems(IPage page)
        {
            PageType pageType = PageHeaderBase.GetPageType(page);
            if (pageType != PageType.RadixTree)
                throw new PageFormatException("Page is not dedicated to variable size items.");

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);

            var result = new List<byte[]>();

            var lengthMarkers = ReadMarkers(page, headerLength);

            int offset = 0;
            foreach (var length in lengthMarkers)
            {
                offset += Math.Abs(length);

                var bytes = new byte[length];
                Buffer.BlockCopy(page.Content, page.Content.Length - offset, bytes, 0, length);

                result.Add(bytes);
            }

            return result;
        }

        public static void DeleteVariableSizeItem(IPage page, short index, out bool hasRemainingItems)
        {
            hasRemainingItems = true;
            ushort freeSpace = GetFreeSpace(page);

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);
            var lengthMarkers = ReadMarkers(page, headerLength);
            if (lengthMarkers[index] <= 0)
                throw new PageFormatException("Item has been deleted.");

            if (index == lengthMarkers.Length - 1)
            {
                int i = index;
                lengthMarkers[i] = (short) -lengthMarkers[i];

                while (i >= 0 && lengthMarkers[i] <= 0)
                    i--;

                i++;

                // write length of the marker array
                var markerLengthBytes = BitConverter.GetBytes((short)i);
                markerLengthBytes.CopyTo(page.Content, headerLength);

                WriteFreeSpace(page, (ushort)(freeSpace - lengthMarkers[index] + (lengthMarkers.Length - i) * OnPagePointerSize));

                if(i == 0)
                    hasRemainingItems = false;
            }
            else
            {
                // write length of the marker array
                var markerBytes = BitConverter.GetBytes((short) (-lengthMarkers[index]));
                markerBytes.CopyTo(page.Content, headerLength + OnPagePointerSize*(index + 1));

                WriteFreeSpace(page, (ushort) (freeSpace + lengthMarkers[index]));
            }
        }

        private static void WriteFreeSpace(IPage page, ushort freeSpace)
        {
            RadixTreeNodesPageHeader.WriteFreeSpace(page, freeSpace);
        }

        public static bool ReplaceVariableSizeItemIfPossible(IPage page, short index, byte[] itemContent)
        {
            ushort freeSpace = GetFreeSpace(page);
            var itemLength = itemContent.Length;

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);
            var lengthMarkers = ReadMarkers(page, headerLength);

            if (lengthMarkers[index] < itemLength && freeSpace < itemLength - lengthMarkers[index])
                return false;

            if (lengthMarkers[index] == itemLength)
            {
                int offset = 0;
                for (short i = 0; i <= index; i++)
                    offset += Math.Abs(lengthMarkers[i]);

                RewriteVariableSizeItem(page, itemContent, headerLength, index, offset, null);
            }
            else
                PlaceItemToSlot(page, headerLength, itemContent, lengthMarkers, index);    

            return true;
        }

        private static void PlaceItemToSlot(IPage page, int headerLength, byte[] itemContent, short[] lengthMarkers, short slotIndex)
        {
            var items = new List<byte[]>(lengthMarkers.Length);

            var pageSize = page.Content.Length;

            int offset = 0;
            for (int i = 0; i < lengthMarkers.Length; i++)
            {
                var length = lengthMarkers[i];
                var absLength = Math.Abs(length);
                offset += absLength;
                var item = new byte[absLength];
                items.Add(item);
                if (length > 0)
                {
                    Buffer.BlockCopy(page.Content, pageSize - offset, item, 0, item.Length);
                }
                else
                    lengthMarkers[i] = 0;
            }

            items[slotIndex] = itemContent;
            lengthMarkers[slotIndex] = (short) itemContent.Length;

            offset = 0;
            for (int i = 0; i < lengthMarkers.Length; i++)
            {
                var length = lengthMarkers[i];
                offset += length;
                if(length > 0)
                    Buffer.BlockCopy(items[i], 0, page.Content, pageSize - offset, length);

                var bytes = BitConverter.GetBytes(lengthMarkers[i]);
                bytes.CopyTo(page.Content, headerLength + OnPagePointerSize * (i + 1));
            }

            WriteFreeSpace(page, 
                (ushort)(pageSize - 
                    offset - 
                    headerLength - 
                    OnPagePointerSize * (lengthMarkers.Length + 1)));
        }

        private static void RewriteVariableSizeItem(IPage page, byte[] itemContent, short headerLength, short itemIndex, int offset, ushort? freeSpace)
        {
            var itemLength = itemContent.Length;
            var bytes = BitConverter.GetBytes((short)itemContent.Length);
            bytes.CopyTo(page.Content, headerLength + OnPagePointerSize * (itemIndex + 1));

            Buffer.BlockCopy(itemContent, 0, page.Content, page.Content.Length - offset, itemLength);

            if (freeSpace != null) 
                WriteFreeSpace(page, (ushort) (freeSpace.Value - itemLength));
        }

        public static short AddVariableSizeItem(IPage page, byte[] itemContent)
        {
            ushort freeSpace = GetFreeSpace(page);
            var itemLength = itemContent.Length;
            if (freeSpace < itemLength)
                return -1;

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);

            var lengthMarkers = ReadMarkers(page, headerLength);

            int offset = 0;
            for (short i = 0; i < lengthMarkers.Length; i++)
            {
                offset += Math.Abs(lengthMarkers[i]);
                if (lengthMarkers[i] <= 0)
                {
                    // we have an empty slot
                    if (itemLength == -lengthMarkers[i])
                    {
                        // simple case: a new item has exactly the same size as deleted one, rewrite it
                        RewriteVariableSizeItem(page, itemContent, headerLength, i, offset, freeSpace);
                    }
                    else
                        PlaceItemToSlot(page, headerLength, itemContent, lengthMarkers, i);

                    return i;
                }
            }

            // here we need two extra bytes
            if (freeSpace < itemLength + OnPagePointerSize)
                return -1;


            // write length of the marker array
            var markersLengthBytes = BitConverter.GetBytes((short)(lengthMarkers.Length + 1));
            markersLengthBytes.CopyTo(page.Content, headerLength);

            // write new length marker
            var newMarkerBytes = BitConverter.GetBytes((short)itemContent.Length);
            newMarkerBytes.CopyTo(page.Content, headerLength + OnPagePointerSize * (lengthMarkers.Length + 1));

            // write item itself
            Buffer.BlockCopy(itemContent, 0, page.Content, page.Content.Length - offset - itemLength, itemLength);

            // write free space
            RadixTreeNodesPageHeader.WriteFreeSpace(page, (ushort)(freeSpace - itemLength - OnPagePointerSize));

            return (short)lengthMarkers.Length;
        }

        public static void FormatVariableSizeItemsPage(IPage page, PageHeaderBase header, List<byte[]> items)
        {
            header.WriteToPage(page);

            short itemsLength = items.Aggregate<byte[], short>(0, (current, item) => (short) (current + (short) (item.Length)));

            int remainingSpace =
                page.Length -                                           // full page length
                header.Length -                                         // subtract header length
                itemsLength -                                           // subtract items length 
                items.Count * OnPagePointerSize + OnPagePointerSize;    // subtract markers array and its length

            if (remainingSpace < 0)
                throw new ArgumentException("Page have no space to add specified items", nameof(items));

            var content = page.Content;
            int contentLength = page.Length;
            int cnt = items.Count;
            int headerLength = header.Length;

            short offset = 0;

            for (int index = 0; index < cnt; index++)
            {
                var item = items[index];

                byte[] bytes;
                if (item != null)
                {
                    bytes = BitConverter.GetBytes((short) item.Length);

                    offset += (short) item.Length;

                    // write the body of item
                    Buffer.BlockCopy(item, 0, content, contentLength - offset, item.Length);
                }
                else
                    bytes = zeroBytesShort;

                // write the length marker
                Buffer.BlockCopy(bytes, 0, content, headerLength + OnPagePointerSize * (index + 1), sizeof(short));
            }

            // write the length of length marker array
            byte[] lengthLengthMarkers = BitConverter.GetBytes((short)cnt);
            lengthLengthMarkers.CopyTo(content, headerLength);

            WriteFreeSpace(page, (ushort)remainingSpace);
        }
    }
}
