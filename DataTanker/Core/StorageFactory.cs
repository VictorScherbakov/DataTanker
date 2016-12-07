namespace DataTanker
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Settings;
    using PageManagement;
    using MemoryManagement;

    using AccessMethods;
    using AccessMethods.BPlusTree;
    using AccessMethods.BPlusTree.Storage;
    using AccessMethods.RadixTree;
    using AccessMethods.RadixTree.Storage;

    /// <summary>
    /// Class for create storage instances.
    /// </summary>
    public class StorageFactory : IStorageFactory 
    {
        private static Dictionary<Type, object> BuiltInKeySerializers()
        {
            return new Dictionary<Type, object>
            {
                  { typeof (int), new Serializer<Int32>(obj => BitConverter.GetBytes((Int32)obj), bytes => BitConverter.ToInt32(bytes, 0)) },
                  { typeof (long), new Serializer<Int64>(BitConverter.GetBytes, bytes => BitConverter.ToInt64(bytes, 0)) },
                  { typeof (uint), new Serializer<UInt32>(BitConverter.GetBytes, bytes => BitConverter.ToUInt32(bytes, 0)) },
                  { typeof (ulong), new Serializer<UInt64>(BitConverter.GetBytes, bytes => BitConverter.ToUInt64(bytes, 0)) },
                  { typeof (double), new Serializer<Double>(BitConverter.GetBytes, bytes => BitConverter.ToDouble(bytes, 0)) },
                  { typeof (float), new Serializer<Single>(BitConverter.GetBytes, bytes => BitConverter.ToSingle(bytes, 0)) },
                  { typeof (DateTime), new Serializer<DateTime>(dt => BitConverter.GetBytes(dt.ToBinary()), bytes => DateTime.FromBinary(BitConverter.ToInt64(bytes, 0))) },
                  { typeof (Guid), new Serializer<Guid>(guid => guid.ToByteArray(), bytes => new Guid(bytes)) },
                  { typeof (string), new Serializer<String>(Encoding.UTF8.GetBytes, bytes => Encoding.UTF8.GetString(bytes)) },
                  { typeof (byte[]), new Serializer<byte[]>(obj => obj, bytes => bytes) },
            };
        }

        #region IStorageFactory Members

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="serializeKey">Key serialization method</param>
        /// <param name="deserializeKey">Key deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<byte[]>> CreateBPlusTreeByteArrayStorage<TKey>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            BPlusTreeStorageSettings settings)

            where TKey : IComparable
        {
            return CreateBPlusTreeStorage(serializeKey, deserializeKey, p => p, p => p, settings);
        }

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys using built-in serialization routines for keys.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<byte[]>> CreateBPlusTreeByteArrayStorage<TKey>(
            BPlusTreeStorageSettings settings)

            where TKey : IComparable
        {
            return CreateBPlusTreeStorage<TKey, byte[]>(p => p, p => p, settings);
        }

        /// <summary>
        /// Creates a new storage instance with specified type of keys and values using built-in serialization routines for keys.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creraating storage</param>
        /// <returns>New storage instance</returns>
        public IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(ISerializer<TValue> valueSerializer, BPlusTreeStorageSettings settings)
            where TKey : IComparable
        {
            var keys = BuiltInKeySerializers();
            var type = typeof (TKey);
            if (!keys.ContainsKey(type))
                ThrowNotSupportedType(type, nameof(CreateBPlusTreeStorage));

            return CreateBPlusTreeStorage((ISerializer<TKey>) keys[type], valueSerializer, settings);
        }

        /// <summary>
        /// Creates a new storage instance with specified type of keys and values using built-in serialization routines for keys
        /// and BinaryFormatter serialization for values.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="settings">Setiings of creraating storage</param>
        /// <returns>New storage instance</returns>
        public IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(BPlusTreeStorageSettings settings)
            where TKey : IComparable
        {
            var keys = BuiltInKeySerializers();
            var type = typeof(TKey);
            if (!keys.ContainsKey(type))
                ThrowNotSupportedType(type, nameof(CreateBPlusTreeStorage));

            return CreateBPlusTreeStorage((ISerializer<TKey>) keys[type], new NativeSerializer<TValue>(), settings);
        }

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
        public IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(Func<TValue, byte[]> serializeValue,
                                                                                                             Func<byte[], TValue> deserializeValue,
                                                                                                             BPlusTreeStorageSettings settings)
            where TKey : IComparable
        {
            var keys = BuiltInKeySerializers();
            var type = typeof(TKey);
            if (!keys.ContainsKey(type))
                ThrowNotSupportedType(type, nameof(CreateBPlusTreeStorage));

            return CreateBPlusTreeStorage((ISerializer<TKey>)keys[type], new Serializer<TValue>(serializeValue, deserializeValue), settings);
        }

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
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(
            Func<TValue, byte[]> serializeValue,
            Func<byte[], TValue> deserializeValue,
            RadixTreeStorageSettings settings)
        {
            var keys = BuiltInKeySerializers();
            var type = typeof(TKey);
            if (!keys.ContainsKey(type))
                ThrowNotSupportedType(type, nameof(CreateRadixTreeStorage));

            return CreateRadixTreeStorage((ISerializer<TKey>)keys[type], new Serializer<TValue>(serializeValue, deserializeValue), settings);
        }

        /// <summary>
        /// Creates a new storage instance with specified types of keys using built-in serialization routines.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(ISerializer<TValue> valueSerializer, RadixTreeStorageSettings settings)
        {
            var keys = BuiltInKeySerializers();
            var type = typeof(TKey);
            if (!keys.ContainsKey(type))
                ThrowNotSupportedType(type, nameof(CreateRadixTreeStorage));

            return CreateRadixTreeStorage((ISerializer<TKey>)keys[type], valueSerializer, settings);
        }

        /// <summary>
        /// Creates a new storage instance with specified types of keys using built-in serialization routines.
        /// and BinaryFormatter serialization for values.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(RadixTreeStorageSettings settings)
        {
            var keys = BuiltInKeySerializers();
            var type = typeof(TKey);
            if (!keys.ContainsKey(type))
                ThrowNotSupportedType(type, nameof(CreateRadixTreeStorage));

            return CreateRadixTreeStorage((ISerializer<TKey>)keys[type], new NativeSerializer<TValue>(), settings);
        }

        private static void ThrowNotSupportedType(Type type, string methodName)
        {
            throw new NotSupportedException($"Key of type {type} has no built-in serializer. Provide one using {methodName} method with other parameters");
        }

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="serializeKey">Key serialization method</param>
        /// <param name="deserializeKey">Key deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<byte[]>> CreateRadixTreeByteArrayStorage<TKey>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            RadixTreeStorageSettings settings)
        {
            return CreateRadixTreeStorage(serializeKey, deserializeKey, p => p, p => p, settings);
        }

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys using built-in serialization routines.
        /// Supported types of keys are: int, long, uint, ulong, double, float, DateTime, Guid, string and byte[]
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<byte[]>> CreateRadixTreeByteArrayStorage<TKey>(
            RadixTreeStorageSettings settings)
        {
            return CreateRadixTreeStorage<TKey, byte[]>(p => p, p => p, settings);
        }

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
        public IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            Func<TValue, byte[]> serializeValue,
            Func<byte[], TValue> deserializeValue,
            BPlusTreeStorageSettings settings)

            where TKey : IComparable
        {
            if (serializeKey == null) 
                throw new ArgumentNullException(nameof(serializeKey));

            if (deserializeKey == null) 
                throw new ArgumentNullException(nameof(deserializeKey));

            if (serializeValue == null) 
                throw new ArgumentNullException(nameof(serializeValue));

            if (deserializeValue == null) 
                throw new ArgumentNullException(nameof(deserializeValue));

            return CreateBPlusTreeStorage(new Serializer<TKey>(serializeKey, deserializeKey),
                          new Serializer<TValue>(serializeValue, deserializeValue), settings);
        }

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
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            Func<TValue, byte[]> serializeValue,
            Func<byte[], TValue> deserializeValue,
            RadixTreeStorageSettings settings)
        {
            if (serializeKey == null)
                throw new ArgumentNullException(nameof(serializeKey));

            if (deserializeKey == null)
                throw new ArgumentNullException(nameof(deserializeKey));

            if (serializeValue == null)
                throw new ArgumentNullException(nameof(serializeValue));

            if (deserializeValue == null)
                throw new ArgumentNullException(nameof(deserializeValue));

            return CreateRadixTreeStorage(new Serializer<TKey>(serializeKey, deserializeKey),
                          new Serializer<TValue>(serializeValue, deserializeValue), settings);
        }

        /// <summary>
        /// Creates a new storage instance with specified types of keys and values.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="keySerializer">Object implementing ISerializer interface for key serialization</param>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IBPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(ISerializer<TKey> keySerializer, 
                                                                                                             ISerializer<TValue> valueSerializer, 
                                                                                                             BPlusTreeStorageSettings settings)
            where TKey : IComparable
        {
            bool usePageCache = settings.CacheSettings != null;

            if(settings.MaxEmptyPages < 0)
                throw new ArgumentException("MaxEmptyPages shouldn't be negative", nameof(settings));

            if (usePageCache)
            {
                if(settings.CacheSettings.MaxCachedPages < 0)
                    throw new ArgumentException("MaxCachedPages shouldn't be negative", nameof(settings));

                if (settings.CacheSettings.MaxDirtyPages < 0)
                    throw new ArgumentException("MaxDirtyPages shouldn't be negative", nameof(settings));

                if (settings.CacheSettings.MaxDirtyPages > settings.CacheSettings.MaxCachedPages)
                    throw new ArgumentException("MaxDirtyPages shouldn be equal to or less than MaxCachedPages", nameof(settings));
            }

            IPageManager pageManager = null;
            IPageManager fsPageManager = null;
            try
            {
                var asyncWriteBuffer = usePageCache ? Math.Min(settings.CacheSettings.MaxDirtyPages, 1000) : 100;

                fsPageManager = new FileSystemPageManager((int)settings.PageSize, settings.ForcedWrites, asyncWriteBuffer, true) { MaxEmptyPages = settings.MaxEmptyPages };

                pageManager = usePageCache ?
                                new CachingPageManager(fsPageManager, settings.CacheSettings.MaxCachedPages, settings.CacheSettings.MaxDirtyPages)
                                : fsPageManager;

                var ks = new Serializer<ComparableKeyOf<TKey>>(obj => keySerializer.Serialize(obj), bytes => keySerializer.Deserialize(bytes));
                var vs = new Serializer<ValueOf<TValue>>(obj => valueSerializer.Serialize(obj), bytes => valueSerializer.Deserialize(bytes));

                if (settings.MaxKeySize <= 0)
                    throw new ArgumentException("MaxKeySize size should be positive", nameof(settings));

                var bPlusTree = new BPlusTree<ComparableKeyOf<TKey>, ValueOf<TValue>>(
                    new BPlusTreeNodeStorage<ComparableKeyOf<TKey>>(pageManager, ks, settings.MaxKeySize),
                    new ValueStorage<ValueOf<TValue>>(new MemoryManager(new FreeSpaceMap(pageManager), pageManager), vs));

                return new BPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>>(pageManager, bPlusTree, settings.MaxKeySize, settings.AutoFlushInterval, settings.AutoFlushTimeout);
            }
            catch (Exception)
            {
                if (pageManager != null)    
                    pageManager.Close();
                else
                    fsPageManager?.Close();

                throw;
            }
        }

        /// <summary>
        /// Creates a new storage instance with specified types of keys and values.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="keySerializer">Object implementing ISerializer interface for key serialization</param>
        /// <param name="valueSerializer">Object implementing ISerializer interface for value serialization</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(ISerializer<TKey> keySerializer, 
                                                                                                   ISerializer<TValue> valueSerializer, 
                                                                                                   RadixTreeStorageSettings settings)
        {
            bool usePageCache = settings.CacheSettings != null;

            if (settings.MaxEmptyPages < 0)
                throw new ArgumentException("MaxEmptyPages shouldn't be negative", nameof(settings));

            if (usePageCache)
            {
                if (settings.CacheSettings.MaxCachedPages < 0)
                    throw new ArgumentException("MaxCachedPages shouldn't be negative", nameof(settings));

                if (settings.CacheSettings.MaxDirtyPages < 0)
                    throw new ArgumentException("MaxDirtyPages shouldn't be negative", nameof(settings));

                if (settings.CacheSettings.MaxDirtyPages > settings.CacheSettings.MaxCachedPages)
                    throw new ArgumentException("MaxDirtyPages shouldn be equal to or less than MaxCachedPages", nameof(settings));
            }

            IPageManager pageManager = null;
            IPageManager fsPageManager = null;
            try
            {
                var asyncWriteBuffer = usePageCache ? Math.Min(settings.CacheSettings.MaxDirtyPages, 1000) : 100;

                fsPageManager = new FileSystemPageManager((int)settings.PageSize, settings.ForcedWrites, asyncWriteBuffer, true) { MaxEmptyPages = settings.MaxEmptyPages };

                pageManager = usePageCache ?
                                new CachingPageManager(fsPageManager, settings.CacheSettings.MaxCachedPages, settings.CacheSettings.MaxDirtyPages)
                                : fsPageManager;

                var ks = new Serializer<KeyOf<TKey>>(obj => keySerializer.Serialize(obj), bytes => keySerializer.Deserialize(bytes));
                var vs = new Serializer<ValueOf<TValue>>(obj => valueSerializer.Serialize(obj), bytes => valueSerializer.Deserialize(bytes));

                var radixTree = new RadixTree<KeyOf<TKey>, ValueOf<TValue>>(
                    new RadixTreeNodeStorage(pageManager),
                    new ValueStorage<ValueOf<TValue>>(new MemoryManager(new FreeSpaceMap(pageManager), pageManager), vs), ks);

                return new RadixTreeKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>>(pageManager, radixTree, settings.AutoFlushInterval, settings.AutoFlushTimeout);

            }
            catch (Exception)
            {
                if (pageManager != null)
                    pageManager.Close();
                else
                    fsPageManager?.Close();

                throw;
            }
        }

        #endregion
    }
}