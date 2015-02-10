namespace DataTanker.BinaryFormat.Page
{
    using System;

    using PageManagement;

    internal class FixedSizeItemsPageHeader : ItemsPageHeader
    {
        public static short FixedSizeItemsHeaderLength = 6;

        public short EmptySlotCount { get; set; }

        public override SizeClass SizeClass
        {
            get { return base.SizeClass; }
            set
            {
                if (value == SizeClass.NotApplicable ||
                    value == SizeClass.MultiPage)
                    throw new InvalidOperationException("Invalid SizeClass.");

                base.SizeClass = value;
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

            CheckSizeClass(page.Length);

            base.WriteToPage(page);

            // write the empty slot count
            byte[] emptySlotCountBytes = BitConverter.GetBytes(EmptySlotCount);
            emptySlotCountBytes.CopyTo(page.Content, OnPageOffsets.FixedSizeItem.EmptySlotCount);
        }

        public FixedSizeItemsPageHeader()
        {
            base.SizeClass = SizeClass.Class0;
        }
    }
}