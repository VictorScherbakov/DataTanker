namespace DataTanker
{
    /// <summary>
    /// Contains methods for accessing a key-value storage 
    /// baed on B+Tree.
    /// </summary>
    /// <typeparam name="TKey">The type of keys</typeparam>
    /// <typeparam name="TValue">The type of values</typeparam>
    public interface IBPlusTreeKeyValueStorage<TKey, TValue> : IKeyValueStorage<TKey, TValue> 
        where TKey : IComparableKey
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
        /// Computes the count of key-value pairs in storage.
        /// </summary>
        /// <returns>the count of key-value pairs</returns>
        long Count();
    }
}