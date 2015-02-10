namespace DataTanker.BinaryFormat.Page
{
    using PageManagement;

    internal sealed class MultipageItemPageHeader : MultipageItemPageHeaderBase
    {
        public override void WriteToPage(IPage page)
        {
            Length = 28;
            base.WriteToPage(page);
        }
    }
}