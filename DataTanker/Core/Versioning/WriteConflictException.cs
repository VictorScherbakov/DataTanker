namespace DataTanker.Versioning
{
    using System;

    /// <summary>
    /// Throws when the write conflict occurss.
    /// </summary>
    [Serializable]
    internal class WriteConflictException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the WriteConflictException.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public WriteConflictException(string message)
            : base(message)
        { 
        }
    }
}