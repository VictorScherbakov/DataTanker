using System;

namespace DataTanker.PageManagement
{
    /// <summary>
    /// Throws when page mapping operation is incorrect.
    /// </summary>
    [Serializable]
    internal class PageMapException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PageMapException.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public PageMapException(string message)
            : base(message)
        { 
        }
    }
}