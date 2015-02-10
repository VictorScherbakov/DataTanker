namespace DataTanker
{
    /// <summary>
    /// Provides simple methods for serialization and deserialization of instances.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ISerializer<T>
    {
        /// <summary>
        /// Converts specified bytes to the instance of T.
        /// </summary>
        /// <param name="bytes">Bytes to convert</param>
        /// <returns>Resulted instance</returns>
        T Deserialize(byte[] bytes);

        /// <summary>
        /// Converts specified instance of T to byte array.
        /// </summary>
        /// <param name="obj">Instance to convert</param>
        /// <returns>Resulted bytes</returns>
        byte[] Serialize(T obj);
    }
}