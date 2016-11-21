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

        private readonly SizeClass _sizeClass;

        private byte[] _rawData;

        private static readonly short[] _maxSizes;

        static DbItem()
        {
            _maxSizes = new short[(int)(SizeClass.Class11 + 1)];

            int i = 0;
            foreach (var sizeClass in EnumHelper.FixedSizeItemsSizeClasses())
            {
                var scb = (byte)sizeClass;
                _maxSizes[i++] = (short)(_baseSize * (short)Math.Pow(2, scb));
            }
        }

        public static short GetMaxSize(SizeClass sizeClass)
        {
            if (sizeClass == SizeClass.NotApplicable ||
                sizeClass == SizeClass.MultiPage)
                throw new ArgumentOutOfRangeException(nameof(sizeClass));

            return _maxSizes[(int) sizeClass];
        }

        public static SizeClass GetSizeClass(long length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            for (int i = 0; i < _maxSizes.Length; i++)
            {
                if (_maxSizes[i] >= length)
                    return (SizeClass)i;
            }

            return SizeClass.MultiPage;
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
            if(_sizeClass == SizeClass.MultiPage)
                return AllocationType.MultiPage;

            return GetMaxSize(SizeClass) > pageSize - 8 
                ? AllocationType.MultiPage 
                : AllocationType.SinglePage; 
        }

        /// <summary>
        /// Gets a size class of this item.
        /// Items unable to change its size classes.
        /// </summary>
        public SizeClass SizeClass => _sizeClass;


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

            return _sizeClass == other.SizeClass &&
                   _rawData.Length == other.RawData.Length &&
                   _rawData.SequenceEqual(other.RawData);
        }

        public DbItem(byte[] rawData)
        {
            if(rawData == null)
                throw new ArgumentNullException(nameof(rawData));

            _rawData = rawData;

            _sizeClass = GetSizeClass(rawData.Length);
        }
    }
}