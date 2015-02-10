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

            if (Info.MaxKeyLength != int.MaxValue)
                throw new DataTankerException("Mismatch key size");
        }

        protected override void FillInfo()
        {
            base.FillInfo();
            Info.KeyClrTypeName = typeof(TKey).FullName;
            Info.ValueClrTypeName = typeof(TValue).FullName;
            Info.MaxKeyLength = int.MaxValue;
        }

        public Type KeyType { get; private set; }
        public Type ValueType { get; private set; }

        /// <summary>
        /// Gets the access method implemented by this storage
        /// </summary>
        public override AccessMethod AccessMethod { get { return AccessMethod.RadixTree; } }

        public TValue Get(TKey key)
        {
            return WrapMethod(_tree.Get, key);
        }

        public bool HasSubkeys(TKey key)
        {
            return WrapMethod(_tree.HasSubkeys, key);
        }

        /// <summary>
        /// Inserts a new value to storage or updates an existing one.
        /// </summary>
        /// <param name="key">ComparableComparableKeyOf value</param>
        /// <param name="value">ValueOf value :)</param>
        public void Set(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            EnterWrap();
            try
            {
                _tree.Set(key, value);
            }
            finally
            {
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

        internal RadixTreeKeyValueStorage(IPageManager pageManager, IRadixTree<TKey, TValue> tree)
            : base(pageManager)
        {
            if (tree == null)
                throw new ArgumentNullException("tree");

            _tree = tree;

            ValueType = typeof(TKey);
            KeyType = typeof(TValue);
        }

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
        /// Computes the count of key-value pairs in storage.
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
    }
}