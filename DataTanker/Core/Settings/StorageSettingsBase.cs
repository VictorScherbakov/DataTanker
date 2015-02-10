namespace DataTanker.Settings
{
    /// <summary>
    /// Represents the settings of storage.
    /// </summary>
    public abstract class StorageSettingsBase
    {
        /// <summary>
        /// Gets or sets size of the on-disk page (in bytes).
        /// </summary>
        public PageSize PageSize { get; set; }

        /// <summary>
        /// Gets or sets the page-level caching.
        /// Set this to null to disable caching.
        /// </summary>
        public CacheSettings CacheSettings { get; set; }

        /// <summary>
        /// Gets the value indicating whether all write operations perform immediatly to file storage.
        /// <remarks>
        /// Setting to true disables only OS file system buffer on write operations.
        /// This does not affect the page caching behavior. Disable caching if you don't need it
        /// or call IStorage.Flush() to control the durability of all updates.
        /// </remarks>
        /// </summary>
        public bool ForcedWrites { get; set; }

        /// <summary>
        /// Gets or sets the maximal number of released pages.
        /// Vacuum procedure starts immediately when this value is reached.
        /// </summary>
        public int MaxEmptyPages { get; set; }
    }
}
