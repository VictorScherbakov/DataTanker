namespace DataTanker
{
    using System;

    /// <summary>
    /// Common storage interface.
    /// </summary>
    public interface IStorage : IDisposable
    {
        /// <summary>
        /// Opens an existing storage.
        /// </summary>
        /// <param name="path">A string containing information about storage location</param>
        void OpenExisting(string path);

        /// <summary>
        /// Creates a new storage.
        /// </summary>
        /// <param name="path">A string containing information about storage location</param>
        void CreateNew(string path);

        /// <summary>
        /// Opens existing storage or creates a new one.
        /// </summary>
        /// <param name="path">A string containing information about Storage location</param>
        void OpenOrCreate(string path);

        /// <summary>
        /// Gets a storage location.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets a value indicating whether a storage is open.
        /// </summary>
        bool IsOpen { get; }
    }
}