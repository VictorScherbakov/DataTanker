namespace DataTanker.BinaryFormat.Page
{
    using System;

    using PageManagement;

    internal class MultipageItemPageHeaderBase : ItemsPageHeader
    {
        public long StartPageIndex { get; set; }
        public long PreviousPageIndex { get; set; }
        public long NextPageIndex { get; set; }

        public override SizeRange SizeRange
        {
            get { return base.SizeRange; }
            set
            {
                if (value != SizeRange.MultiPage)
                    throw new InvalidOperationException("Invalid SizeRange.");

                base.SizeRange = value;
            }
        }

        public override void Read(IPage page)
        {
            base.Read(page);

            StartPageIndex = BitConverter.ToInt64(page.Content, OnPageOffsets.MultipageItem.StartPageIndex);
            PreviousPageIndex = BitConverter.ToInt64(page.Content, OnPageOffsets.MultipageItem.PreviousPageIndex);
            NextPageIndex = BitConverter.ToInt64(page.Content, OnPageOffsets.MultipageItem.NextPageIndex);
        }

        protected override PageType GetActualPageType()
        {
            return PageType.MultipageItem;
        }

        public override void WriteToPage(IPage page)
        {
            base.WriteToPage(page);

            // start page index
            byte[] spiBytes = BitConverter.GetBytes(StartPageIndex);
            spiBytes.CopyTo(page.Content, OnPageOffsets.MultipageItem.StartPageIndex);

            // previous page index
            byte[] ppiBytes = BitConverter.GetBytes(PreviousPageIndex);
            ppiBytes.CopyTo(page.Content, OnPageOffsets.MultipageItem.PreviousPageIndex);

            // next page index
            byte[] npiBytes = BitConverter.GetBytes(NextPageIndex);
            npiBytes.CopyTo(page.Content, OnPageOffsets.MultipageItem.NextPageIndex);
        }

        public MultipageItemPageHeaderBase()
        {
            base.SizeRange = SizeRange.MultiPage;
        }
    }
}