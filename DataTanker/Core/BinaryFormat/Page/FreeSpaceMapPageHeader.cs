namespace DataTanker.BinaryFormat.Page
{
    using System;

    using PageManagement;

    internal sealed class FreeSpaceMapPageHeader : MultipageItemPageHeaderBase
    {
        public long BasePageIndex { get; set; }

        public override void Read(IPage page)
        {
            base.Read(page);
            BasePageIndex = BitConverter.ToInt64(page.Content, OnPageOffsets.Fsm.BasePageIndex);
        }

        protected override PageType GetActualPageType()
        {
            return PageType.FreeSpaceMap;
        }

        public override void WriteToPage(IPage page)
        {
            Length = 36;

            base.WriteToPage(page);

            // base page index
            byte[] baseIndexBytes = BitConverter.GetBytes(BasePageIndex);
            baseIndexBytes.CopyTo(page.Content, OnPageOffsets.Fsm.BasePageIndex);
        }
    }
}