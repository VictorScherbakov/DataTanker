using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace DataTanker
{
    /// <summary>
    /// Native .NET serializer. This implementation uses BinaryFormatter class to serialize objects.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class NativeSerializer<T> : ISerializer<T>
    {
        public T Deserialize(byte[] bytes)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream(bytes))
            {
                return (T)bf.Deserialize(ms);
            }
        }

        public byte[] Serialize(T obj)
        {
            var bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                ms.Flush();
                return ms.ToArray();
            }
        }
    }
}