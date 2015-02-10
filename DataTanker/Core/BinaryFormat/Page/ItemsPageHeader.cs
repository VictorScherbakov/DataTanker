namespace DataTanker.BinaryFormat.Page
{
    using PageManagement;

    internal abstract class ItemsPageHeader : PageHeaderBase
    {
        public override void Read(IPage page)
        {
            base.Read(page);
        }

        public override void WriteToPage(IPage page)
        {
            base.WriteToPage(page);
        }
    }
}