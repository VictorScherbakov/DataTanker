using System.Collections.Generic;
using DataTanker;
using DataTanker.MemoryManagement;

namespace Tests.Emulation
{
    /// <summary>
    /// Simple value storage. Mainly for testing purposes.
    /// </summary>
    /// <typeparam name="TValue">Type of value</typeparam>
    public class MemoryValueStorage<TValue> : IValueStorage<TValue> 
        where TValue : class, IValue
    {
        private long _index;
        private readonly Dictionary<string, TValue> _dictionary = new Dictionary<string, TValue>();

        public TValue Fetch(DbItemReference reference)
        {
            return _dictionary[reference.ToString()];
        }

        public long GetRawDataLength(DbItemReference reference)
        {
            throw new System.NotImplementedException();
        }

        public byte[] GetRawDataSegment(DbItemReference reference, long startIndex, long endIndex)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Allocates a new value and produces reference to it.
        /// </summary>
        /// <param name="value">Value to allocate</param>
        /// <returns>Reference to allocated value</returns>
        public DbItemReference AllocateNew(TValue value)
        {
            var reference = new DbItemReference(_index++, 0);
            _dictionary.Add(reference.ToString(), value);
            return reference;
        }

        /// <summary>
        /// Reallocates already allocated value and produces reference to it.
        /// </summary>
        /// <param name="reference">Reference to the already allocated value</param>
        /// <param name="newValue">New value to allocate</param>
        /// <returns>Reference to reallocated value</returns>
        public DbItemReference Reallocate(DbItemReference reference, TValue newValue)
        {
            var str = reference.ToString();
            _dictionary.Remove(str);
            return AllocateNew(newValue);
        }

        /// <summary>
        /// Release allocated value.
        /// </summary>
        /// <param name="reference">Reference to allocated value</param>
        public void Free(DbItemReference reference)
        {
            _dictionary.Remove(reference.ToString());
        }

        /// <summary>
        /// Gets the value indicating whether the versioning mechanisms is enabled.
        /// </summary>
        public bool IsVersioningEnabled
        {
            get { return false; }
        }
    }
}