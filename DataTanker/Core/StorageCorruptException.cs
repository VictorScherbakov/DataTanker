using System;

namespace DataTanker
{
    /// <summary>
    /// Throws when the storage content is corrupt.
    /// </summary>
    [Serializable]
    internal class StorageCorruptException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the StorageCorruptException.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public StorageCorruptException(string message)
            : base(message)
        {
        }
    }
}