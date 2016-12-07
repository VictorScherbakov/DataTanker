namespace DataTanker.BinaryFormat.Page
{
    using System;

    using PageManagement;

    internal class RadixTreeNodesPageHeader : PageHeaderBase
    {
        public static short RadixTreeNodesHeaderLength = 6;

        public ushort FreeSpace { get; set; }

        public static ushort ReadFreeSpace(IPage page)
        {
            return BitConverter.ToUInt16(page.Content, OnPageOffsets.RadixTreeNodes.FreeSpace);
        }


        public static void WriteFreeSpace(IPage page, ushort freeSpace)
        {
            byte[] bytes = BitConverter.GetBytes(freeSpace);
            bytes.CopyTo(page.Content, OnPageOffsets.RadixTreeNodes.FreeSpace);
        }

        public override SizeRange SizeRange
        {
            get { return SizeRange.NotApplicable; }
            set
            {
                if (value != SizeRange.NotApplicable)
                    throw new InvalidOperationException("Invalid SizeRange.");

                base.SizeRange = value;
            }
        }

        public override void Read(IPage page)
        {
            base.Read(page);

            FreeSpace = ReadFreeSpace(page);
        }

        protected override PageType GetActualPageType()
        {
            return PageType.RadixTree;
        }

        public override void WriteToPage(IPage page)
        {
            Length = RadixTreeNodesHeaderLength;

            base.WriteToPage(page);

            WriteFreeSpace(page, FreeSpace);
        }

        public RadixTreeNodesPageHeader()
        {
            base.SizeRange = SizeRange.Range0;
        }
    }
}