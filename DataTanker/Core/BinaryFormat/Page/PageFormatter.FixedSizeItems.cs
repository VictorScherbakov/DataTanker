namespace DataTanker.BinaryFormat.Page
{
    using System.Linq;
    using System;
    using System.Collections.Generic;

    using MemoryManagement;
    using PageManagement;

    /// <summary>
    /// Contains methods for control on-page structures.
    /// </summary>
    internal static partial class PageFormatter
    {
        public static short ReadFixedSizeItemsCount(IPage page)
        {
            short[] itemLengths = ReadFixedSizeItemLengths(page);
            short result = 0;

            foreach (short il in itemLengths)
                if (il != -1)
                    result++;

            return result;
        }

        public static short ReadFixedSizeItemMarkersLength(IPage page)
        {
            SizeRange sc = PageHeaderBase.GetSizeRange(page);

            if (sc == SizeRange.MultiPage || sc == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);
            return BitConverter.ToInt16(page.Content, headerLength);
        }

        private static short GetFirstFixedSizeItemEmptySlotIndex(IPage page, short headerLength)
        {
            short itemSizesLength = BitConverter.ToInt16(page.Content, headerLength);

            int offset = headerLength + OnPagePointerSize;
            var content = page.Content;
            for (short i = 0; i < itemSizesLength; i++)
            {
                if (BitConverter.ToInt16(content, offset) == -1)
                    return i;
                offset += OnPagePointerSize;
            }

            return -1;
        }

        public static short[] ReadFixedSizeItemLengths(IPage page)
        {
            SizeRange range = PageHeaderBase.GetSizeRange(page);

            if (range == SizeRange.MultiPage || range == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            short headerLength = PageHeaderBase.GetHeaderLength(page);
            short itemSizesLength = BitConverter.ToInt16(page.Content, headerLength);

            var result = new short[itemSizesLength];

            int offset = headerLength + OnPagePointerSize;
            var content = page.Content;
            for (int i = 0; i < itemSizesLength; i++)
            {
                result[i] = BitConverter.ToInt16(content, offset);
                offset += OnPagePointerSize;
            }

            return result;
        }

        public static short ReadFixedSizeItemLength(IPage page, short itemIndex)
        {
            SizeRange range = PageHeaderBase.GetSizeRange(page);

            if (range == SizeRange.MultiPage || range == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);
            short itemSizesLength = BitConverter.ToInt16(page.Content, headerLength);

            if (itemIndex >= itemSizesLength)
                throw new ArgumentOutOfRangeException(nameof(itemIndex));

            int offset = headerLength + OnPagePointerSize + itemIndex * OnPagePointerSize;
            return BitConverter.ToInt16(page.Content, offset);
        }

        public static bool IsFixedSizeItemAllocated(IPage page, short itemIndex)
        {
            SizeRange sc = PageHeaderBase.GetSizeRange(page);

            if (sc == SizeRange.MultiPage || sc == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            short headerLength = BitConverter.ToInt16(page.Content, OnPageOffsets.HeaderLength);
            short itemSizesLength = BitConverter.ToInt16(page.Content, headerLength);

            if (itemIndex >= itemSizesLength)
                return false;

            int offset = headerLength + OnPagePointerSize + itemIndex * OnPagePointerSize;
            return BitConverter.ToInt16(page.Content, offset) != -1;
        }

        public static DbItem ReadFixedSizeItem(IPage page, short itemIndex)
        {
            short itemLength = ReadFixedSizeItemLength(page, itemIndex);
            if (itemLength == -1)
                throw new PageFormatException("Item has been deleted.");

            var itemBytes = new byte[itemLength];
            short slotSize = DbItem.GetMaxSize(PageHeaderBase.GetSizeRange(page));
            int offset = page.Content.Length - slotSize * (itemIndex + 1);

            for (int j = 0; j < itemBytes.Length; j++)
                itemBytes[j] = page.Content[j + offset];

            return new DbItem(itemBytes);
        }

        public static List<DbItem> ReadFixedSizeItems(IPage page)
        {
            short[] itemLengths = ReadFixedSizeItemLengths(page);
            var result = new List<DbItem>(itemLengths.Length);

            short maxSize = DbItem.GetMaxSize(PageHeaderBase.GetSizeRange(page));
            int offset = page.Content.Length - maxSize;
            var content = page.Content;
            int cnt = itemLengths.Length;
            for (int i = 0; i < cnt; i++)
            {
                var il = itemLengths[i];
                if (il != -1) // item is actualy present on page, read it
                {
                    var itemBytes = new byte[Math.Max(il, (short)0)];
                    Buffer.BlockCopy(content, offset, itemBytes, 0, itemBytes.Length);

                    result.Add(new DbItem(itemBytes));
                }

                offset -= maxSize;
            }

            return result;
        }

        public static void FormatFixedSizeItemsPage(IPage page, PageHeaderBase header, DbItem[] items)
        {
            var sizeRange = header.SizeRange;
            if (items.Any())
            {
                if (items.Any(item => item.SizeRange != sizeRange))
                    throw new ArgumentException("Size ranges should be equal", nameof(items));
            }

            header.WriteToPage(page);

            int remainingSpace =
                page.Length -                                                     // full page length
                header.Length -                                                   // subtract header length
                items.Length * OnPagePointerSize - OnPagePointerSize;             // subtract item length markers array and its length

            if (remainingSpace < 0)
                throw new ArgumentException("Page have no space to add specified items", nameof(items));

            var maxSize = DbItem.GetMaxSize(sizeRange);
            var content = page.Content;
            int contentLength = page.Length;
            int cnt = items.Length;
            int headerLength = header.Length;

            for (int index = 0; index < cnt; index++)
            {
                var rawData = items[index].RawData;

                // write the length marker
                Buffer.BlockCopy(BitConverter.GetBytes((short)rawData.Length), 0, content, headerLength + OnPagePointerSize * (index + 1), sizeof(short));

                // write the body of item
                Buffer.BlockCopy(rawData, 0, content, contentLength - maxSize * (index + 1), rawData.Length);
            }

            // write the length of length marker array
            byte[] lengthLengthMarkers = BitConverter.GetBytes((short)cnt);
            lengthLengthMarkers.CopyTo(content, headerLength);
        }

        public static DbItemReference AddFixedSizeItem(IPage page, DbItem item, out bool hasRemainingSpace)
        {
            var header = (FixedSizeItemsPageHeader)GetPageHeader(page);
            if (header.SizeRange == SizeRange.MultiPage ||
               header.SizeRange == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            if (item.GetAllocationType(page.Length) == AllocationType.MultiPage)
                throw new PageFormatException("Unable to add item on page. Item is too large.");

            if (header.SizeRange != item.SizeRange)
                throw new PageFormatException("Unable to add item on page. Mismatch size ranges.");

            hasRemainingSpace = false;

            byte[] lengthBytes;
            if (header.EmptySlotCount > 0)
            {
                // find an empty slot for a new item
                var slotIndex = GetFirstFixedSizeItemEmptySlotIndex(page, header.Length);
                if (slotIndex == -1)
                    throw new DataTankerException($"Page is corrupt: empty slot counter is {header.EmptySlotCount}, but there is no empty slot found");

                // write the item length marker
                lengthBytes = BitConverter.GetBytes((short)item.RawData.Length);
                lengthBytes.CopyTo(page.Content,
                                   header.Length +               // length of header
                                   OnPagePointerSize +           // length of length marker array
                                   slotIndex * OnPagePointerSize // offset in length marker array
                    );

                Buffer.BlockCopy(item.RawData,
                                 0,
                                 page.Content,
                                 page.Length - DbItem.GetMaxSize(header.SizeRange) * (slotIndex + 1),
                                 item.RawData.Length);

                if (header.EmptySlotCount > 1)
                    hasRemainingSpace = true;

                // decrease empty slot count
                BitConverter.GetBytes((short)(header.EmptySlotCount - 1)).CopyTo(page.Content, OnPageOffsets.FixedSizeItem.EmptySlotCount);
                return new DbItemReference(page.Index, slotIndex);
            }

            // there are no empty slots on this page
            // check the page have the enough space to allocate a new slot
            var newSlotCount = GetNewSlotCount(page, header);
            if (newSlotCount == 0)
                throw new PageFormatException("The page have no space to add an item.");

            var itemLengthsLength = BitConverter.ToInt16(page.Content, header.Length);

            // write the length marker
            lengthBytes = BitConverter.GetBytes((short)item.RawData.Length);
            lengthBytes.CopyTo(page.Content,
                header.Length +                              // length of header
                OnPagePointerSize +                    // length of length marker array
                itemLengthsLength * OnPagePointerSize // skip to the end of length marker array
                );

            // write the increased length of length marker array
            byte[] lengthLengthMarkers = BitConverter.GetBytes((short)(itemLengthsLength + 1));
            lengthLengthMarkers.CopyTo(page.Content, header.Length);

            // write the item body
            Buffer.BlockCopy(item.RawData,
                0,
                page.Content,
                page.Length - DbItem.GetMaxSize(header.SizeRange) * (itemLengthsLength + 1),
                item.RawData.Length);

            if (newSlotCount > 1)
                hasRemainingSpace = true;

            return new DbItemReference(page.Index, itemLengthsLength);
        }

        public static void DeleteFixedSizeItem(IPage page, short itemIndex)
        {
            if (itemIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(itemIndex));

            var header = (FixedSizeItemsPageHeader)GetPageHeader(page);
            if (header.SizeRange == SizeRange.MultiPage ||
               header.SizeRange == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            short itemLengths = ReadFixedSizeItemLength(page, itemIndex);

            if (itemLengths == -1)
                throw new PageFormatException("A fixed size item is already deleted.");

            _deletedMarkerBytes.CopyTo(page.Content, header.Length + OnPagePointerSize + itemIndex * OnPagePointerSize);

            header.EmptySlotCount++;

            byte[] emptySlotCountBytes = BitConverter.GetBytes(header.EmptySlotCount);
            emptySlotCountBytes.CopyTo(page.Content, OnPageOffsets.FixedSizeItem.EmptySlotCount);
        }

        public static void RewriteFixedSizeItem(IPage page, short itemIndex, DbItem item)
        {
            if (itemIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(itemIndex));

            PageHeaderBase header = GetPageHeader(page);
            if (header.SizeRange == SizeRange.MultiPage ||
               header.SizeRange == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            if (ReadFixedSizeItemMarkersLength(page) - 1 < itemIndex)
                throw new PageFormatException("Unable to rewrite fixed size item. Index is too large.");

            // write the item length marker
            byte[] lengthBytes = BitConverter.GetBytes((short)item.RawData.Length);
            lengthBytes.CopyTo(page.Content,
                header.Length +              // length of header
                OnPagePointerSize +    // length of length marker array
                itemIndex * OnPagePointerSize  // offset in length marker array
                );

            Buffer.BlockCopy(item.RawData,
                0,
                page.Content,
                page.Length - DbItem.GetMaxSize(header.SizeRange) * (itemIndex + 1),
                item.RawData.Length);
        }

        public static void DeleteFixedSizeItems(IPage page)
        {
            PageHeaderBase header = GetPageHeader(page);
            if (header.SizeRange == SizeRange.MultiPage ||
               header.SizeRange == SizeRange.NotApplicable)
                throw new PageFormatException("Page is not dedicated to fixed size items.");

            // set length of item markers to zero
            zeroBytesShort.CopyTo(page.Content, header.Length);

            // set empty slot count to zero
            zeroBytesShort.CopyTo(page.Content, OnPageOffsets.FixedSizeItem.EmptySlotCount);
        }

        private static readonly byte[] zeroBytesShort = BitConverter.GetBytes((short)0);

        private static int GetNewSlotCount(IPage page, PageHeaderBase header)
        {
            short fixedSizeItemMarkersLength = BitConverter.ToInt16(page.Content, FixedSizeItemsPageHeader.FixedSizeItemsHeaderLength);

            short slotSize = DbItem.GetMaxSize(header.SizeRange);

            int remainingSpace =
                page.Length -                                                                 // full page length
                header.Length -                                                               // subtract header length
                fixedSizeItemMarkersLength * OnPagePointerSize - OnPagePointerSize - // subtract item length markers array and its length
                fixedSizeItemMarkersLength * slotSize;                                           // subtract items

            return remainingSpace / (slotSize + OnPagePointerSize);
        }

        public static bool HasFreeSpaceForFixedSizeItem(IPage page)
        {
            PageHeaderBase header = GetPageHeader(page);
            if (header.SizeRange == SizeRange.NotApplicable ||
               header.SizeRange == SizeRange.MultiPage)
                return false;

            short fixedSizeItemMarkersLength = ReadFixedSizeItemMarkersLength(page);

            short slotSize = DbItem.GetMaxSize(header.SizeRange);

            int remainingSpace =
                page.Length -                                                                 // full page length
                header.Length -                                                               // subtract header length
                fixedSizeItemMarkersLength * OnPagePointerSize - OnPagePointerSize -  // subtract item length markers array and its length
                ReadFixedSizeItemsCount(page) * slotSize                                          // subtract non-deleted items
                - OnPagePointerSize;                                                    // subtruct new item length marker needed to add a new item

            if (remainingSpace < slotSize)
                return false;

            return true;
        }
    }
}
