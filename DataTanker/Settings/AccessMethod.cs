namespace DataTanker.Settings
{
    /// <summary>
    /// Defines methods for access storage provided by DataTanker
    /// </summary>
    public enum AccessMethod : short
    {
        /// <summary>
        /// Access to the storage is undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Access to the storage via B+Tree.
        /// </summary>
        BPlusTree = 1,

        /// <summary>
        /// Access to the storage via Radix Tree.
        /// </summary>
        RadixTree = 2
    }
}
