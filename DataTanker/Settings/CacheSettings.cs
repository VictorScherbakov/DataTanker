namespace DataTanker.Settings
{
    /// <summary>
    /// Represents settings of cache.
    /// </summary>
    public class CacheSettings
    {
        /// <summary>
        /// Gets or sets the maximum number of cached pages (fetched and updated).
        /// </summary>
        public int MaxCachedPages { get; set; }

        /// <summary>
        /// Gets or sets a maximum number of updated pages.
        /// </summary>
        public int MaxDirtyPages { get; set; }
    }
}
