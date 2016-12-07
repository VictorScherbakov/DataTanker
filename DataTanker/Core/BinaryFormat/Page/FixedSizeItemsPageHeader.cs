namespace DataTanker.BinaryFormat.Page
{
    using System;

    using PageManagement;

    internal class FixedSizeItemsPageHeader : ItemsPageHeader
    {
        public static short FixedSizeItemsHeaderLength = 6;

        public short EmptySlotCount { get; set; }

        public override SizeRange SizeRange
        {
            get { return base.SizeRange; }
            set
            {
                if (value == SizeRange.NotApplicable ||
                    value == SizeRange.MultiPage)
                    throw new InvalidOperationException("Invalid SizeRange.");

                base.SizeRange = value;
            }
        }

        public override void Read(IPage page)
        {
            base.Read(page);

            EmptySlotCount = BitConverter.ToInt16(page.Content, OnPageOffsets.FixedSizeItem.EmptySlotCount);
        }

        protected override PageType GetActualPageType()
        {
            return PageType.FixedSizeItem;
        }

        public override void WriteToPage(IPage page)
        {
            Length = FixedSizeItemsHeaderLength;

            CheckSizeRange(page.Length);

            base.WriteToPage(page);

            // write the empty slot count
            byte[] emptySlotCountBytes = BitConverter.GetBytes(EmptySlotCount);
            emptySlotCountBytes.CopyTo(page.Content, OnPageOffsets.FixedSizeItem.EmptySlotCount);
        }

        public FixedSizeItemsPageHeader()
        {
            base.SizeRange = SizeRange.Range0;
        }
    }
}