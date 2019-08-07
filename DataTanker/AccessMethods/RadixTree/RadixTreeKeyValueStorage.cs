namespace DataTanker.AccessMethods.RadixTree
{
    using System;
    using System.Diagnostics;

    using BinaryFormat.Page;
    using PageManagement;
    using Settings;

    /// <summary>
    /// Key-value storage based on Radix Tree
    /// </summary>
    /// <typeparam name="TValue">The type of values</typeparam>
    /// <typeparam name="TKey">The type of keys</typeparam>
    internal class RadixTreeKeyValueStorage<TKey, TValue> : TransactionalStorage, IRadixTreeKeyValueStorage<TKey, TValue>
        where TKey : IKey
        where TValue : IValue
    {
        private readonly IRadixTree<TKey, TValue> _tree;

        private TReturnValue WrapWithReadLock<TReturnValue>(Func<TKey, TReturnValue> method, TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            EnterReadWrap();
            try
            {
                return method(key);
            }
            finally
            {
                ExitReadWrap();
            }
        }

        private TReturnValue WrapWithReadLock<TReturnValue>(Func<TReturnValue> method)
        {
            EnterReadWrap();
            try
            {
                return method();
            }
            finally
            {
                ExitReadWrap();
            }
        }

        private TKey WrapWithReadLock(Func<TKey> method)
        {
            EnterReadWrap();
            try
            {
                return method();
            }
            finally
            {
                ExitReadWrap();
            }
        }

        protected override void Init()
        {
            base.Init();

            //add access-method page
            IPage amPage = PageManager.CreatePage();

            Debug.Assert(amPage.Index == 2, "The first access method page should have index 2");

            var header = new RadixTreeNodesPageHeader
            {
                FreeSpace = (ushort)PageFormatter.GetMaximalFreeSpace((PageSize)PageManager.PageSize)
            };

            PageFormatter.InitPage(amPage, header);

            PageManager.UpdatePage(amPage);
        }

        protected override void CheckInfo()
        {
            base.CheckInfo();

            if (Info.KeyClrTypeName != typeof(TKey).FullName)
                throw new DataTankerException("Mismatch storage key type");

            if (Info.ValueClrTypeName != typeof(TValue).FullName)
                throw new DataTankerException("Mismatch storage value type");

            // we needn't check key size for radix-tree
            //if (Info.MaxKeyLength != int.MaxValue)
            //    throw new DataTankerException("Mismatch key size");
        }

        protected override void FillInfo()
        {
            base.FillInfo();
            Info.KeyClrTypeName = typeof(TKey).FullName;
            Info.ValueClrTypeName = typeof(TValue).FullName;
        }

        public Type KeyType { get; }
        public Type ValueType { get; }

        /// <summary>
        /// Gets the access method implemented by this storage
        /// </summary>
        public virtual AccessMethod AccessMethod => AccessMethod.RadixTree;

        public TValue Get(TKey key)
        {
            return WrapWithReadLock(_tree.Get, key);
        }

        public bool HasSubkeys(TKey key)
        {
            return WrapWithReadLock(_tree.HasSubKeys, key);
        }

        /// <summary>
        /// Inserts a new value to storage or updates an existing one.
        /// </summary>
        /// <param name="key">ComparableComparableKeyOf value</param>
        /// <param name="value">ValueOf value :)</param>
        public void Set(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            EnterWriteWrap();
            try
            {
                _tree.Set(key, value);
            }
            finally
            {
                EditOperationFinished();
                ExitWriteWrap();
            }
        }

        /// <summary>
        /// Removes a value from storage by its key
        /// </summary>
        /// <param name="key">The key of value to remove</param>
        public void Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            EnterWriteWrap();
            try
            {
                _tree.Remove(key);
            }
            finally
            {
                EditOperationFinished();
                ExitWriteWrap();
            }
        }

        /// <summary>
        /// Checks if key-value pair exists in storage.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if key-value pair exists, false otherwise</returns>
        public bool Exists(TKey key)
        {
            return WrapWithReadLock(_tree.Exists, key);
        }

        public long GetRawDataLength(TKey key)
        {
            return WrapWithReadLock(_tree.GetRawDataLength, key);
        }

        /// <summary>
        /// Retrieves a segment of binary representation
        /// of the value referenced by the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="startIndex">The index in binary representation where the specified segment starts</param>
        /// <param name="endIndex">The index in binary representation where the specified segment ends</param>
        /// <returns></returns>
        public byte[] GetRawDataSegment(TKey key, long startIndex, long endIndex)
        {
            EnterReadWrap();
            try
            {
                return _tree.GetRawDataSegment(key, startIndex, endIndex);
            }
            finally
            {
                ExitReadWrap();
            }
        }

        internal RadixTreeKeyValueStorage(IPageManager pageManager, IRadixTree<TKey, TValue> tree)
            : this(pageManager,  tree, 10000, TimeSpan.Zero)
        {
        }

        internal RadixTreeKeyValueStorage(IPageManager pageManager, IRadixTree<TKey, TValue> tree, int autoFlushInterval, TimeSpan autoFlishInterval)
            : base(pageManager, autoFlushInterval, autoFlishInterval)
        {
            _tree = tree ?? throw new ArgumentNullException(nameof(tree));

            ValueType = typeof(TKey);
            KeyType = typeof(TValue);
        }

        /// <summary>
        /// Gets the minimal key.
        /// </summary>
        /// <returns>The minimal key</returns>
        public TKey Min()
        {
            return WrapWithReadLock(_tree.Min);
        }

        /// <summary>
        /// Gets the maximal key.
        /// </summary>
        /// <returns>The maximal key</returns>
        public TKey Max()
        {
            return WrapWithReadLock(_tree.Max);
        }

        /// <summary>
        /// Gets a value corresponding to the minimal key.
        /// </summary>
        /// <returns>The value corresponding to the minimal key</returns>
        public TValue MinValue()
        {
            return WrapWithReadLock<TValue>(_tree.MinValue);
            
        }

        /// <summary>
        /// Gets the value corresponding to the maximal key.
        /// </summary>
        /// <returns>The value corresponding to the maximal key</returns>
        public TValue MaxValue()
        {
            return WrapWithReadLock<TValue>(_tree.MaxValue);
        }

        /// <summary>
        /// Gets the key previous to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key previous to specified key</returns>
        public TKey PreviousTo(TKey key)
        {
            return WrapWithReadLock(_tree.PreviousTo, key);
        }

        /// <summary>
        /// Gets the key next to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key next to specified key</returns>
        public TKey NextTo(TKey key)
        {
            return WrapWithReadLock(_tree.NextTo, key);
        }

        /// <summary>
        /// Computes the count of key-value pairs in storage.
        /// </summary>
        /// <returns>the count of key-value pairs</returns>
        public long Count()
        {
            EnterReadWrap();
            try
            {
                return _tree.Count();
            }
            finally
            {
                ExitReadWrap();
            }
        }
    }
}