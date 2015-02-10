using System;
using System.Collections.Generic;
using DataTanker.MemoryManagement;

namespace Tests.Emulation
{
    internal class MemoryManager : IMemoryManager
    {
        private static long _lastNumber;
        private readonly Dictionary<long, byte[]> _items = new Dictionary<long, byte[]>();

        public DbItemReference Allocate(byte[] content)
        {
            _items[_lastNumber] = content;
            var reference = new DbItemReference(_lastNumber, 0);
            _lastNumber++;

            return reference;
        }

        public void Free(DbItemReference reference)
        {
            _items.Remove(reference.PageIndex);
        }

        public DbItemReference Reallocate(DbItemReference reference, byte[] newContent)
        {
            _items[reference.PageIndex] = newContent;
            return new DbItemReference(reference.PageIndex, 0);
        }

        public DbItem Get(DbItemReference reference)
        {
            return new DbItem(_items[reference.PageIndex]);
        }

        public long GetLength(DbItemReference reference)
        {
            return _items[reference.PageIndex].Length;
        }

        public byte[] GetItemSegment(DbItemReference reference, long startIndex, long endIndex)
        {
            var item = _items[reference.PageIndex];
            var result = new byte[endIndex - startIndex];
            Array.Copy(item, startIndex, result, 0, result.Length);
            return result;
        }
    }
}
