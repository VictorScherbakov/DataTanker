namespace DataTanker
{
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;

    /// <summary>
    /// Container for general information about storage.
    /// </summary>
    [DataContract]
    public class StorageInfo
    {
        [DataMember]
        public string StorageClrTypeName { get; set; }

        [DataMember]
        public string KeyClrTypeName { get; set; }

        [DataMember]
        public string ValueClrTypeName { get; set; }

        [DataMember]
        public int MaxKeyLength { get; set; }

        public override string ToString()
        {
            var serializer = new DataContractJsonSerializer(typeof(StorageInfo));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Flush();
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static StorageInfo FromString(string str)
        {
            var serializer = new DataContractJsonSerializer(typeof(StorageInfo));
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
            {
                return serializer.ReadObject(ms) as StorageInfo;
            }
        }
    }
}