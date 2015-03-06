namespace DataTanker.AccessMethods.BPlusTree
{
    using System;
    using System.Diagnostics;

    using BinaryFormat.Page;
    using MemoryManagement;
    using PageManagement;
    using Settings;

    /// <summary>
    /// Key-value storage based on B+Tree
    /// </summary>
    /// <typeparam name="TKey">The type of keys</typeparam>
    /// <typeparam name="TValue">The type of values</typeparam>
    internal class BPlusTreeKeyValueStorage<TKey, TValue> : TransactionalStorage, IBPlusTreeKeyValueStorage<TKey, TValue>
        where TKey : IComparableKey 
        where TValue : IValue
    {
        private readonly int _maxKeySize;
        private readonly IBPlusTree<TKey, TValue> _tree;

        private TKey WrapMethod(Func<TKey> method)
        {
            EnterWrap();
            try
            {
                return method();
            }
            finally
            {
                ExitWrap();
            }
        }

        private TReturnValue WrapMethod<TReturnValue>(Func<TKey, TReturnValue> method, TKey key)
        {
            if (key == null) throw new ArgumentNullException("key");

            EnterWrap();
            try
            {
                return method(key);
            }
            finally
            {
                ExitWrap();
            }
        }

        protected override void Init()
        {
            base.Init();

            //add access-method page
            IPage amPage = PageManager.CreatePage();

            Debug.Assert(amPage.Index == 2, "The first access method page should have index 2");

            var tnph = new BPlusTreeNodePageHeader
            {
                ParentPageIndex = -1,
                PreviousPageIndex = -1,
                NextPageIndex = -1,
                IsLeaf = true,
                SizeClass = DbItem.GetSizeClass(_maxKeySize + sizeof(Int64) + sizeof(Int16))
            };

            PageFormatter.InitPage(amPage, tnph);
            PageManager.UpdatePage(amPage);
        }

        protected override void CheckInfo()
        {
            base.CheckInfo();

            if (Info.KeyClrTypeName != typeof(TKey).FullName)
                throw new DataTankerException("Mismatch storage key type");

            if (Info.ValueClrTypeName != typeof(TValue).FullName)
                throw new DataTankerException("Mismatch storage value type");

            if (Info.MaxKeyLength != _maxKeySize)
                throw new DataTankerException("Mismatch key size");
        }

        protected override void FillInfo()
        {
            base.FillInfo();
            Info.KeyClrTypeName = typeof(TKey).FullName;
            Info.ValueClrTypeName = typeof(TValue).FullName;
            Info.MaxKeyLength = _maxKeySize;
        }

        public Type KeyType { get; private set; }
        public Type ValueType { get; private set; }

        /// <summary>
        /// Gets the access method implemented by this storage
        /// </summary>
        public override AccessMethod AccessMethod { get { return AccessMethod.BPlusTree; } }

        /// <summary>
        /// Gets the minimal key.
        /// </summary>
        /// <returns>The minimal key</returns>
        public TKey Min()
        {
            return WrapMethod(_tree.Min);
        }

        /// <summary>
        /// Gets the maximal key.
        /// </summary>
        /// <returns>The maximal key</returns>
        public TKey Max()
        {
            return WrapMethod(_tree.Max);
        }

        /// <summary>
        /// Gets the key previous to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key previous to specified key</returns>
        public TKey PreviousTo(TKey key)
        {
            return WrapMethod(_tree.PreviousTo, key);
        }

        /// <summary>
        /// Gets the key next to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key next to specified key</returns>
        public TKey NextTo(TKey key)
        {
            return WrapMethod(_tree.NextTo, key);
        }

        /// <summary>
        /// Gets a value from storage.
        /// </summary>
        /// <param name="key">ComparableComparableKeyOf value</param>
        /// <returns>Requested value</returns>
        public TValue Get(TKey key)
        {
            return WrapMethod(_tree.Get, key);
        }

        /// <summary>
        /// Inserts a new value to storage or updates an existing one.
        /// </summary>
        /// <param name="key">ComparableComparableKeyOf value</param>
        /// <param name="value">ValueOf value :)</param>
        public void Set(TKey key, TValue value)
        {
            if(key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            EnterWrap();
            try
            {
                _tree.Set(key, value);
            }
            finally
            {
                EditOperationFinished();
                ExitWrap();
            }
        }

        /// <summary>
        /// Removes a value from storage by its key
        /// </summary>
        /// <param name="key">The key of value to remove</param>
        public void Remove(TKey key)
        {
            if (key == null) throw new ArgumentNullException("key");

            EnterWrap();
            try
            {
                _tree.Remove(key);
            }
            finally
            {
                EditOperationFinished();
                ExitWrap();
            }
        }

        /// <summary>
        /// Cheks if key-value pair exists in storage.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if key-value pair exists, false otherwise</returns>
        public bool Exists(TKey key)
        {
            return WrapMethod(_tree.Exists, key);
        }

        /// <summary>
        /// Computes the count of key-value pairs in tree.
        /// </summary>
        /// <returns>the count of key-value pairs</returns>
        public long Count()
        {
            EnterWrap();
            try
            {
                return _tree.Count();
            }
            finally
            {
                ExitWrap();
            }
        }

        /// <summary>
        /// Retrieves the length (in bytes) of binary representation
        /// of the value referenced by the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The length of binary representation</returns>
        public long GetRawDataLength(TKey key)
        {
            return WrapMethod(_tree.GetRawDataLength, key);
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
            EnterWrap();
            try
            {
                return _tree.GetRawDataSegment(key, startIndex, endIndex);
            }
            finally
            {
                ExitWrap();
            }
        }

        internal BPlusTreeKeyValueStorage(IPageManager pageManager, IBPlusTree<TKey, TValue> tree, int maxKeySize)
            : this(pageManager, tree, maxKeySize, 10000, TimeSpan.Zero)
        {
        }

        internal BPlusTreeKeyValueStorage(IPageManager pageManager, IBPlusTree<TKey, TValue> tree, int maxKeySize, int autoFlushInterval, TimeSpan autoFlushTimeout) 
            : base(pageManager, autoFlushInterval, autoFlushTimeout)
        {
            if (tree == null) 
                throw new ArgumentNullException("tree");

            _maxKeySize = maxKeySize;
            _tree = tree;

            ValueType = typeof(TKey);
            KeyType = typeof(TValue);
        }
    }
}