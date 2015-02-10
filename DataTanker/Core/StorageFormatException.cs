namespace DataTanker
{
    using System;

    /// <summary>
    /// Throws when the storage content does not comply its format.
    /// </summary>
    [Serializable]
    internal class StorageFormatException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the StorageFormatException.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public StorageFormatException(string message)
            : base(message)
        { 
        }
    }
}