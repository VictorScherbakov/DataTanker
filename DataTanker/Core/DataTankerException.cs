namespace DataTanker
{
    using System;

    /// <summary>
    /// Throws when occurs an exception specific for storage.
    /// </summary>
    [Serializable]
    internal class DataTankerException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the DataTankerException.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public DataTankerException(string message)
            : base(message)
        { 
        }
    }
}