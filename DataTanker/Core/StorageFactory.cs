namespace DataTanker
{
    using System;

    using Settings;
    using PageManagement;
    using MemoryManagement;

    using AccessMethods.BPlusTree;
    using AccessMethods.BPlusTree.Storage;
    using AccessMethods.RadixTree;
    using AccessMethods.RadixTree.Storage;

    /// <summary>
    /// Class for create storage instances.
    /// </summary>
    public class StorageFactory : IStorageFactory 
    {
        #region IStorageFactory Members

        /// <summary>
        /// Creates a new bytearray storage instance with specified type of keys.
        /// </summary>
        /// <typeparam name="TKey">The type of key</typeparam>
        /// <param name="serializeKey">Key serialization method</param>
        /// <param name="deserializeKey">Key deserialization method</param>
        /// <param name="settings">Setiings of creating storage</param>
        /// <returns>New storage instance</returns>
        public IKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<byte[]>> CreateBPlusTreeByteArrayStorage<TKey>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            BPlusTreeStorageSettings settings)

            where TKey : IComparable
        {
            return CreateBPlusTreeStorage(serializeKey, deserializeKey, p => p, p => p, settings);
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
        public IKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(
            Func<TKey, byte[]> serializeKey,
            Func<byte[], TKey> deserializeKey,
            Func<TValue, byte[]> serializeValue,
            Func<byte[], TValue> deserializeValue,
            BPlusTreeStorageSettings settings)

            where TKey : IComparable
        {
            if (serializeKey == null) 
                throw new ArgumentNullException("serializeKey");

            if (deserializeKey == null) 
                throw new ArgumentNullException("deserializeKey");

            if (serializeValue == null) 
                throw new ArgumentNullException("serializeValue");

            if (deserializeValue == null) 
                throw new ArgumentNullException("deserializeValue");

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
                throw new ArgumentNullException("serializeKey");

            if (deserializeKey == null)
                throw new ArgumentNullException("deserializeKey");

            if (serializeValue == null)
                throw new ArgumentNullException("serializeValue");

            if (deserializeValue == null)
                throw new ArgumentNullException("deserializeValue");

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
        public IKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>> CreateBPlusTreeStorage<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, BPlusTreeStorageSettings settings)
            where TKey : IComparable
        {
            bool usePageCache = settings.CacheSettings != null;

            if(settings.MaxEmptyPages < 0)
                throw new ArgumentException("MaxEmptyPages shouldn't be negative", "settings");

            if (usePageCache)
            {
                if(settings.CacheSettings.MaxCachedPages < 0)
                    throw new ArgumentException("MaxCachedPages shouldn't be negative", "settings");

                if (settings.CacheSettings.MaxDirtyPages < 0)
                    throw new ArgumentException("MaxDirtyPages shouldn't be negative", "settings");

                if (settings.CacheSettings.MaxDirtyPages > settings.CacheSettings.MaxCachedPages)
                    throw new ArgumentException("MaxDirtyPages shouldn be equal to or less than MaxCachedPages", "settings");
            }

            IPageManager pageManager = null;
            IPageManager fsPageManager = null;
            try
            {
                var asyncWriteBuffer = usePageCache ? Math.Min(settings.CacheSettings.MaxDirtyPages, 1000) : 100;

                fsPageManager = new FileSystemPageManager((int)settings.PageSize, settings.ForcedWrites, asyncWriteBuffer) { MaxEmptyPages = settings.MaxEmptyPages };

                pageManager = usePageCache ?
                                new CachingPageManager(fsPageManager, settings.CacheSettings.MaxCachedPages, settings.CacheSettings.MaxDirtyPages)
                                : fsPageManager;

                var ks = new Serializer<ComparableKeyOf<TKey>>(obj => keySerializer.Serialize(obj), bytes => keySerializer.Deserialize(bytes));
                var vs = new Serializer<ValueOf<TValue>>(obj => valueSerializer.Serialize(obj), bytes => valueSerializer.Deserialize(bytes));

                if (settings.MaxKeySize <= 0)
                    throw new ArgumentException("MaxKeySize size should be positive", "settings");

                var bPlusTree = new BPlusTree<ComparableKeyOf<TKey>, ValueOf<TValue>>(
                    new BPlusTreeNodeStorage<ComparableKeyOf<TKey>>(pageManager, ks, settings.MaxKeySize),
                    new ValueStorage<ValueOf<TValue>>(new MemoryManager(new FreeSpaceMap(pageManager), pageManager), vs));

                return new BPlusTreeKeyValueStorage<ComparableKeyOf<TKey>, ValueOf<TValue>>(pageManager, bPlusTree, settings.MaxKeySize);
            }
            catch (Exception)
            {
                if (pageManager != null)    
                    pageManager.Close();
                else if(fsPageManager != null)
                    fsPageManager.Close();

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
        public IKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>> CreateRadixTreeStorage<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, RadixTreeStorageSettings settings)
        {
            bool usePageCache = settings.CacheSettings != null;

            if (settings.MaxEmptyPages < 0)
                throw new ArgumentException("MaxEmptyPages shouldn't be negative", "settings");

            if (usePageCache)
            {
                if (settings.CacheSettings.MaxCachedPages < 0)
                    throw new ArgumentException("MaxCachedPages shouldn't be negative", "settings");

                if (settings.CacheSettings.MaxDirtyPages < 0)
                    throw new ArgumentException("MaxDirtyPages shouldn't be negative", "settings");

                if (settings.CacheSettings.MaxDirtyPages > settings.CacheSettings.MaxCachedPages)
                    throw new ArgumentException("MaxDirtyPages shouldn be equal to or less than MaxCachedPages", "settings");
            }

            IPageManager pageManager = null;
            IPageManager fsPageManager = null;
            try
            {
                var asyncWriteBuffer = usePageCache ? Math.Min(settings.CacheSettings.MaxDirtyPages, 1000) : 100;

                fsPageManager = new FileSystemPageManager((int)settings.PageSize, settings.ForcedWrites, asyncWriteBuffer) { MaxEmptyPages = settings.MaxEmptyPages };

                pageManager = usePageCache ?
                                new CachingPageManager(fsPageManager, settings.CacheSettings.MaxCachedPages, settings.CacheSettings.MaxDirtyPages)
                                : fsPageManager;

                var ks = new Serializer<KeyOf<TKey>>(obj => keySerializer.Serialize(obj), bytes => keySerializer.Deserialize(bytes));
                var vs = new Serializer<ValueOf<TValue>>(obj => valueSerializer.Serialize(obj), bytes => valueSerializer.Deserialize(bytes));

                var radixTree = new RadixTree<KeyOf<TKey>, ValueOf<TValue>>(
                    new RadixTreeNodeStorage(pageManager),
                    new ValueStorage<ValueOf<TValue>>(new MemoryManager(new FreeSpaceMap(pageManager), pageManager), vs), ks);

                return new RadixTreeKeyValueStorage<KeyOf<TKey>, ValueOf<TValue>>(pageManager, radixTree);

            }
            catch (Exception)
            {
                if (pageManager != null)
                    pageManager.Close();
                else if (fsPageManager != null)
                    fsPageManager.Close();

                throw;
            }
        }

        #endregion
    }
}