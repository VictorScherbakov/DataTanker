namespace DataTanker.MemoryManagement
{
    using System;
    using System.Linq;

    using BinaryFormat.Page;

    /// <summary>
    /// Represents a db item like key, value, index node,
    /// free space map entry etc.
    /// </summary>
    public class DbItem : IEquatable<DbItem>
    {
        private static readonly short BaseSize = 10;
        private byte[] _rawData;

        private static readonly short[] MaxSizes;

        static DbItem()
        {
            MaxSizes = new short[(int)(SizeRange.Range11 + 1)];

            int i = 0;
            foreach (var sizeRange in EnumHelper.FixedSizeItemsSizeRanges())
            {
                var scb = (byte)sizeRange;
                MaxSizes[i++] = (short)(BaseSize * (short)Math.Pow(2, scb));
            }
        }

        public static short GetMaxSize(SizeRange sizeRange)
        {
            if (sizeRange == SizeRange.NotApplicable ||
                sizeRange == SizeRange.MultiPage)
                throw new ArgumentOutOfRangeException(nameof(sizeRange));

            return MaxSizes[(int) sizeRange];
        }

        public static SizeRange GetSizeRange(long length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            for (int i = 0; i < MaxSizes.Length; i++)
            {
                if (MaxSizes[i] >= length)
                    return (SizeRange)i;
            }

            return SizeRange.MultiPage;
        }


        public byte[] RawData
        {
            get => _rawData;
            set => _rawData = value;
        }

        /// <summary>
        /// Returns an allocation type of object.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public AllocationType GetAllocationType(int pageSize)
        {
            if(SizeRange == SizeRange.MultiPage)
                return AllocationType.MultiPage;

            return GetMaxSize(SizeRange) > pageSize - 8 
                ? AllocationType.MultiPage 
                : AllocationType.SinglePage; 
        }

        /// <summary>
        /// Gets a size range of this item.
        /// Items unable to change its size ranges.
        /// </summary>
        public SizeRange SizeRange { get; }


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

            return SizeRange == other.SizeRange &&
                   _rawData.Length == other.RawData.Length &&
                   _rawData.SequenceEqual(other.RawData);
        }

        public DbItem(byte[] rawData)
        {
            _rawData = rawData ?? throw new ArgumentNullException(nameof(rawData));

            SizeRange = GetSizeRange(rawData.Length);
        }
    }
}