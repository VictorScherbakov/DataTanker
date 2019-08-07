namespace DataTanker.Versioning
{
    using System;
    using System.IO;

    using MemoryManagement;

    internal class VersionInfo
    {
        public int CreateChangeNumber { get; set; }
        public int ExpireChangeNumber { get; set; }

        public DbItemReference VersionReference { get; set; }

        public static int BytesLength => sizeof (int) + // CreateChangeNumber
                                         sizeof (int) + // ExpireChangeNumber
                                         DbItemReference.BytesLength;

        public void Write(Stream stream)
        {
            stream.Write(BitConverter.GetBytes(CreateChangeNumber), 0, sizeof(int));
            stream.Write(BitConverter.GetBytes(ExpireChangeNumber), 0, sizeof(int));
            VersionReference.Write(stream);
        }

        public static VersionInfo Read(Stream stream)
        {
            var result = new VersionInfo();
            var buffer = new byte[sizeof(int)];
            stream.Read(buffer, 0, sizeof(int));
            result.CreateChangeNumber = BitConverter.ToInt32(buffer, 0);
            stream.Read(buffer, 0, sizeof(int));
            result.ExpireChangeNumber = BitConverter.ToInt32(buffer, 0);
            result.VersionReference = DbItemReference.Read(stream);

            return result;
        }
    }
}