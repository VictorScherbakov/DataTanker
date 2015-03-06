namespace DataTanker.Settings
{
    using System;

    /// <summary>
    /// Represents settings specific to the BPlusTree access.
    /// </summary>
    public sealed class BPlusTreeStorageSettings : StorageSettingsBase
    {
        /// <summary>
        /// Gets or sets the maximal length of key (in bytes)
        /// </summary>
        public int MaxKeySize { get; set; }

        /// <summary>
        /// Gets the default storage settings with specified maximal key size.
        /// </summary>
        /// <param name="maxKeySize">Maximal key size</param>
        /// <returns>Default storage settings</returns>
        public static BPlusTreeStorageSettings Default(int maxKeySize)
        {
            var settings = Default();
            settings.MaxKeySize = maxKeySize;
            return settings;
        }

        /// <summary>
        /// Gets the default storage settings:
        /// CacheSettings = new CacheSettings
        ///     {
        ///         MaxCachedPages = 3000,
        ///         MaxDirtyPages = 1000
        ///     },
        /// PageSize = PageSize.Default,
        /// ForcedWrites = false,
        /// MaxKeySize = 500,
        /// MaxEmptyPages = 100,
        /// AutoFlushInterval = 10000,
        /// AutoFlushTimeout = TimeSpan.Zero
        /// </summary>
        /// <returns>Default storage settings</returns>
        public static BPlusTreeStorageSettings Default()
        {
            return new BPlusTreeStorageSettings
            {
                CacheSettings = new CacheSettings
                {
                    MaxCachedPages = 3000,
                    MaxDirtyPages = 1000
                },
                PageSize = PageSize.Default,
                ForcedWrites = false,
                MaxKeySize = 500,
                MaxEmptyPages = 100,
                AutoFlushInterval = 100000,
                AutoFlushTimeout = TimeSpan.Zero
            };
        }
    }
}