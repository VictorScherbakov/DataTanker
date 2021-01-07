namespace DataTanker
{
    using System;
    using System.Diagnostics;
    using System.IO;

    using PageManagement;
    using BinaryFormat.Page;
    using Settings;
    using Utils;

    using FlushExcepEvent = System.EventHandler<StorageFlushExceptionEventArgs>;

    /// <summary>
    /// Represents a data Storage.
    /// </summary>
    internal class Storage : IStorage
    {
        private bool _disposed;

        private readonly TimeSpan _autoFlushTimeout;
        private string _path = string.Empty;
        private bool _isOpen;

        private string _infoFileName = "info";

        private StorageInfo _info;

        public event FlushExcepEvent OnFlushException;

        public StorageInfo Info => _info;

        private string InfoFileName()
        {
            return Path + System.IO.Path.DirectorySeparatorChar + _infoFileName;
        }

        protected virtual void Init()
        {
            // add header page
            IPage headingPage = PageManager.CreatePage();

            Debug.Assert(headingPage.Index == 0, "The header page should have zero index");

            var hph = new HeadingPageHeader
                          {
                              FsmPageIndex = 1,
                              AccessMethodPageIndex = 2,
                              PageSize = PageManager.PageSize,
                              OnDiskStructureVersion = OnDiskStructureVersion,
                              AccessMethod = (short)AccessMethod
                          };

            PageFormatter.InitPage(headingPage, hph);
            PageManager.UpdatePage(headingPage);

            // add the first free-space-map page
            IPage fsmPage = PageManager.CreatePage();

            Debug.Assert(fsmPage.Index == 1, "The first free-space-map page should have index 1");

            var fsmh = new FreeSpaceMapPageHeader
                           {
                               StartPageIndex = fsmPage.Index,
                               PreviousPageIndex = -1,
                               NextPageIndex = -1,
                               BasePageIndex = 0
                           };

            PageFormatter.InitPage(fsmPage, fsmh);
            PageFormatter.SetAllFsmValues(fsmPage, FsmValue.Full);
            PageManager.UpdatePage(fsmPage);
        }

        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("Storage");
        }

        private long _updateOperationCount;
        private readonly long _autoFlushInterval;
        readonly TimerHelper _flushTimer;

        protected void EditOperationFinished()
        {
            _updateOperationCount++;
            if (_autoFlushInterval > 0 && _updateOperationCount > _autoFlushInterval)
                Flush();
            else
            {
                if(_autoFlushTimeout > TimeSpan.Zero)
                    _flushTimer.Start(_autoFlushTimeout);
            }
        }

        #region IStorage Members

        /// <summary>
        /// Gets the on-disk structure version.
        /// </summary>
        public int OnDiskStructureVersion => 1;

        /// <summary>
        /// Gets the access method implemented by this storage
        /// </summary>
        public virtual AccessMethod AccessMethod => AccessMethod.Undefined;

        /// <summary>
        /// Opens an existing Storage.
        /// </summary>
        /// <param name="path">A string containing information about storage location</param>
        public void OpenExisting(string path)
        {
            CheckDisposed();

            if (PageManager == null)
                throw new InvalidOperationException("Page manager is not set");

            if (_isOpen)
                throw new InvalidOperationException("Storage is already open");

            _path = path;

            ReadInfo();
            CheckInfo();

            _isOpen = true;
            PageManager.OpenExistingPageSpace();


            IPage headingPage = PageManager.FetchPage(0);
            var header = PageFormatter.GetPageHeader(headingPage);
            var headingHeader = (HeadingPageHeader) header;

            if(headingHeader == null)
                throw new StorageFormatException("Heading page not found");

            if (headingHeader.PageSize != PageSize)
            {
                var pageSize = PageSize;
                _isOpen = false;
                Close();
                throw new StorageFormatException($"Page size: {pageSize} bytes is set. But pages of the opening storage is {headingHeader.PageSize} bytes length");
            }

            if(headingHeader.OnDiskStructureVersion != OnDiskStructureVersion)
            {
                _isOpen = false;
                Close();
                throw new NotSupportedException($"On-disk structure version {headingHeader.OnDiskStructureVersion} is not supported.");
            }

            if (headingHeader.AccessMethod != (short) AccessMethod)
            {
                _isOpen = false;
                Close();
                throw new NotSupportedException($"Access method {headingHeader.AccessMethod} is not supported by this instance of storage.");
            }

            PageManager.EnterAtomicOperation();
        }

        private void ReadInfo()
        {
            var infoFileName = InfoFileName();
            if (!File.Exists(infoFileName))
                throw new FileNotFoundException($"File '{infoFileName}' not found");

            var infoString = File.ReadAllText(infoFileName);
            _info = StorageInfo.FromString(infoString);
        }

        protected virtual void CheckInfo()
        {
            if (_info.StorageClrTypeName != GetType().FullName)
                throw new DataTankerException("Mismatch storage type");
        }

        /// <summary>
        /// Opens existing storage or creates a new one.
        /// </summary>
        /// <param name="path">A string containing information about storage location</param>
        public void OpenOrCreate(string path)
        {
            CheckDisposed();

            if (PageManager == null)
                throw new InvalidOperationException("Page manager is not set");

            if (_isOpen)
                throw new InvalidOperationException("Storage is already open");

            _path = path;
            PageManager.Lock();
            try
            {
                if(PageManager.CanCreate())
                    CreateNew(path);
                else
                    OpenExisting(path);
            }
            finally
            {
                PageManager.Unlock();
            }

        }

        /// <summary>
        /// Creates a new Storage.
        /// </summary>
        /// <param name="path">A string containing information about storage location</param>
        public void CreateNew(string path)
        {
            CheckDisposed();

            if (PageManager == null)
                throw new InvalidOperationException("Page manager is not set");

            if (_isOpen)
                throw new InvalidOperationException("Unable to create starage because this instance is using to operate with the other storage");

            _path = path;
            FillInfo();
            WriteInfo();

            PageManager.CreateNewPageSpace();
            _isOpen = true;

            PageManager.Lock();
            try
            {
                PageManager.EnterAtomicOperation();

                Init();

                PageManager.ExitAtomicOperation();
                PageManager.EnterAtomicOperation();
            }
            finally
            {
                PageManager.Unlock();
            }
        }

        private void WriteInfo()
        {
            if (!File.Exists(InfoFileName()))
                File.WriteAllText(InfoFileName(), _info.ToString());
            else throw new DataTankerException("Storage cannot be created here. Files with names matching the names of storage files already exist. Try to call OpenExisting().");
        }

        protected virtual void FillInfo()
        {
            _info = new StorageInfo
            {
                StorageClrTypeName = GetType().FullName
            };
        }

        /// <summary>
        /// Closes the Storage if it is open.  Actually calls Dispose() method.
        /// </summary>
        public void Close()
        {
            Dispose();
            _isOpen = false;
        }

        /// <summary>
        /// Clears buffers for this Storage and causes any buffered data to be written.
        /// </summary>
        public void Flush()
        {
            CheckDisposed();

            if (PageManager != null)
            {
                _flushTimer.Stop();

                (PageManager as ICachingPageManager)?.Flush();

                PageManager.ExitAtomicOperation();
                PageManager.EnterAtomicOperation();
            }

            _updateOperationCount = 0;
        }

        /// <summary>
        /// Gets a page size in bytes.
        /// Page is a data block that is write and read entirely.
        /// </summary>
        public int PageSize => PageManager.PageSize;

        /// <summary>
        /// Gets a Storage location.
        /// </summary>
        public string Path
        {
            get { return _path; }
            internal set { _path = value; }
        }

        /// <summary>
        /// Gets a value indicating whether a Storage is open.
        /// </summary>
        public bool IsOpen => _isOpen;

        #endregion

        protected IPageManager PageManager { get; }

        /// <summary> Initializes a new instance of the Storage.
        /// </summary>
        /// <param name="pageManager">The FileSystemPageManager instance</param>
        internal Storage(IPageManager pageManager)
            :this(pageManager, TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Storage.
        /// </summary>
        /// <param name="pageManager">The FileSystemPageManager instance</param>
        /// <param name="autoFlushTimeout"></param>
        /// <param name="autoFlushInterval"></param>
        internal Storage(IPageManager pageManager, TimeSpan autoFlushTimeout, long autoFlushInterval = 10000)
        {
            _autoFlushTimeout = autoFlushTimeout;
            _autoFlushInterval = autoFlushInterval;
            _flushTimer = new TimerHelper();
            _flushTimer.Elapsed += (timer, o) => {
                // We wrap |Flush| with try...catch... because, in case we have
                // a storage corruption, this might throw an exception (e.g.
                // IndexOutOfRangeException) and we would like to give the
                // library user a chance to stop gracefully.
                try
                {
                    Flush();
                }
                catch (Exception ex)
                {
                    _flushTimer.Stop();
                    TriggerFlushException(ex);
                }
            };

            PageManager = pageManager;
            pageManager.Storage = this;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="ex"></param>
        private void TriggerFlushException(Exception ex)
        {
            if (OnFlushException == null)
                return;

            var args = new StorageFlushExceptionEventArgs() {
                FlushException = ex
            };

            OnFlushException.Invoke(this, args);
        }

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
                    _flushTimer.Dispose();
                    Flush();
                    (PageManager as IDisposable)?.Dispose();
                }
                _disposed = true;
            }
        }

        ~Storage()
        {
            Dispose(false);
        }
    }
}