namespace DataTanker.PageManagement
{
    using System;
    using System.Threading;
    using System.Diagnostics;
    using System.Collections.Generic;

    /// <summary>
    /// Implements a page-level interface of a storage.
    /// </summary>
    internal class MemoryPageManager : IPageManager, IDisposable
    {
        private bool _disposed;

        private readonly Dictionary<long, IPage> _entries = new Dictionary<long, IPage>();

        private IStorage _storage;
        private readonly int _pageSize;

        private readonly object _locker = new object();

        [DebuggerNonUserCode]
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("MemoryPageManager");
        }

        private void CheckStorage()
        {
            if (_storage == null)
                throw new InvalidOperationException("Storage is null");

            if (!_storage.IsOpen)
                throw new InvalidOperationException("Storage is not open");
        }

        private long _maxPageIndex;

        private IPage AppendPage()
        {
            var page = new Page(this, _maxPageIndex, new byte[_pageSize]);
            _entries.Add(_maxPageIndex, page);
            _maxPageIndex++;
            return page;
        }

        private IPage AppendPage(byte[] content)
        {

            var page = new Page(this, _maxPageIndex, content);
            _entries.Add(_maxPageIndex, page);
            _maxPageIndex++;
            return page;
        }

        #region IPageManager Members

        /// <summary>
        /// Locks current instance.
        /// This should prevent an execution of another threads until Unlock() is called.
        /// </summary>
        public void Lock()
        {
            Monitor.Enter(_locker);
        }

        /// <summary>
        /// This should allow an execution of another threads.
        /// </summary>
        public void Unlock()
        {
            Monitor.Exit(_locker);
        }

        public bool CanCreate()
        {
            return true;
        }

        /// <summary>
        /// Switches page manager instance to the atomic operation mode.
        /// In such a mode, all further changes can be applied all at once 
        /// by calling ExitAtomicOperation() method or canceled.
        /// </summary>
        public void EnterAtomicOperation()
        {
        }

        /// <summary>
        /// Switches page manager instance to normal mode.
        /// All the changes made since the last EnterAtomicOperation() call are applied.
        /// </summary>
        public void ExitAtomicOperation()
        {
        }

        /// <summary>
        /// Gets the storage instance that operates with storage pages via this page manager.
        /// </summary>
        public IStorage Storage
        {
            get { return _storage; }
            set { _storage = value; }
        }

        /// <summary>
        /// Checks if the page exists.
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public bool PageExists(long pageIndex)
        {
            if (pageIndex < 0)
                throw new ArgumentException("Page index should not be negative", nameof(pageIndex));

            CheckDisposed();

            Lock();
            try
            {
                CheckStorage();

                return _entries.ContainsKey(pageIndex);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Fetches the page by index.
        /// </summary>
        /// <param name="pageIndex">An index of page</param>
        /// <returns>The fetched page</returns>
        public IPage FetchPage(long pageIndex)
        {
            CheckDisposed();

            Lock();
            try
            {
                CheckStorage();

                if (_entries.ContainsKey(pageIndex))
                    return _entries[pageIndex];

                throw new PageMapException("Page not found");
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Fetches the page with specified index using provided array as a container.
        /// Callee should have full control over the sharing of content.
        /// This method should used to reduce buffers reallocation. 
        /// </summary>
        /// <param name="pageIndex">An index of page</param>
        /// <param name="content">The content of creating page</param>
        /// <returns>The fetched page</returns>
        public IPage FetchPage(long pageIndex, byte[] content)
        {
            CheckDisposed();

            if (content == null) throw new ArgumentNullException(nameof(content));
            if (content.Length != _pageSize)
                throw new ArgumentException("content");

            Lock();
            try
            {
                CheckStorage();

                if (_entries.ContainsKey(pageIndex))
                {
                    var page = _entries[pageIndex];
                    Buffer.BlockCopy(page.Content, 0, content, 0, content.Length);
                    return new Page(this, page.Index, content);
                }
                    
                throw new PageMapException("Page not found");
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Updates a page.
        /// </summary>
        /// <param name="page">A page instance to update</param>
        public void UpdatePage(IPage page)
        {
            CheckDisposed();

            if (page == null)
                throw new ArgumentNullException(nameof(page));

            Lock();
            try
            {
                CheckStorage();

                if (_entries.ContainsKey(page.Index))
                {
                    var p = _entries[page.Index];
                    Buffer.BlockCopy(page.Content, 0, p.Content, 0, p.Content.Length);
                    return;
                }

                throw new PageMapException("Page not found");
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Creates a new page in storage.
        /// </summary>
        /// <returns>Created page</returns>
        public IPage CreatePage()
        {
            CheckDisposed();

            Lock();
            try
            {
                CheckStorage();

                return AppendPage();
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Creates a new page in storage with the specified page content.
        /// Callee should have full control over the sharing of page content.
        /// This method should used to reduce buffers reallocation. 
        /// </summary>
        /// <param name="content">The content of creating page</param>
        /// <returns>Created page</returns>
        public IPage CreatePage(byte[] content)
        {
            CheckDisposed();

            if (content == null) throw new ArgumentNullException(nameof(content));

            Lock();
            try
            {
                CheckStorage();

                return AppendPage(content);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Removes a page from storage.
        /// </summary>
        /// <param name="pageIndex">Index of removing page</param>
        public void RemovePage(long pageIndex)
        {
            CheckDisposed();

            Lock();
            try
            {
                CheckStorage();

                if (_entries.ContainsKey(pageIndex))
                    _entries.Remove(pageIndex);

                throw new PageMapException("Page not found");
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Creates a new page space.
        /// </summary>
        public void CreateNewPageSpace()
        {
            CheckDisposed();

            Lock();
            try
            {
                OpenExistingPageSpace();
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Opens an existing page space.
        /// </summary>
        public void OpenExistingPageSpace()
        {
            CheckDisposed();

            Lock();
            try
            {
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Closes the storage if it is open. Actually calls Dispose() method.
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        /// <summary>
        /// Gets a page size in bytes.
        /// </summary>
        public int PageSize => _pageSize;

        /// <summary>
        /// Gets a value indicating whether all write operations perform immediately to file storage
        /// </summary>
        public bool ForcedWrites { get; }

        /// <summary>
        /// Checks if this page manager instance can
        /// operate with specified page.
        /// This is needed to properly creation of pages.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public bool CheckPage(IPage page)
        {
            CheckDisposed();

            if (!(page is Page))
                return false;

            return page.Length == _pageSize;
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~MemoryPageManager()
        {
            Dispose();
        }

        /// <summary>
        /// Initializes a new instance of the MemoryPageManager.
        /// </summary>
        /// <param name="pageSize"></param>
        internal MemoryPageManager(int pageSize)
            : this(pageSize, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the MemoryPageManager.
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="forcedWrites"></param>
        internal MemoryPageManager(int pageSize, bool forcedWrites)
        {
            if (pageSize < 4096)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Too small page size");

            _pageSize = pageSize;
            ForcedWrites = forcedWrites;
        }
    }
}