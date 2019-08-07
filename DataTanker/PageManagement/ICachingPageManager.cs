namespace DataTanker.PageManagement
{
    /// <summary>
    /// Provides methods and properties for page-level caching.
    /// </summary>
    internal interface ICachingPageManager : IPageManager
    {
        /// <summary>
        /// Gets a maximum number of cached pages (fetched and updated).
        /// </summary>
        long MaxCachedPages { get; }

        /// <summary>
        /// Gets or sets a maximum number of updated pages.
        /// </summary>
        long MaxDirtyPages { get; }

        /// <summary>
        /// Force writing of all updated pages to underlying page manager.
        /// </summary>
        void Flush();

        /// <summary>
        /// Gets a value (from zero to one) indicating efficiency of caching.
        /// This value is calculated as: SuccessfulOperations / AllOperations.
        /// </summary>
        double SuccessRate { get; }

        /// <summary>
        /// Resets the statistics of successful operations.
        /// </summary>
        void ResetStatistics();

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