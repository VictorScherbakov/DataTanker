namespace DataTanker.PageManagement
{
    using System.Linq;
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Wraps the underlying page manager with in-memory caching.
    /// </summary>
    internal class CachingPageManager : ICachingPageManager, IDisposable
    {
        private bool _disposed;

        private readonly IPageManager _underlyingPageManager;
        private readonly int _maxCachedPages;
        private long _maxDirtyPages;

        private long _successfulHitCount;
        private long _unsuccessfulHitCount;

        private int _dirtyPageCount;

        private readonly LinkedList<PageCacheEntry> _list = new LinkedList<PageCacheEntry>();
        private readonly Dictionary<long, LinkedListNode<PageCacheEntry>> _entries = new Dictionary<long, LinkedListNode<PageCacheEntry>>();

        private void AddPage(IPage page)
        {
            if (_entries.Count >= _maxCachedPages)
                Evict();

            var newEntry = new LinkedListNode<PageCacheEntry>(new PageCacheEntry {Page = page, HitCount = 1});

            _list.AddFirst(newEntry);
            _entries.Add(page.Index, newEntry);
        }

        private void Evict()
        {
            var entry = _list.Last.Value;
            if (entry.IsDirty)
            {
                _underlyingPageManager.UpdatePage(entry.Page);
                _dirtyPageCount--;
            }

            _entries.Remove(entry.Page.Index);
            _list.RemoveLast();
        }

        private void RemovePageFromCache(long pageIndex)
        {
            var entry = _entries[pageIndex];
            if (entry.Value.IsDirty)
                _dirtyPageCount--;

            _entries.Remove(pageIndex);
            _list.Remove(entry);
        }

        private IPage GetPage(long pageIndex)
        {
            var entry = _entries[pageIndex];

            entry.Value.HitCount++;

            _list.Remove(entry);
            _list.AddFirst(entry);
            return entry.Value.Page;
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("CachingPageManager");
        }

        #region ICachingPageManager

        /// <summary>
        /// Locks current instance.
        /// This should prevent an execution of another threads until Unlock() is called.
        /// </summary>
        public void Lock()
        {
            CheckDisposed();
            _underlyingPageManager.Lock();
        }

        /// <summary>
        /// This should allow an execution of another threads.
        /// </summary>
        public void Unlock()
        {
            CheckDisposed();
            _underlyingPageManager.Unlock();
        }

        /// <summary>
        /// Determines if storage can be created
        /// </summary>
        /// <returns>True if storage can be created, false otherwise</returns>
        public bool CanCreate()
        {
            return _underlyingPageManager.CanCreate();
        }

        /// <summary>
        /// Gets or sets a maximum number of updated pages.
        /// </summary>
        public long MaxDirtyPages
        {
            get 
            {
                CheckDisposed();
                return _maxDirtyPages; 
            }
            set 
            {
                CheckDisposed();

                if (value < 0)
                    throw new ArgumentException("MaxDirtyPages value should not be negative");
                _maxDirtyPages = value;

                if (_dirtyPageCount > _maxDirtyPages)
                    Flush();
            }
        }

        /// <summary>
        /// Gets the maximum number of cached pages (fetched and updated).
        /// </summary>
        public long MaxCachedPages
        {
            get 
            {
                CheckDisposed();
                return _maxCachedPages; 
            }
        }

        /// <summary>
        /// Force writing of all dirty pages to underlying page manager.
        /// </summary>
        public void Flush()
        {
            CheckDisposed();
            Lock();
            try
            {
                foreach (LinkedListNode<PageCacheEntry> cacheEntry in _entries.Values.Where(entry => entry.Value.IsDirty))
                {
                    _underlyingPageManager.UpdatePage(cacheEntry.Value.Page);
                    cacheEntry.Value.IsDirty = false;
                }

                _dirtyPageCount = 0;
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Gets a storage instance that operates with storage pages via this page manager.
        /// </summary>
        public IStorage Storage
        {
            get 
            {
                CheckDisposed();
                return _underlyingPageManager.Storage; 
            }
            set
            {
                CheckDisposed();
                _underlyingPageManager.Storage = value; 
            }
        }

        /// <summary>
        /// Checks if the page exists.
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        public bool PageExists(long pageIndex)
        {
            CheckDisposed();

            Lock();
            try
            {
                return _entries.ContainsKey(pageIndex) || _underlyingPageManager.PageExists(pageIndex);
            }
            finally
            {
                Unlock();
            }
        }

        /// <summary>
        /// Fetches a page.
        /// </summary>
        /// <param name="pageIndex">An index of page</param>
        /// <returns>The fetched page</returns>
        public IPage FetchPage(long pageIndex)
        {
            CheckDisposed();

            Lock();
            try
            {
                if (_entries.ContainsKey(pageIndex))
                {
                    _successfulHitCount++;
                    return GetPage(pageIndex);
                }
                else
                {
                    _unsuccessfulHitCount++;
                    IPage page = _underlyingPageManager.FetchPage(pageIndex, new byte[_underlyingPageManager.PageSize]);
                    AddPage(page);
                    return GetPage(page.Index);
                }
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
            if (content == null) throw new ArgumentNullException("content");
            if (content.Length != _underlyingPageManager.PageSize) throw new ArgumentException("content");

            Lock();
            try
            {
                if (_entries.ContainsKey(pageIndex))
                {
                    _successfulHitCount++;
                    return GetPage(pageIndex);
                }
                else
                {
                    _unsuccessfulHitCount++;
                    IPage page = _underlyingPageManager.FetchPage(pageIndex, content);
                    AddPage(page);
                    return GetPage(page.Index);
                }
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

            Lock();
            try
            {
                // replace the content of page if it is already in the cache
                if (_entries.ContainsKey(page.Index))
                {
                    _successfulHitCount++;
                    var entry = _entries[page.Index];
                    entry.Value.Page = page;
                    _list.Remove(entry);
                    _list.AddFirst(entry);
                }
                else
                {
#if DEBUG
                    if (_maxDirtyPages > 0)
                    {
                        // check if page exists
                        if(!_underlyingPageManager.PageExists(page.Index))
                            throw new ArgumentException("Page does not exists in storage", "page");
                    }
#endif

                    var newEntry = new LinkedListNode<PageCacheEntry>(new PageCacheEntry {Page = page});
                    _entries.Add(page.Index, newEntry);
                    _list.AddFirst(newEntry);

                    _unsuccessfulHitCount++;
                }

                // touch it
                _entries[page.Index].Value.HitCount++;

                if (_maxDirtyPages == 0)
                {
                    // dirty pages are not allowed, force underlying update
                    _underlyingPageManager.UpdatePage(page);
                }
                else
                {
                    if (!_entries[page.Index].Value.IsDirty)
                        _dirtyPageCount++;

                    _entries[page.Index].Value.IsDirty = true;

                    // maybe we need to flush all dirty pages
                    if (_dirtyPageCount > _maxDirtyPages)
                        Flush();
                }

                if (_entries.Count >= _maxCachedPages)
                    Evict();
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
                IPage page = _underlyingPageManager.CreatePage();
                AddPage(page);
                return GetPage(page.Index);
            }
            finally 
            {
                Unlock();
            }
        }

        /// <summary>
        /// Gets a value (from zero to one) indicating efficiency of caching.
        /// This value is calculated as: SuccessfulOperations / AllOperations.
        /// </summary>
        public double SuccessRate 
        {
            get 
            {
                if (_successfulHitCount + _unsuccessfulHitCount == 0)
                    return 0;

                return (double)_successfulHitCount / (_unsuccessfulHitCount + _successfulHitCount);
            } 
        }

        /// <summary>
        /// Resets the statistics of successful operations.
        /// </summary>
        public void ResetStatistics()
        {
            _successfulHitCount = 0;
            _unsuccessfulHitCount = 0;
        }

        /// <summary>
        /// Creates a new page space.
        /// </summary>
        public void CreateNewPageSpace()
        {
            CheckDisposed();

            _underlyingPageManager.CreateNewPageSpace();
        }

        /// <summary>
        /// Opens an existing page space.
        /// </summary>
        public void OpenExistingPageSpace()
        {
            CheckDisposed();

            _underlyingPageManager.OpenExistingPageSpace();
        }

        /// <summary>
        /// Closes the storage if it is open.
        /// </summary>
        public void Close()
        {
            Dispose();
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
                if (_entries.ContainsKey(pageIndex))
                    RemovePageFromCache(pageIndex);

                _underlyingPageManager.RemovePage(pageIndex);
            }
            finally
            {
                Unlock();
            }
        }

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
            return _underlyingPageManager.CheckPage(page);
        }

        /// <summary>
        /// Gets a page size in bytes.
        /// </summary>
        public int PageSize
        {
            get 
            {
                CheckDisposed();
                return _underlyingPageManager.PageSize; 
            }
        }

        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Flush();
                    var pm = _underlyingPageManager as IDisposable;
                    if (pm != null)
                        pm.Dispose();
                }
                _disposed = true;
            }
        }

        ~CachingPageManager()
        {
            Dispose(false);
        }


        internal CachingPageManager(IPageManager underlyingManager, int maxCachedPages, int maxDirtyPages)
        {
            if (underlyingManager == null)
                throw new ArgumentNullException("underlyingManager");

            _underlyingPageManager = underlyingManager;

            if(maxCachedPages <= 0)
                throw new ArgumentOutOfRangeException("maxCachedPages");

            if (maxDirtyPages < 0 || maxDirtyPages > maxCachedPages)
                throw new ArgumentOutOfRangeException("maxDirtyPages");

            _maxCachedPages = maxCachedPages;
            _maxDirtyPages = maxDirtyPages;
        }
    }
}