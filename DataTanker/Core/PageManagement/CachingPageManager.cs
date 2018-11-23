namespace DataTanker.PageManagement
{
    using System.Linq;
    using System.Collections.Generic;
    using System;

    /// <summary>
    /// Wraps the underlying page manager with in-memory caching.
    /// <remarks>
    /// This class basically implements the LFU approach. The main difference with classic LFU is that
    /// the eviction routine is called less frequently, removes many pages at once and resets the hit counters of all pages.
    /// </remarks>
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

        private Dictionary<long, PageCacheEntry> _entries = new Dictionary<long, PageCacheEntry>();

        private static double _truncationSize = 0.7;

        private void Truncate()
        {
            // fill temp list with cached pages
            var temp = _entries.Values.ToList();
            _entries = new Dictionary<long, PageCacheEntry>();

            // order list by hits
            temp.Sort((w1, w2) => -(w1.HitCount.CompareTo(w2.HitCount)));

            var truncation = (temp.Count - 1) * _truncationSize;

            for (int i = 0; i < temp.Count; i++)
            {
                var t = temp[i];
                if (i < truncation || t.Pinned)
                {
                    t.HitCount = 0;
                    _entries.Add(t.Page.Index, t);
                }
                else
                {
                    // force underlying update if the removing page is dirty
                    if (t.IsDirty)
                    {
                        _underlyingPageManager.UpdatePage(t.Page);
                        _dirtyPageCount--;
                    }
                }
            }
        }

        private void AddPage(IPage page)
        {
            if (_entries.Count >= _maxCachedPages)
                Truncate();

            _entries.Add(page.Index, new PageCacheEntry {Page = page, HitCount = 1});
        }

        private void RemovePageFromCache(long pageIndex)
        {
            var entry = _entries[pageIndex];
            if (entry.IsDirty)
                _dirtyPageCount--;

            _entries.Remove(pageIndex);
        }

        private IPage GetPage(long pageIndex)
        {
            var entry = _entries[pageIndex];

            entry.HitCount++;
            return entry.Page;
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
        /// Switches page manager instance to the atomic operation mode.
        /// In such a mode, all further changes can be applied all at once 
        /// by calling ExitAtomicOperation() method or canceled.
        /// </summary>
        public void EnterAtomicOperation()
        {
            _underlyingPageManager.EnterAtomicOperation();
        }

        /// <summary>
        /// Switches page manager instance to normal mode.
        /// All the changes made since the last EnterAtomicOperation() call are applied.
        /// </summary>
        public void ExitAtomicOperation()
        {
            Flush();
            _underlyingPageManager.ExitAtomicOperation();
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
                if (_dirtyPageCount > 0)
                {
                    foreach (PageCacheEntry cacheEntry in _entries.Values.Where(entry => entry.IsDirty && !entry.Pinned))
                    {
                        _underlyingPageManager.UpdatePage(cacheEntry.Page);
                        cacheEntry.IsDirty = false;
                        _dirtyPageCount--;
                    }
                }
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
                    return GetPage(pageIndex);
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
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (content.Length != _underlyingPageManager.PageSize) throw new ArgumentException(nameof(content));

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
                    _entries[page.Index].Page = page;
                }
                else
                {
#if DEBUG
                    if (_maxDirtyPages > 0)
                    {
                        // check if page exists
                        if(!_underlyingPageManager.PageExists(page.Index))
                            throw new ArgumentException("Page does not exists in storage", nameof(page));
                    }
#endif
                    _entries.Add(page.Index, new PageCacheEntry { Page = page });

                    _unsuccessfulHitCount++;
                }

                // touch it
                _entries[page.Index].HitCount++;

                if (_maxDirtyPages == 0)
                {
                    // dirty pages are not allowed, force underlying update
                    _underlyingPageManager.UpdatePage(page);
                }
                else
                {
                    if (!_entries[page.Index].IsDirty)
                        _dirtyPageCount++;

                    _entries[page.Index].IsDirty = true;

                    // maybe we need to flush all dirty pages
                    if (_dirtyPageCount > _maxDirtyPages)
                        Flush();
                }

                if (_entries.Count >= _maxCachedPages)
                    Truncate();
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
        /// Pins the page
        /// </summary>
        public void PinPage(long index)
        {
            if(_entries.ContainsKey(index))
                _entries[index].Pinned = true;
        }

        /// <summary>
        /// Unpins the page
        /// </summary>
        public void UnpinPage(long index)
        {
            if (_entries.ContainsKey(index))
                _entries[index].Pinned = false;
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
                    (_underlyingPageManager as IDisposable)?.Dispose();
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
            _underlyingPageManager = underlyingManager ?? throw new ArgumentNullException(nameof(underlyingManager));

            if(maxCachedPages <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxCachedPages));

            if (maxDirtyPages < 0 || maxDirtyPages > maxCachedPages)
                throw new ArgumentOutOfRangeException(nameof(maxDirtyPages));

            _maxCachedPages = maxCachedPages;
            _maxDirtyPages = maxDirtyPages;
        }
    }
}