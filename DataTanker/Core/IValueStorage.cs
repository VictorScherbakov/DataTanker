namespace DataTanker
{
    using MemoryManagement;

    /// <summary>
    /// Provides methods that performs storing values.
    /// </summary>
    internal interface IValueStorage<TValue>
        where TValue : IValue
    {
        /// <summary>
        /// Fetches value by its reference.
        /// </summary>
        /// <param name="reference">Reference to the value</param>
        /// <returns>The instance of value</returns>
        TValue Fetch(DbItemReference reference);

        /// <summary>
        /// Retreives the length of value (in bytes) by its reference.
        /// </summary>
        /// <param name="reference">Reference to the value</param>
        /// <returns>The length of referenced value</returns>
        long GetRawDataLength(DbItemReference reference);

        /// <summary>
        /// Gets the segment of binary representation of value.
        /// </summary>
        /// <param name="reference">Reference to the value</param>
        /// <param name="startIndex">The start index of binary representation</param>
        /// <param name="endIndex">The end index of binary representation</param>
        /// <returns>The array of bytes containing specified segment of value</returns>
        byte[] GetRawDataSegment(DbItemReference reference, long startIndex, long endIndex);

        /// <summary>
        /// Allocates a new value and produces reference to it.
        /// </summary>
        /// <param name="value">Value to allocate</param>
        /// <returns>Reference to allocated value</returns>
        DbItemReference AllocateNew(TValue value);

        /// <summary>
        /// Reallocates already allocated value and produces reference to it.
        /// </summary>
        /// <param name="reference">Reference to the already allocated value</param>
        /// <param name="newValue">New value to allocate</param>
        /// <returns>Reference to reallocated value</returns>
        DbItemReference Reallocate(DbItemReference reference, TValue newValue);

        /// <summary>
        /// Release allocated value.
        /// </summary>
        /// <param name="reference">Reference to allocated value</param>
        void Free(DbItemReference reference);

        /// <summary>
        /// Gets the value indicating whether the versioning mechanisms is enabled.
        /// </summary>
        bool IsVersioningEnabled { get; }
    }
}