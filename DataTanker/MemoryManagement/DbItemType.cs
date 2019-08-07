namespace DataTanker.MemoryManagement
{
    /// <summary>
    /// Defines possible types of db items.
    /// </summary>
    internal enum DbItemType
    { 
        /// <summary>
        /// Free space map entry.
        /// </summary>
        FreeSpaceMap,

        /// <summary>
        /// Node of index.
        /// </summary>
        IndexNode,

        /// <summary>
        /// General value.
        /// </summary>
        Data
    }
}