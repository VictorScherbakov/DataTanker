namespace DataTanker
{
    using System;

    /// <summary>
    /// Common storage interface.
    /// </summary>
    public interface IStorage : IDisposable
    {
        /// <summary>
        /// Triggered when there is a storage flush exception - usually
        /// indicating a corrupted storage (that should be restarted/deleted).
        /// </summary>
        event EventHandler<StorageFlushExceptionEventArgs> OnFlushException;

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
        /// Closes the storage if it is open.
        /// </summary>
        void Close();

        /// <summary>
        /// Clears buffers for this storage and causes any buffered data to be written.
        /// </summary>
        void Flush();

        /// <summary>
        /// Gets a page size in bytes.
        /// Page is a data block that is written and read entirely.
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Gets a storage location.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Gets a value indicating whether a storage is open.
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the on-disk structure version.
        /// </summary>
        int OnDiskStructureVersion { get; }

        /// <summary>
        /// Gets the access method implemented by this storage
        /// </summary>
        Settings.AccessMethod AccessMethod { get; }
    }
}