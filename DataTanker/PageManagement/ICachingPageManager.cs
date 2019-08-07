namespace DataTanker.PageManagement
{
    /// <summary>
    /// Provides methods and properties for page-level caching.
    /// </summary>
    internal interface ICachingPageManager : IPageManager
    {
        /// <summary>
        /// Force writing of all updated pages to underlying page manager.
        /// </summary>
        void Flush();

        /// <summary>
        /// Pins the page
        /// </summary>
        void PinPage(long index);

        /// <summary>
        /// Unpins the page
        /// </summary>
        void UnpinPage(long index);
    }
}