namespace DataTanker.BinaryFormat.Page
{
    using System;
    using PageManagement;

    internal class HeadingPageHeader : PageHeaderBase
    {
        public int PageSize { get; set; }
        public long FsmPageIndex { get; set; }
        public long AccessMethodPageIndex { get; set; }
        public int OnDiskStructureVersion { get; set; }
        public short AccessMethod { get; set; }

        public override SizeRange SizeRange
        {
            get { return base.SizeRange; }
            set 
            {
                if (value != SizeRange.NotApplicable)
                    throw new InvalidOperationException("SizeRange is not applicable to heading page.");

                base.SizeRange = value; 
            }
        }

        public override void Read(IPage page)
        {
            base.Read(page);

            PageSize = BitConverter.ToInt32(page.Content, OnPageOffsets.Heading.PageSize);
            FsmPageIndex = BitConverter.ToInt64(page.Content, OnPageOffsets.Heading.FsmPageIndex);
            AccessMethodPageIndex = BitConverter.ToInt64(page.Content, OnPageOffsets.Heading.AccessMethodPageIndex);
            OnDiskStructureVersion = BitConverter.ToInt32(page.Content, OnPageOffsets.Heading.OnDiskStructureVersion);
            AccessMethod = BitConverter.ToInt16(page.Content, OnPageOffsets.Heading.AccessMethod);
        }

        protected override PageType GetActualPageType()
        {
            return PageType.Heading;
        }

        public override void WriteToPage(IPage page)
        {
            Length = 30;
            base.WriteToPage(page);

            // write the on-disk version
            byte[] odsVersionBytes = BitConverter.GetBytes(OnDiskStructureVersion);
            odsVersionBytes.CopyTo(page.Content, OnPageOffsets.Heading.OnDiskStructureVersion);

            // write the access method
            byte[] accessMethodBytes = BitConverter.GetBytes(AccessMethod);
            accessMethodBytes.CopyTo(page.Content, OnPageOffsets.Heading.AccessMethod);

            // write the page size of the storage
            byte[] psBytes = BitConverter.GetBytes(PageSize);
            psBytes.CopyTo(page.Content, OnPageOffsets.Heading.PageSize);

            // write the index of the free-space-map page
            byte[] fsmPageIndexBytes = BitConverter.GetBytes(FsmPageIndex);
            fsmPageIndexBytes.CopyTo(page.Content, OnPageOffsets.Heading.FsmPageIndex);

            // write the index of the access method page
            byte[] accessMethodPageIndexBytes = BitConverter.GetBytes(AccessMethodPageIndex);
            accessMethodPageIndexBytes.CopyTo(page.Content, OnPageOffsets.Heading.AccessMethodPageIndex);
        }

        public HeadingPageHeader()
        {
            base.SizeRange = SizeRange.NotApplicable;
        }
    }
}