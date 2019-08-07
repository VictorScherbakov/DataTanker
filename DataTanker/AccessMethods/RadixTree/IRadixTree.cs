namespace DataTanker.AccessMethods.RadixTree
{
    internal interface IRadixTree<TKey, TValue>
        where TKey : IKey
        where TValue : IValue
    {
        /// <summary>
        /// Gets the minimal key.
        /// </summary>
        /// <returns>The minimal key</returns>
        TKey Min();

        /// <summary>
        /// Gets the maximal key.
        /// </summary>
        /// <returns>The maximal key</returns>
        TKey Max();

        /// <summary>
        /// Determines if the specified key has subkeys.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool HasSubkeys(TKey key);

        /// <summary>
        /// Gets the key previous to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key previous to specified key</returns>
        TKey PreviousTo(TKey key);

        /// <summary>
        /// Gets the key next to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key next to specified key</returns>
        TKey NextTo(TKey key);

        /// <summary>
        /// Gets the value by its key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The value corresponding to the given key</returns>
        TValue Get(TKey key);

        /// <summary>
        /// Gets a value corresponing to the minimal key.
        /// </summary>
        /// <returns>The value corresponding to the minimal key</returns>
        TValue MinValue();

        /// <summary>
        /// Gets the value corresponing to the maximal key.
        /// </summary>
        /// <returns>The value corresponding to the maximal key</returns>
        TValue MaxValue();

        /// <summary>
        /// Removes key-value pair by key.
        /// </summary>
        /// <param name="key">The key</param>
        void Remove(TKey key);

        /// <summary>
        /// Inserts or updates key value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        void Set(TKey key, TValue value);

        /// <summary>
        /// Checks if key-value pair exists in tree.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if key-value pair exists, false otherwise</returns>
        bool Exists(TKey key);

        /// <summary>
        /// Computes the count of key-value pairs in tree.
        /// </summary>
        /// <returns>the count of key-value pairs</returns>
        long Count();

        /// <summary>
        /// Computes the number of child key-value pairs for a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        long SubkeysCount(TKey key);

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

        /// <summary>
        /// Checks the tree for consistency.
        /// </summary>
        /// <param name="message">Diagnostic message describing the specific inconsistencies</param>
        /// <returns>True if the tree is consisternt, false otherwise</returns>
        bool CheckConsistency(out string message);

        /// <summary>
        /// Gets the maximal length of prefix.
        /// </summary>
        int MaxPrefixLength { get; }
    }
}