namespace DataTanker.BinaryFormat.Page
{
    using System;

    /// <summary>
    /// Throws when the page format mismatching occurs.
    /// </summary>
    [Serializable]
    internal class PageFormatException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PageFormatException.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public PageFormatException(string message)
            : base(message)
        { 
        }
    }
}