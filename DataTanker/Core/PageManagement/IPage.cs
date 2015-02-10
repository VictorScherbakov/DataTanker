namespace DataTanker.PageManagement
{
    using System;

    /// <summary>
    /// Common interface of page.
    /// Page is a data block that is written and read entirely. 
    /// </summary>
    internal interface IPage : ICloneable
    {
        /// <summary>
        /// Gets the storage instance that owns this page.
        /// </summary>
        IStorage Storage { get; }

        /// <summary>
        /// Gets the index of this page.
        /// </summary>
        long Index { get; }

        /// <summary>
        /// Gets the content of this page.
        /// </summary>
        byte[] Content { get; }

        /// <summary>
        /// Gets the copy of page content.
        /// </summary>
        byte[] ContentCopy { get; }

        /// <summary>
        /// Gets or sets the object representing this page.
        /// </summary>
        object BackingObject { get; set; }

        /// <summary>
        /// Gets the byte length of this page.
        /// </summary>
        int Length { get; }
    }
}