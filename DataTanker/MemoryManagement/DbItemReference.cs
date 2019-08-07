namespace DataTanker.MemoryManagement
{
    using System;
    using System.IO;

    /// <summary>
    /// Represents a reference to the DbItem
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Page: {PageIndex} Item: {ItemIndex}")]
    public class DbItemReference : IEquatable<DbItemReference>, ICloneable
    {
        public static DbItemReference Null = new DbItemReference(-1, 0);

        public long PageIndex { get; set; }
        public short ItemIndex { get; set; }

        public bool Equals(DbItemReference other)
        {
            if (other == null)
                return false;

            return PageIndex == other.PageIndex && ItemIndex == other.ItemIndex;
        }

        public static bool IsNull(DbItemReference reference)
        {
            return reference == null || (reference.PageIndex == -1 && reference.ItemIndex == 0);
        }

        public override string ToString()
        {
            return $"{PageIndex}_{ItemIndex}";
        }

        public object Clone()
        {
            return new DbItemReference(PageIndex, ItemIndex);
        }

        public static DbItemReference Parse(string value)
        {
            var entries = value.Split('_');
            return new DbItemReference(long.Parse(entries[0]), short.Parse(entries[1]));
        }

        public void WriteBytes(byte[] array, int offset)
        {
            BitConverter.GetBytes(PageIndex).CopyTo(array, offset);
            Buffer.BlockCopy(BitConverter.GetBytes(ItemIndex), 0, array, offset + sizeof(Int64), sizeof(Int16));
        }

        public byte[] GetBytes()
        {
            var result = new byte[BytesLength];
            BitConverter.GetBytes(PageIndex).CopyTo(result, 0);

            Buffer.BlockCopy(BitConverter.GetBytes(ItemIndex), 0, result, sizeof(Int64), sizeof(Int16));

            return result;
        }

        public void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(PageIndex), 0, sizeof(Int64));
            stream.Write(BitConverter.GetBytes(ItemIndex), 0, sizeof(Int16));
        }

        public static DbItemReference Read(Stream stream)
        {
            var buffer = new byte[BytesLength];
            stream.Read(buffer, 0, BytesLength);
            var pageIndex = BitConverter.ToInt64(buffer, 0);
            var itemIndex = BitConverter.ToInt16(buffer, sizeof(Int64));

            return new DbItemReference(pageIndex, itemIndex);
        }

        public static int BytesLength => sizeof(Int64) + // PageIndex
                                         sizeof(Int16);

        public static DbItemReference FromBytes(byte[] bytes, int offset = 0)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < BytesLength + offset)
                throw new ArgumentException(nameof(bytes));

            return new DbItemReference(BitConverter.ToInt64(bytes, offset), BitConverter.ToInt16(bytes, offset + sizeof(Int64)));
        }

        public DbItemReference(long pageIndex, short itemIndex)
        {
            PageIndex = pageIndex;
            ItemIndex = itemIndex;
        }
    }
}