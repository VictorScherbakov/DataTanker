namespace DataTanker.Recovery
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Diagnostics;
    using System.Linq;

    using Utils;
    using PageManagement;

    /// <summary>
    /// Represents a file used to collect all the changes before they are applied to the primary file.
    /// This prevents corruption in cases of partial writes, inconsistently removed pages etc.
    /// </summary>
    internal class RecoveryFile : IDisposable
    {
        private readonly FileSystemPageManager _pageManager;
        private readonly int _pageSize;

        private FileStream _stream;
        private bool _writeDisabled;

        private string _fileName = "recovery";

        private class UpdateRecord
        {
            public byte[] Content { get; set; }
            public long Offset { get; set; }
        }

        private readonly Dictionary<long, UpdateRecord> _updatedPages = new Dictionary<long, UpdateRecord>();
        private readonly HashSet<long> _deletedPages = new HashSet<long>();

        private FileStream Stream
        {
            get 
            {
                if (_stream == null)
                {
                    if (new FileInfo(FileName).Length > 0)
                        _writeDisabled = true; //prohibit writing when already exists non-empty file

                    FileOptions options = _pageManager.ForcedWrites ? FileOptions.WriteThrough : FileOptions.None;

                    _stream = new FileStream(_pageManager.Storage.Path + Path.DirectorySeparatorChar + _fileName,
                        FileMode.Open, FileAccess.ReadWrite, FileShare.Read, 4096 * 100, options);
                }

                return _stream;
            }
        }

        public string FileName => _pageManager.Storage.Path + Path.DirectorySeparatorChar + _fileName;

        /// <summary>
        /// Determine if the page is present in recovery file as updated
        /// </summary>
        /// <param name="pageIndex">The index of requested page</param>
        /// <returns>True if updated, false otherwise</returns>
        public bool IsPageUpdated(long pageIndex)
        {
            return _updatedPages.ContainsKey(pageIndex);
        }

        /// <summary>
        /// Returns a content of updated page.
        /// </summary>
        /// <param name="pageIndex">The index of requested page</param>
        /// <returns>Content of page</returns>
        public byte[] GetUpdatedPageContent(long pageIndex)
        {
            return _updatedPages[pageIndex].Content;
        }

        /// <summary>
        /// Returns a list of all pages presented in the recovery file as updated
        /// </summary>
        public IList<long> UpdatedPageIndexes => _updatedPages.Keys.ToList();

        /// <summary>
        /// Returns a set of all pages presented in the recovery file as deleted
        /// </summary>
        public HashSet<long> DeletedPageIndexes => _deletedPages;

        private static void EnsureFileExists(string fileName)
        {
            if (File.Exists(fileName))
                return;

            using (File.Create(fileName))
            { }
        }

        /// <summary>
        /// Initializes a new instance of the RecoveryFile
        /// </summary>
        /// <param name="pageManager"></param>
        /// <param name="pageSize"></param>
        public RecoveryFile(FileSystemPageManager pageManager, int pageSize)
        {
            _pageManager = pageManager ?? throw new ArgumentNullException(nameof(pageManager));
            _pageSize = pageSize;
        }

        /// <summary>
        /// Writes an information of the updated page to the recovery file
        /// </summary>
        /// <param name="page">An updated page</param>
        public void WriteUpdatePageRecord(IPage page)
        {
            CheckDisposed();
            CheckWriteEnabled();

            EnsureFileExists(FileName);

            var record = new RecoveryRecord
            {
                RecordType = RecoveryRecordType.Update,
                PageIndex = page.Index,
                PageContent = page.ContentCopy
            };

            var updateRecord = new UpdateRecord { Content = record.PageContent, Offset = Stream.Length};

            if (_updatedPages.ContainsKey(page.Index))
                _updatedPages[page.Index] = updateRecord;
            else
                _updatedPages.Add(page.Index, new UpdateRecord { Content = record.PageContent, Offset = Stream.Length });

            WriteRecordToDisk(record);
        }

        private void WriteRecordToDisk(RecoveryRecord record, long? offset = null)
        {
            if (offset.HasValue)
                Stream.Seek(offset.Value, SeekOrigin.Begin);

            byte[] intBytes = BitConverter.GetBytes((int)record.RecordType);
            Stream.Write(intBytes, 0, intBytes.Length);

            byte[] longBytes = BitConverter.GetBytes(record.PageIndex);
            switch (record.RecordType)
            {
                case RecoveryRecordType.Update:
                    Stream.Write(longBytes, 0, longBytes.Length); // page index
                    Stream.Write(BitConverter.GetBytes(_pageSize), 0, sizeof(int)); // page size
                    Stream.Write(record.PageContent, 0, record.PageContent.Length); // page content
                    break;
                case RecoveryRecordType.Delete:
                    Stream.Write(longBytes, 0, longBytes.Length);
                    break;
                case RecoveryRecordType.Final:
                    longBytes = BitConverter.GetBytes(_stream.Position + 8);
                    Stream.Write(longBytes, 0, longBytes.Length);
                    Stream.Flush();
                    break;
            }

            if (offset.HasValue)
                Stream.Seek(0, SeekOrigin.End);
        }

        /// <summary>
        /// Writes an information of the deleted page to the recovery file
        /// </summary>
        public void WriteDeletePageRecord(long pageIndex)
        {
            CheckDisposed();
            CheckWriteEnabled();
            EnsureFileExists(FileName);

            if (!_deletedPages.Contains(pageIndex))
            {
                _deletedPages.Add(pageIndex);
                if (_updatedPages.ContainsKey(pageIndex))
                    _updatedPages.Remove(pageIndex);
            }
            else
                throw new PageMapException("Page is already removed");

            var record = new RecoveryRecord
            {
                RecordType = RecoveryRecordType.Delete,
                PageIndex = pageIndex,
            };

            WriteRecordToDisk(record);
        }

        private void CheckWriteEnabled()
        {
            if(_writeDisabled)
                throw new InvalidOperationException("Unable to write recovery file");
        }

        /// <summary>
        /// Writes a marker indicating whether the atomic operation complete
        /// and this recovery file can be used to recover the storage to its 
        /// consistent state
        /// </summary>
        public void WriteFinalMarker()
        {
            CheckDisposed();

            CheckWriteEnabled();
            EnsureFileExists(FileName);

            WriteRecordToDisk(new RecoveryRecord { RecordType = RecoveryRecordType.Final });

            // Now all changes complete.
            // We need the OS to flush any buffered data to disk
            // to provide the durability of changes.
            Stream.Flush();
        }
        
        /// <summary>
        /// Gets a value indicating whether final marked is correctly 
        /// written out for this recovery file
        /// </summary>
        public bool CorrectlyFinished
        {
            get
            {
                CheckDisposed();

                const int finalRecordSize = sizeof (int) + sizeof (long);
                if (Stream.Length >= finalRecordSize)
                {
                    Stream.Seek(-finalRecordSize, SeekOrigin.End);
                    var intBytes = new byte[sizeof (int)];
                    Stream.BlockingRead(intBytes);
                    if ((RecoveryRecordType)BitConverter.ToInt32(intBytes, 0) == RecoveryRecordType.Final)
                    {
                        var longBytes = new byte[sizeof(long)];
                        Stream.BlockingRead(longBytes);
                        if (BitConverter.ToInt64(longBytes, 0) == Stream.Length)
                            return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the recovery file exists
        /// </summary>
        public bool Exists => File.Exists(FileName);

        /// <summary>
        /// Cancels all records in the recovery file (if any) and start 
        /// and starts a new cycle of updates.
        /// </summary>
        public void Reset()
        {
            CheckDisposed();
            if (Exists)
            {
                Stream.SetLength(0);
                Stream.Close();
                _stream = null;

                _deletedPages.Clear();
                _updatedPages.Clear();

                File.Delete(FileName);
            }
        }

        /// <summary>
        /// Gets a record corresponding the specified index of page.
        /// </summary>
        /// <param name="pageIndex">An index of page</param>
        /// <returns>A RecoveryRecord instance corresponding the requested index of page. 
        /// Returns null if the record is not found</returns>
        public RecoveryRecord GetRecoveryRecord(long pageIndex)
        {
            CheckDisposed();
            if(_deletedPages.Contains(pageIndex))
                return new RecoveryRecord
                {
                    PageIndex =  pageIndex, 
                    RecordType = RecoveryRecordType.Delete
                };

            if (_updatedPages.ContainsKey(pageIndex))
                return new RecoveryRecord
                {
                    PageIndex = pageIndex, 
                    PageContent = _updatedPages[pageIndex].Content, 
                    RecordType = RecoveryRecordType.Update
                };

            return null;
        }

        private RecoveryRecord ReadRecord()
        {
            var intBytes = new byte[sizeof(int)];
            Stream.BlockingRead(intBytes);

            var rt = (RecoveryRecordType) BitConverter.ToInt32(intBytes, 0);

            var longBytes = new byte[sizeof(long)];

            switch (rt)
            {
                case RecoveryRecordType.Update:
                    Stream.BlockingRead(longBytes); // page index
                    Stream.BlockingRead(intBytes); // page length
                    var length = BitConverter.ToInt32(intBytes, 0);
                    var content = new byte[length];
                    Stream.BlockingRead(content); // page content
                    return new RecoveryRecord
                    {
                        RecordType = RecoveryRecordType.Update,
                        PageContent = content,
                        PageIndex = BitConverter.ToInt64(longBytes, 0)
                    };
                case RecoveryRecordType.Delete:
                    Stream.BlockingRead(longBytes); // page index
                    return new RecoveryRecord
                    {
                        RecordType = RecoveryRecordType.Delete,
                        PageIndex = BitConverter.ToInt64(longBytes, 0)
                    };
                case RecoveryRecordType.Final:
                    Stream.BlockingRead(longBytes);
                    Debug.Assert(BitConverter.ToInt64(longBytes, 0) == _stream.Position);

                    return new RecoveryRecord
                    {
                        RecordType = RecoveryRecordType.Final
                    };
            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads all records from disk.
        /// </summary>
        public void ReadAllRecoveryRecords()
        {
            CheckDisposed();
            _deletedPages.Clear();
            _updatedPages.Clear();
            Stream.Seek(0, SeekOrigin.Begin);
            try
            {
                while (Stream.Position < Stream.Length)
                {
                    var pos = Stream.Position;
                    var record = ReadRecord();
                    if (record.RecordType == RecoveryRecordType.Delete)
                    {
                        _deletedPages.Add(record.PageIndex);
                        if (_updatedPages.ContainsKey(record.PageIndex))
                            _updatedPages.Remove(record.PageIndex);
                    }
                    else if (record.RecordType == RecoveryRecordType.Update)
                    {
                        var r = new UpdateRecord {Content = record.PageContent, Offset = pos};
                        if (_updatedPages.ContainsKey(record.PageIndex))
                            _updatedPages[record.PageIndex] = r;
                        else
                            _updatedPages.Add(record.PageIndex, r);

                        if (_deletedPages.Contains(record.PageIndex))
                            _deletedPages.Remove(record.PageIndex);
                    }
                }
            }
            finally
            {
                Stream.Seek(0, SeekOrigin.End);
            }
        }

        private bool _disposed;

        [DebuggerNonUserCode]
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("RecoveryFile");
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
                    _stream?.Close();

                _disposed = true;
            }
        }

        ~RecoveryFile()
        {
            Dispose(false);
        }
    }
}