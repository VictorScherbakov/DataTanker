namespace DataTanker
{
    /// <summary>
    /// Contains methods for accessing a key-value storage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys</typeparam>
    /// <typeparam name="TValue">The type of values</typeparam>
    public interface IKeyValueStorage<in TKey, TValue> : IStorage
        where TKey : IKey
        where TValue : IValue
    {
        /// <summary>
        /// Gets a value from storage.
        /// </summary>
        /// <param name="key">ComparableComparableKeyOf value</param>
        /// <returns>Requested value</returns>
        TValue Get(TKey key);

        /// <summary>
        /// Inserts a new value to storage or updates an existing one.
        /// </summary>
        /// <param name="key">ComparableComparableKeyOf value</param>
        /// <param name="value">ValueOf value :)</param>
        void Set(TKey key, TValue value);

        /// <summary>
        /// Removes a value from storage by its key
        /// </summary>
        /// <param name="key">The key of value to remove</param>
        void Remove(TKey key);

        /// <summary>
        /// Cheks if key-value pair exists in storage.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if key-value pair exists, false otherwise</returns>
        bool Exists(TKey key);

        /// <summary>
        /// Retrieves the length (in bytes) of binary representation
        /// of the value referenced by the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The length of binary representation</returns>
        long GetRawDataLength(TKey key);

        /// <summary>
        /// Retrieves a segment of binary representation
        /// of the value referenced by the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="startIndex">The index in binary representation where the specified segment starts</param>
        /// <param name="endIndex">The index in binary representation where the specified segment ends</param>
        /// <returns></returns>
        byte[] GetRawDataSegment(TKey key, long startIndex, long endIndex);
    }
}