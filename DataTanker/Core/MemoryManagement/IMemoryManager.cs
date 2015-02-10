namespace DataTanker.MemoryManagement
{
    /// <summary>
    /// Provides methods and properties for storage memory management.
    /// </summary>
    internal interface IMemoryManager
    {
        /// <summary>
        /// Allocates new db item with specified content and produces reference to it.
        /// </summary>
        /// <param name="content">Content of item to allocate</param>
        /// <returns>Reference to the allocated item</returns>
        DbItemReference Allocate(byte[] content);

        /// <summary>
        /// Releases db item by its reference
        /// </summary>
        /// <param name="reference">Reference to item to release</param>
        void Free(DbItemReference reference);

        /// <summary>
        /// Reallocates already allocated db item with specified content and produces reference to it.
        /// </summary>
        /// <param name="reference">Reference to already allocated item</param>
        /// <param name="newContent">Content of item to reallocate</param>
        /// <returns>Reference to the reallocated item</returns>
        DbItemReference Reallocate(DbItemReference reference, byte[] newContent);

        /// <summary>
        /// Gets DbItem instance by reference.
        /// </summary>
        /// <param name="reference">Reference to the requested db item</param>
        /// <returns></returns>
        DbItem Get(DbItemReference reference);

        /// <summary>
        /// Gets the db item length (in bytes).
        /// </summary>
        /// <param name="reference">Reference to the requested db item</param>
        /// <returns>The length of requested db item</returns>
        long GetLength(DbItemReference reference);

        /// <summary>
        /// Gets the segment of binary representation of db item.
        /// </summary>
        /// <param name="reference">Reference to the db item</param>
        /// <param name="startIndex">The start index in binary representation</param>
        /// <param name="endIndex">The end index in binary representation</param>
        /// <returns>The array of bytes containing specified segment of db item</returns>
        byte[] GetItemSegment(DbItemReference reference, long startIndex, long endIndex);
    }
}