namespace DataTanker.Settings
{
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
        /// MaxEmptyPages = 100
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
                MaxEmptyPages = 100
            };
        }
    }
}