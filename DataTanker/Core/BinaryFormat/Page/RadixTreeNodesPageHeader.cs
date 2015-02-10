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

        public override SizeClass SizeClass
        {
            get { return SizeClass.NotApplicable; }
            set
            {
                if (value != SizeClass.NotApplicable)
                    throw new InvalidOperationException("Invalid SizeClass.");

                base.SizeClass = value;
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
            base.SizeClass = SizeClass.Class0;
        }
    }
}