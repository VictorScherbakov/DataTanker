namespace DataTanker.Settings
{
    using System;

    /// <summary>
    /// Represents settings specific to the RadixTree access.
    /// </summary>
    public sealed class RadixTreeStorageSettings : StorageSettingsBase
    {
        /// <summary>
        /// Gets the default storage settings:
        /// CacheSettings = new CacheSettings
        ///     {
        ///         MaxCachedPages = 3000,
        ///         MaxDirtyPages = 1000
        ///     },
        /// PageSize = PageSize.Default,
        /// ForcedWrites = false,
        /// MaxEmptyPages = 100,
        /// AutoFlushInterval = 10000,
        /// AutoFlushTimeout = TimeSpan.Zero
        /// </summary>
        /// <returns>Default storage settings</returns>
        public static RadixTreeStorageSettings Default()
        {
            return new RadixTreeStorageSettings
            {
                CacheSettings = new CacheSettings
                {
                    MaxCachedPages = 3000,
                    MaxDirtyPages = 1000
                },
                PageSize = PageSize.Default,
                ForcedWrites = false,
                MaxEmptyPages = 100,
                AutoFlushInterval = 100000,
                AutoFlushTimeout = TimeSpan.Zero
            };
        }
    }
}