namespace DataTanker.PageManagement
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;

    using Utils;

    /// <summary>
    /// Represents a map of on-disk stored pages.
    /// </summary>
    internal class PageMap : IDisposable
    {
        internal class PagemapHeader
        {
            private bool _changed;
            public bool Changed => _changed;

            private long _pageCount;
            public long PageCount
            {
                get { return _pageCount; }
                set
                {
                    _changed = _pageCount != value;
                    _pageCount = value;
                }
            }

            private long _releasedPageCount;
            public long ReleasedPageCount
            {
                get { return _releasedPageCount; }
                set
                {
                    _changed = _releasedPageCount != value;
                    _releasedPageCount = value;
                }
            }

            private long _onDiskPageCount;
            public long OnDiskPageCount
            {
                get { return _onDiskPageCount; }
                set
                {
                    _changed = _onDiskPageCount != value;
                    _onDiskPageCount = value;
                }
            }

            public static int BytesLength => 24;

            public void Write(Stream stream)
            {
                stream.Write(BitConverter.GetBytes(PageCount), 0, sizeof(long));
                stream.Write(BitConverter.GetBytes(OnDiskPageCount), 0, sizeof(long));
                stream.Write(BitConverter.GetBytes(ReleasedPageCount), 0, sizeof(long));
            }

            public void Read(Stream stream)
            {
                var buffer = new byte[sizeof(long)];

                stream.BlockingRead(buffer);
                PageCount = BitConverter.ToInt64(buffer, 0);

                stream.BlockingRead(buffer);
                OnDiskPageCount = BitConverter.ToInt64(buffer, 0);

                stream.BlockingRead(buffer);
                ReleasedPageCount = BitConverter.ToInt64(buffer, 0);

            }
        }

        private bool _disposed;

        private static string _fileName = "pagemap";

        private const int _longSize = sizeof (long);
        private const int _itemSize = sizeof(long) * 2;

        private const int _blockLength = 4096;

        private FileStream _fileStream;

        private PagemapHeader _header;

        private List<long> _freePageIndexes;
        private readonly List<long> _dirtyBlockIndexes = new List<long>();

        private readonly Dictionary<long, byte[]> _blockCache = new Dictionary<long, byte[]>();

        private byte[] GetBytes(long number)
        {
            return BitConverter.GetBytes(number);
        }

        private readonly FileSystemPageManager _pageManager;

        private void Flush(Stream stream)
        {
            if (_pageManager.ForcedWrites)
                stream.Flush();
        }

        private void CheckIfFileExists(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException($"File '{fileName}' not found");
        }

        private string PagemapFileName()
        {
            return _pageManager.Storage.Path + Path.DirectorySeparatorChar + _fileName;
        }

        public void CheckIfFilesExist()
        {
            CheckIfFileExists(PagemapFileName());
        }

        public bool FilesExist()
        {
            return File.Exists(PagemapFileName());
        }

        private static void EnsureFileExists(string fileName)
        {
            if (File.Exists(fileName))
                return;

            using (File.Create(fileName))
            { }
        }

        public void EnsureFilesExist()
        {
            EnsureFileExists(PagemapFileName());
        }

        public bool CanCreate()
        {
            return !File.Exists(PagemapFileName());
        }

        public long GetPageAllocation(long pageIndex)
        {
            if (pageIndex >= _header.PageCount)
                throw new ArgumentException("Too large page index", nameof(pageIndex));

            if (pageIndex < 0)
                throw new ArgumentException("Page index should not be negative", nameof(pageIndex));

            long blockIndex = pageIndex * _itemSize / _blockLength;
            var blockOffset = (int)(pageIndex * _itemSize % _blockLength);

            var blockContent = FetchBlock(blockIndex);

            return BitConverter.ToInt64(blockContent, blockOffset);
        }

        private void CheckReleasedPageExists()
        {
            if (_header.ReleasedPageCount == 0)
                throw new InvalidOperationException("There are no free pages in pagemap");
        }


        public long GetLastFreePageIndex()
        {
            CheckReleasedPageExists();

            long absoluteOffset = _header.PageCount * _itemSize + (_header.ReleasedPageCount - 1) * _longSize;
            long blockIndex = absoluteOffset / _blockLength;
            var blockOffset = (int)(absoluteOffset % _blockLength);

            var blockContent = FetchBlock(blockIndex);

            return BitConverter.ToInt64(blockContent, blockOffset);
        }

        public void TruncateLastFreePageMarker()
        {
            if (_header.ReleasedPageCount == 0)
                throw new InvalidOperationException("There are no free pages in pagemap");

            _freePageIndexes?.RemoveAt(_freePageIndexes.Count - 1);

            _header.ReleasedPageCount--;
        }

        public bool IsPageRemoved(long pageIndex)
        {
            if (_header.ReleasedPageCount == 0)
                return false;

            var freePageIndexes = FreePageIndexes;
            return freePageIndexes.Contains(pageIndex);
        }

        public bool CanAppendPage()
        {
            return _header.ReleasedPageCount == 0;
        }

        public void MarkPageAsFree(long index)
        {
            long absoluteOffset = _header.PageCount * _itemSize + (_header.ReleasedPageCount) * _longSize;
            long blockIndex = absoluteOffset / _blockLength;
            var blockOffset = (int)(absoluteOffset % _blockLength);

            var blockContent = FetchBlock(blockIndex);
            GetBytes(index).CopyTo(blockContent, blockOffset);
            MarkBlockAsDirty(blockContent, blockIndex);

            _header.ReleasedPageCount++;

            _freePageIndexes?.Add(index);
        }

        public long GetMaxPageIndex()
        {
            return _header.PageCount - 1;
        }

        public long GetEmptyPageCount()
        {
            return _header.ReleasedPageCount;
        }

        public IEnumerable<long> FreePageIndexes
        {
            get
            {
                if (_freePageIndexes != null)
                    return _freePageIndexes;

                _freePageIndexes = new List<long>();

                int indexesRead = 0;
                long absoluteOffset = _header.PageCount*_itemSize;
                long blockIndex = absoluteOffset/_blockLength;
                var blockOffset = (int) (absoluteOffset%_blockLength);

                while (indexesRead < _header.ReleasedPageCount)
                {
                    var blockContent = FetchBlock(blockIndex);

                    while (blockOffset < _blockLength && indexesRead < _header.ReleasedPageCount)
                    {
                        _freePageIndexes.Add(BitConverter.ToInt64(blockContent, blockOffset));
                        blockOffset += _longSize;
                        indexesRead++;
                    }
                    blockIndex++;
                    blockOffset = 0;
                }

                return _freePageIndexes;
            }
        }

        public long ReadLastPageIndex()
        {
            long absoluteOffset = (_header.OnDiskPageCount - 1) * _itemSize + _longSize;
            long blockIndex =  absoluteOffset / _blockLength;
            var blockOffset = (int)(absoluteOffset % _blockLength);

            var blockContent = FetchBlock(blockIndex);

            return BitConverter.ToInt64(blockContent, blockOffset);
        }

        public void ClearFreePageMarkers()
        {
            _header.ReleasedPageCount = 0;

            // clear the cache of the free page indexes
            _freePageIndexes = null;
        }

        public void TruncateLastPageIndex()
        {
            if (_header.OnDiskPageCount == 0)
                throw new InvalidOperationException("There are no pages on disk");

            _header.OnDiskPageCount--;
        }

        public void WritePageAllocation(long pageIndex, long allocation)
        {
            if (pageIndex > _header.PageCount)
                throw new ArgumentException("Too large page index", nameof(pageIndex));

            if (pageIndex < 0)
                throw new ArgumentException("Page index should not be negative", nameof(pageIndex));

            long blockIndex = pageIndex * _itemSize / _blockLength;
            var blockOffset = (int)(pageIndex * _itemSize % _blockLength);

            var blockContent = FetchBlock(blockIndex);

            GetBytes(allocation).CopyTo(blockContent, blockOffset);
            MarkBlockAsDirty(blockContent, blockIndex);

            if(pageIndex == _header.PageCount)
            {
                _header.PageCount++;
            }
        }

        public void WritePageIndex(long pageIndex, long allocation)
        {
            long absoluteOffset = allocation / _pageManager.PageSize * _itemSize + _longSize;
            long blockIndex = absoluteOffset / _blockLength;
            var blockOffset = (int)(absoluteOffset % _blockLength);

            var blockContent = FetchBlock(blockIndex);
            GetBytes(pageIndex).CopyTo(blockContent, blockOffset);
            MarkBlockAsDirty(blockContent, blockIndex);

            if (allocation / _pageManager.PageSize == _header.OnDiskPageCount)
            {
                _header.OnDiskPageCount++;
            }
        }

        private void WriteHeader()
        {
            _fileStream.Seek(0, SeekOrigin.Begin);
            _header.Write(_fileStream);
            Flush(_fileStream);
        }

        private void ReadHeader()
        {
            _fileStream.Seek(0, SeekOrigin.Begin);
            _header.Read(_fileStream);
        }

        private void MarkBlockAsDirty(byte[] content, long index)
        {
            _blockCache[index] = content;

            if(!_dirtyBlockIndexes.Contains(index))
                _dirtyBlockIndexes.Add(index);
        }

        private byte[] FetchBlock(long index)
        {
            if (_blockCache.ContainsKey(index))
                return _blockCache[index];

            var result = new byte[_blockLength];
            var position = _blockLength + index * _blockLength;

            if (position == _fileStream.Length)
                return result;

            _fileStream.Seek(position, SeekOrigin.Begin);
            _fileStream.BlockingRead(result);

            _blockCache[index] = result;
            return result;
        }

        public void Flush()
        {
            if (_header.Changed) WriteHeader();

            if (_dirtyBlockIndexes.Any())
            {
                foreach (var blockIndex in _dirtyBlockIndexes)
                {
                    _fileStream.Seek(_blockLength + blockIndex*_blockLength, SeekOrigin.Begin);
                    _fileStream.Write(_blockCache[blockIndex], 0, _blockLength);
                }
                _dirtyBlockIndexes.Clear();
            }

            Flush(_fileStream);
        }

        /// <summary>
        /// Opens an existing pagemap.
        /// </summary>
        public void Open()
        {
            CheckIfFilesExist();

            FileOptions options = _pageManager.ForcedWrites ? FileOptions.WriteThrough : FileOptions.None;
            _fileStream = new FileStream(_pageManager.Storage.Path + Path.DirectorySeparatorChar + _fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096, options);
            _header = new PagemapHeader();

            ReadHeader();

            _opened = true;
        }

        private bool _opened;

        /// <summary>
        /// Creates a pagemap on disk.
        /// </summary>
        public void Create()
        {
            FileOptions options = _pageManager.ForcedWrites ? FileOptions.WriteThrough : FileOptions.None;
            using (_fileStream = new FileStream(_pageManager.Storage.Path + Path.DirectorySeparatorChar + _fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read, _blockLength, options))
            {
                _fileStream.SetLength(_blockLength);
                _header = new PagemapHeader();
                WriteHeader();
            }
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
                    if (_opened)
                    {
                        Flush();
                        _fileStream.Close();
                    }
                }
                _disposed = true;
            }
        }

        ~PageMap()
        {
            Dispose(false);
        }

        /// <summary>
        /// Initializes a new instance of the PageMap.
        /// </summary>
        /// <param name="pageManager">A filesystem page manager instance</param>
        internal PageMap(FileSystemPageManager pageManager)
        {
            _pageManager = pageManager;
        }
    }
}
