namespace DataTanker.Settings
{
    using System;

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
        /// Gets the value indicating whether all write operations perform immediately to file storage.
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

        /// <summary>
        /// Gets or sets a value defining a number of storage write operations (inserts, deletes and updates) 
        /// to allow before forcing the changes to write out to main storage file by calling Flush().
        /// If this property is set to zero or negative value flush is performed in cases of expired 
        /// AutoFlushTimeout and explicit calls of the Flush method.
        /// </summary>
        public int AutoFlushInterval { get; set; }

        /// <summary>
        /// Gets or sets a timeout after which the changes made by write operations (inserts, deletes and updates) 
        /// are written out to main storage file by calling Flush().
        /// New countdown starts after each writing transaction and old one is canceled.
        /// To disable timeout based flushing, set this property to zero or negative value.
        /// </summary>
        public TimeSpan AutoFlushTimeout { get; set; }
    }
}
