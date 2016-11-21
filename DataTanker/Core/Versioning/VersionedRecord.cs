namespace DataTanker.Versioning
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using MemoryManagement;

    internal class VersionedRecord
    {
        private readonly IMemoryManager _memoryManager;
        private readonly ISnapshotData _snapshotData;
        private readonly List<VersionInfo> _versions = new List<VersionInfo>();

        private bool IsCommited(int transactionNumber)
        {
            return _snapshotData.IsComittedTransaction(transactionNumber);
        }

        private bool IsRolledBack(int transactionNumber)
        {
            return _snapshotData.IsRolledBackTransaction(transactionNumber);
        }

        private void SetExpirationMarker(int transactionNumber)
        {
            var version = _versions.FirstOrDefault(v => IsVersionVisibleTo(v, transactionNumber));
            if (version == null)
                throw new WriteConflictException("Record has no visible versions");

            version.ExpireChangeNumber = transactionNumber;
        }

        public DbItemReference AllocateNew()
        {
            byte[] versionInfoBytes;
            using (var ms = new MemoryStream(VersionInfo.BytesLength * _versions.Count))
            {
                foreach (var v in _versions)
                    v.Write(ms);

                versionInfoBytes = ms.ToArray();
            }

            return _memoryManager.Allocate(versionInfoBytes);
        }

        private bool IsVersionVisibleTo(VersionInfo v, int transactionNumber)
        {
            return (v.CreateChangeNumber == transactionNumber && v.ExpireChangeNumber == -1) || // the version is created by this transaction and has not been deleted or
                    (IsCommited(v.CreateChangeNumber) &&                                        // the version is created by commited transaction 
                        (v.ExpireChangeNumber == -1 ||                                          // and has not been deleted yet or
                        (v.ExpireChangeNumber != transactionNumber                              // the version is deleted by another
                        && !IsCommited(v.ExpireChangeNumber))));                                // uncommited transaction
        }

        public bool HasVisibleVersionTo(int transactionNumber)
        {
            return _versions.Any(v => IsVersionVisibleTo(v, transactionNumber));
        }

        public DbItemReference GetMatchingVersionReference(int transactionNumber)
        {
            return _versions
                .Where(version => IsVersionVisibleTo(version, transactionNumber))
                .Select(version => version.VersionReference)
                .FirstOrDefault();
        }

        public DbItem GetMatchingVersion(int transactionNumber)
        {
            return _versions
                .Where(version => IsVersionVisibleTo(version, transactionNumber))
                .Select(version => _memoryManager.Get(version.VersionReference))
                .FirstOrDefault();
        }

        public DbItemReference Insert(byte[] body, int transactionNumber)
        {
            if(_versions.All(v => 
                (IsCommited(v.CreateChangeNumber) && IsCommited(v.ExpireChangeNumber)) || // the version is created and expired by commited transaction 
                 IsRolledBack(v.CreateChangeNumber)))                                     // or the version is created by rolled back transaction
            {
                var reference = _memoryManager.Allocate(body);

                _versions.Add(new VersionInfo
                {
                    CreateChangeNumber = transactionNumber,
                    ExpireChangeNumber = -1,
                    VersionReference = reference
                });

                return AllocateNew();
            }

            throw new WriteConflictException("Cannot insert record.");
        }


        public DbItemReference InsertOrUpdate(byte[] body, int transactionNumber)
        {
            var version = _versions.FirstOrDefault(v => IsVersionVisibleTo(v, transactionNumber));
            return version != null 
                ? Update(body, transactionNumber) 
                : Insert(body, transactionNumber);
        }

        public DbItemReference Update(byte[] body, int transactionNumber)
        {
            var version = _versions.FirstOrDefault(v => IsVersionVisibleTo(v, transactionNumber));
            if (version != null)
            {
                var reference = _memoryManager.Allocate(body);

                if (version.CreateChangeNumber == transactionNumber)
                {
                    // we update the version created by this transaction
                    _memoryManager.Free(version.VersionReference);
                    version.VersionReference = reference;
                }
                else
                {
                    SetExpirationMarker(transactionNumber);

                    _versions.Add(new VersionInfo
                                      {
                                          CreateChangeNumber = transactionNumber,
                                          ExpireChangeNumber = -1,
                                          VersionReference = reference
                                      });

                }
                return AllocateNew();
            }

            throw new WriteConflictException("Record has no visible versions");
        }

        public DbItemReference Expire(int transactionNumber)
        {
            SetExpirationMarker(transactionNumber);
            return AllocateNew();
        }

        public static DbItemReference CreateNew(byte[] body, int transactionNumber, IMemoryManager memoryManager)
        {
            if (body == null) 
                throw new ArgumentNullException(nameof(body));

            if (memoryManager == null) 
                throw new ArgumentNullException(nameof(memoryManager));

            var bodyReference = memoryManager.Allocate(body);
            var versionInfo = new VersionInfo
                                  {
                                      CreateChangeNumber = transactionNumber,
                                      ExpireChangeNumber = -1,
                                      VersionReference = bodyReference
                                  };

            byte[] versionInfoBytes;
            using (var ms = new MemoryStream(VersionInfo.BytesLength))
            {
                versionInfo.Write(ms);
                versionInfoBytes = ms.ToArray();
            }

            return memoryManager.Allocate(versionInfoBytes);
        }

        public VersionedRecord(byte[] versionInfoBytes, IMemoryManager memoryManager, ISnapshotData snapshotData)
        {
            if (versionInfoBytes == null) 
                throw new ArgumentNullException(nameof(versionInfoBytes));

            if (memoryManager == null) 
                throw new ArgumentNullException(nameof(memoryManager));

            if (snapshotData == null) 
                throw new ArgumentNullException(nameof(snapshotData));

            using (var ms = new MemoryStream(versionInfoBytes))
            {
                while (ms.Position < ms.Length)
                    _versions.Add(VersionInfo.Read(ms));
            }
            _memoryManager = memoryManager;
            _snapshotData = snapshotData;
        }
    }
}
