namespace DataTanker
{
    using System;
    using Settings;

    /// <summary>
    /// Provides methods to create storage instances.
    /// </summary>
    public interface IStorageFactory
    {
        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys using built-in serialization routines for keys.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<byte[]>> CreateBPlusTreeByteArrayStorage<TKey>(BPlusTreeStorageSettings settings)
            where TKey : IComparable;

        /// <summary>
        /// Creates a new storage instance with specified type of keys and values using built-in serialization routines for keys.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creraating storage</param>
        /// <returns>New storage instance</returns>
        IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(ISerializer<TValue> valueSerializer, 
                                                                                                      BPlusTreeStorageSettings settings)
            where TKey : IComparable;

        /// <summary>
        /// Creates a new storage instance with specified type of keys and values using built-in serialization routines for keys
        /// and BinaryFormatter serialization for values.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="settings">Setiings of creraating storage</param>
        /// <returns>New storage instance</returns>
        IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(BPlusTreeStorageSettings settings)
            where TKey : IComparable;

        /// <summary>
        /// Creates a new storage instance with specified type of keys using built-in serialization routines.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="serializeValue">Value serialization method</param>
        /// <param name="deserializeValue">Value deserialization method</param>
        /// <param name="settings">Setiings of creraating storage</param>
        /// <returns>New storage instance</returns>
        IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(Func<TValue, byte[]> serializeValue,
                                                                                                      Func<byte[], TValue> deserializeValue,
                                                                                                      BPlusTreeStorageSettings settings)
            where TKey : IComparable;

        /// <summary>
        /// Creates a new storage instance with specified types of keys using built-in serialization routines.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="serializeValue">Value serialization method</param>
        /// <param name="deserializeValue">Value deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(Func<TValue, byte[]> serializeValue,
                                                                                            Func<byte[], TValue> deserializeValue,
                                                                                            RadixTreeStorageSettings settings);

        /// <summary>
        /// Creates a new storage instance with specified types of keys using built-in serialization routines.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(ISerializer<TValue> valueSerializer, RadixTreeStorageSettings settings);

        /// <summary>
        /// Creates a new storage instance with specified types of keys using built-in serialization routines.
        /// and BinaryFormatter serialization for values.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(RadixTreeStorageSettings settings);

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys using built-in serialization routines.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IKeyValueStorage<KeyOf<TKey>, ValueOf<byte[]>> CreateRadixTreeByteArrayStorage<TKey>(RadixTreeStorageSettings settings);

        /// <summary>
        /// Creates a new storage instance with specified types of keys and values.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="keySerializer">Object implementing ISerializer interface for key serialization</param>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(
            ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, BPlusTreeStorageSettings settings)
            where TKey : IComparable;

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="serializeKey">Key serialization method</param>
        /// <param name="deserializeKey">Key deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IKeyValueStorage<KeyOf<TKey>, ValueOf<byte[]>> CreateRadixTreeByteArrayStorage<TKey>(
            Func<TKey, byte[]> serializeKey, Func<byte[], TKey> deserializeKey, RadixTreeStorageSettings settings);


        /// <summary>
        /// Creates a new storage instance with specified types of keys and values.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="serializeKey">Key serialization method</param>
        /// <param name="deserializeKey">Key deserialization method</param>
        /// <param name="serializeValue">Value serialization method</param>
        /// <param name="deserializeValue">Value deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            Func<TValue, byte[]> serializeValue,
            Func<byte[], TValue> deserializeValue,
            BPlusTreeStorageSettings settings)

            where TKey : IComparable;

        /// <summary>
        /// Creates a new storage instance with specified types of keys and values.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="serializeKey">Key serialization method</param>
        /// <param name="deserializeKey">Key deserialization method</param>
        /// <param name="serializeValue">Value serialization method</param>
        /// <param name="deserializeValue">Value deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            Func<TValue, byte[]> serializeValue,
            Func<byte[], TValue> deserializeValue,
            RadixTreeStorageSettings settings);

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="serializeKey">Key serialization method</param>
        /// <param name="deserializeKey">Key deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<byte[]>> CreateBPlusTreeByteArrayStorage<TKey>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            BPlusTreeStorageSettings settings)

            where TKey : IComparable;

        /// <summary>
        /// Creates a new storage instance with specified types of keys and values.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="keySerializer">Object implementing ISerializer interface for key serialization</param>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(
            ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, RadixTreeStorageSettings settings);
    }
}