namespace DataTanker
{
    using System;

    /// <summary>
    /// Simple implementation of ISerializer using specified  methods.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Serializer<T> : ISerializer<T>
    {
        private readonly Func<T, byte[]> _serialize;
        private readonly Func<byte[], T> _deserialize;

        /// <summary>
        /// Initializes a new instance of the Serializer using specified serialization routines.
        /// </summary>
        /// <param name="serialize"></param>
        /// <param name="deserialize"></param>
        public Serializer(Func<T, byte[]> serialize, Func<byte[], T> deserialize)
        {
            _serialize = serialize ?? throw new ArgumentNullException(nameof(serialize));
            _deserialize = deserialize ?? throw new ArgumentNullException(nameof(deserialize));
        }

        /// <summary>
        /// Converts specified bytes to the instance of T.
        /// </summary>
        /// <param name="bytes">Bytes to convert</param>
        /// <returns>Resulted instance</returns>
        public T Deserialize(byte[] bytes)
        {
            return _deserialize(bytes);
        }

        /// <summary>
        /// Converts specified instance of T to byte array.
        /// </summary>
        /// <param name="obj">Instance to convert</param>
        /// <returns>Resulted bytes</returns>
        public byte[] Serialize(T obj)
        {
            return _serialize(obj);
        }
    }
}
