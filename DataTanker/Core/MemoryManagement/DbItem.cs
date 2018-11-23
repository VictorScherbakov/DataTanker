namespace DataTanker.MemoryManagement
{
    using System;
    using System.Linq;

    using BinaryFormat.Page;

    /// <summary>
    /// Represents a db item like key, value, index node,
    /// free space map entry etc.
    /// </summary>
    internal class DbItem : IEquatable<DbItem>
    {
        private static readonly short _baseSize = 10;

        private readonly SizeRange _sizeRange;

        private byte[] _rawData;

        private static readonly short[] _maxSizes;

        static DbItem()
        {
            _maxSizes = new short[(int)(SizeRange.Range11 + 1)];

            int i = 0;
            foreach (var sizeRange in EnumHelper.FixedSizeItemsSizeRanges())
            {
                var scb = (byte)sizeRange;
                _maxSizes[i++] = (short)(_baseSize * (short)Math.Pow(2, scb));
            }
        }

        public static short GetMaxSize(SizeRange sizeRange)
        {
            if (sizeRange == SizeRange.NotApplicable ||
                sizeRange == SizeRange.MultiPage)
                throw new ArgumentOutOfRangeException(nameof(sizeRange));

            return _maxSizes[(int) sizeRange];
        }

        public static SizeRange GetSizeRange(long length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            for (int i = 0; i < _maxSizes.Length; i++)
            {
                if (_maxSizes[i] >= length)
                    return (SizeRange)i;
            }

            return SizeRange.MultiPage;
        }


        public byte[] RawData
        {
            get { return _rawData; }
            set { _rawData = value; }
        }

        /// <summary>
        /// Returns an allocation type of object.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public AllocationType GetAllocationType(int pageSize)
        {
            if(_sizeRange == SizeRange.MultiPage)
                return AllocationType.MultiPage;

            return GetMaxSize(SizeRange) > pageSize - 8 
                ? AllocationType.MultiPage 
                : AllocationType.SinglePage; 
        }

        /// <summary>
        /// Gets a size range of this item.
        /// Items unable to change its size ranges.
        /// </summary>
        public SizeRange SizeRange => _sizeRange;


        /// <summary>
        /// Gets a size of object.
        /// </summary>
        public long Size => _rawData.Length;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(DbItem other)
        {
            if (other == null)
                return false;

            return _sizeRange == other.SizeRange &&
                   _rawData.Length == other.RawData.Length &&
                   _rawData.SequenceEqual(other.RawData);
        }

        public DbItem(byte[] rawData)
        {
            _rawData = rawData ?? throw new ArgumentNullException(nameof(rawData));

            _sizeRange = GetSizeRange(rawData.Length);
        }
    }
}