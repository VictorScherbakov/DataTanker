namespace DataTanker.PageManagement
{
    /// <summary>
    /// Wraps a page for caching purposes.
    /// This instances can contain information about hits, changes etc.
    /// </summary>
    internal class PageCacheEntry
    {
        /// <summary>
        /// Gets or sets caching page instance.
        /// </summary>
        public IPage Page { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a page is updated and not yet saved to disk.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Gets or sets a hit count.
        /// </summary>
        public int HitCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a page is pinned.
        /// Pinned pages cannot be writen out to disk.
        /// </summary>
        public bool Pinned { get; set; }
    }
}