using System;
using DataTanker.MemoryManagement;
using DataTanker.Transactions;
using DataTanker.Versioning;

namespace DataTanker.AccessMethods
{
    /// <summary>
    /// Implementation of IValueStorage. 
    /// Instances of this class store values using specified object that implements IMemoryManager.
    /// </summary>
    /// <typeparam name="TValue">The type of value</typeparam>
    internal class ValueStorage<TValue> : IValueStorage<TValue> 
        where TValue : IValue
    {
        private readonly IMemoryManager _memoryManager;
        private readonly ISerializer<TValue> _valueSerializer;

        public ISnapshotData SnapshotData { get; set; }

        /// <summary>
        /// Fetches value by its reference.
        /// </summary>
        /// <param name="reference">Reference to the value</param>
        /// <returns>The instance of value</returns>
        public TValue Fetch(DbItemReference reference)
        {
            var item = _memoryManager.Get(reference);

            if (IsVersioningEnabled)
            {
                var record = new VersionedRecord(item.RawData, _memoryManager, SnapshotData);

                if (record.HasVisibleVersionTo(DataTankerTransaction.Current.Id))
                    item = record.GetMatchingVersion(DataTankerTransaction.Current.Id);
            }

            return _valueSerializer.Deserialize(item.RawData);
        }

        /// <summary>
        /// Retreives the length of value (in bytes) by its reference.
        /// </summary>
        /// <param name="reference">Reference to the value</param>
        /// <returns>The length of referenced value</returns>
        public long GetRawDataLength(DbItemReference reference)
        {
            reference = GetVersionReferenceByRecordReference(reference);

            return _memoryManager.GetLength(reference);
        }

        /// <summary>
        /// Gets the segment of binary representation of value.
        /// </summary>
        /// <param name="reference">Reference to the value</param>
        /// <param name="startIndex">The start index of binary representation</param>
        /// <param name="endIndex">The end index of binary representation</param>
        /// <returns>The array of bytes containing specified segment of value</returns>
        public byte[] GetRawDataSegment(DbItemReference reference, long startIndex, long endIndex)
        {
            reference = GetVersionReferenceByRecordReference(reference);

            return _memoryManager.GetItemSegment(reference, startIndex, endIndex);
        }

        private DbItemReference GetVersionReferenceByRecordReference(DbItemReference reference)
        {
            if (IsVersioningEnabled)
            {
                var record = new VersionedRecord(_memoryManager.Get(reference).RawData, _memoryManager, SnapshotData);

                if (record.HasVisibleVersionTo(DataTankerTransaction.Current.Id))
                    reference = record.GetMatchingVersionReference(DataTankerTransaction.Current.Id);
            }

            return reference;
        }

        /// <summary>
        /// Allocates a new value and produces reference to it.
        /// </summary>
        /// <param name="value">Value to allocate</param>
        /// <returns>Reference to allocated value</returns>
        public DbItemReference AllocateNew(TValue value)
        {
            var valueBytes = _valueSerializer.Serialize(value);
            return IsVersioningEnabled 
                ? VersionedRecord.CreateNew(valueBytes, DataTankerTransaction.Current.Id, _memoryManager) 
                : _memoryManager.Allocate(valueBytes);
        }

        /// <summary>
        /// Reallocates already allocated value and produces reference to it.
        /// </summary>
        /// <param name="reference">Reference to the already allocated value</param>
        /// <param name="newValue">New value to allocate</param>
        /// <returns>Reference to reallocated value</returns>
        public DbItemReference Reallocate(DbItemReference reference, TValue newValue)
        {
            return _memoryManager.Reallocate(reference, _valueSerializer.Serialize(newValue));
        }

        /// <summary>
        /// Release allocated value.
        /// </summary>
        /// <param name="reference">Reference to allocated value</param>
        public void Free(DbItemReference reference)
        {
            //if (IsVersioningEnabled)
            //{
            //    var record = new VersionedRecord(_memoryManager.Get(reference).RawData, _memoryManager, SnapshotData);
            //    record.Expire(DataTankerTransaction.Current.Id);
            //}
            //else
                _memoryManager.Free(reference);
        }

        /// <summary>
        /// Gets the value indicating whether the versioning mechanisms is enabled.
        /// </summary>
        public bool IsVersioningEnabled => SnapshotData != null && DataTankerTransaction.Current != null;

        public ValueStorage(IMemoryManager memoryManager, ISerializer<TValue> valueSerializer)
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _valueSerializer = valueSerializer ?? throw new ArgumentNullException(nameof(valueSerializer));
        }
    }
}