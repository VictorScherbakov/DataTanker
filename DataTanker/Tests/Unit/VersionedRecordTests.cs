using System.Linq;

using NUnit.Framework;

using Tests.Emulation;

using DataTanker.Versioning;

namespace Tests
{
    [TestFixture]
    public class VersionedRecordTests
    {
        [Test]
        public void RecordIsVisibleToItsCreator()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] {0, 1};

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            Assert.IsTrue(record.HasVisibleVersionTo(0));
        }

        [Test]
        public void UpdateProduceNewVersion()
        {
            var memoryManager = new MemoryManager();

            var oldContent = new byte[] { 0, 1 };
            var newContent = new byte[] { 2, 3 };

            var recordReference = VersionedRecord.CreateNew(oldContent, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            record.Update(newContent, 0);

            Assert.IsTrue(record.HasVisibleVersionTo(0));
            Assert.IsTrue(newContent.SequenceEqual(record.GetMatchingVersion(0).RawData));
        }

        [Test]
        public void ExpiredVersionIsInvisible()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);
            record.Expire(0);

            Assert.IsFalse(record.HasVisibleVersionTo(0));
        }

        [Test]
        public void VersionCreatedByUncommitedTransactionIsInvisible()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            Assert.IsFalse(record.HasVisibleVersionTo(1));
        }

        [Test]
        public void VersionCreatedByCommitedTransactionIsVisible()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new[] { 0 });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            Assert.IsTrue(record.HasVisibleVersionTo(1));
        }

        [Test]
        public void UpdateOfExpiredRecordProduceWriteConflict()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            record.Expire(0); // zero transaction expires the record
            Assert.That(() => record.Update(new byte[] { 2, 3 }, 1), Throws.Exception.TypeOf<WriteConflictException>()); // first transaction trying to update it
        }

        [Test]
        public void ExpireOfExpiredRecordProduceWriteConflict()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            record.Expire(0); // zero transaction expires the record
            Assert.That(() => record.Expire(1), Throws.Exception.TypeOf<WriteConflictException>()); // first transaction trying to expire it
        }

        [Test]
        public void UpdateOfUpdatedRecordProduceWriteConflict()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            record.Update(new byte[] { 2, 3 }, 0); // zero transaction updates the record
            Assert.That(() => record.Update(new byte[] { 4, 5 }, 1), Throws.Exception.TypeOf<WriteConflictException>());  // first transaction trying to update it
        }

        [Test]
        public void ExpireOfUpdatedRecordProduceWriteConflict()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            record.Update(new byte[] { 2, 3 }, 0); // zero transaction updates the record
            Assert.That(() => record.Expire(1), Throws.Exception.TypeOf<WriteConflictException>()); // first transaction trying to expire it
        }

        [Test]
        public void UpdateRecordCreatedByCommitedTransaction()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };
            var newContent = new byte[] { 2, 3 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new[] { 0 }); // zero transaction is commited

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            record.Update(newContent, 1); 
            Assert.IsTrue(newContent.SequenceEqual(record.GetMatchingVersion(1).RawData));
        }

        [Test]
        public void ResurrectRecordExpiredByCommitedTransaction()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };
            var newContent = new byte[] { 2, 3 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { }); 

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            var newReference = record.Expire(0);

            snapshotData = new SnapshotData(new[] { 0 });

            record = new VersionedRecord(memoryManager.Get(newReference).RawData, memoryManager, snapshotData);

            record.Insert(newContent, 1);
            Assert.IsTrue(newContent.SequenceEqual(record.GetMatchingVersion(1).RawData));
        }

        [Test]
        public void ResurrectRecordCreatedAndExpiredByRolledBackTransaction()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };
            var newContent = new byte[] { 2, 3 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new int[] { });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            var newReference = record.Expire(0);

            snapshotData = new SnapshotData(new int[] { }, new[] { 0 });

            record = new VersionedRecord(memoryManager.Get(newReference).RawData, memoryManager, snapshotData);

            record.Insert(newContent, 1);
            Assert.IsTrue(newContent.SequenceEqual(record.GetMatchingVersion(1).RawData));
        }

        [Test]
        public void UpdateRecordExpiredByRolledBackTransaction()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };
            var newContent = new byte[] { 2, 3 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new[] { 0 });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            var newReference = record.Expire(1);

            snapshotData = new SnapshotData(new[] { 0 }, new[] { 1 });

            record = new VersionedRecord(memoryManager.Get(newReference).RawData, memoryManager, snapshotData);

            record.Update(newContent, 2);
            Assert.IsTrue(newContent.SequenceEqual(record.GetMatchingVersion(2).RawData));
        }

        [Test]
        public void ConcurrentlyRunningTransactionsViewTheirOwnVersions()
        {
            var memoryManager = new MemoryManager();

            var content = new byte[] { 0, 1 };
            var newContent = new byte[] { 2, 3 };

            var recordReference = VersionedRecord.CreateNew(content, 0, memoryManager);

            var snapshotData = new SnapshotData(new[] { 0 });

            var record = new VersionedRecord(memoryManager.Get(recordReference).RawData, memoryManager, snapshotData);

            var newReference = record.InsertOrUpdate(newContent, 1);
            record = new VersionedRecord(memoryManager.Get(newReference).RawData, memoryManager, snapshotData);

            Assert.IsTrue(newContent.SequenceEqual(record.GetMatchingVersion(1).RawData));
            Assert.IsTrue(content.SequenceEqual(record.GetMatchingVersion(2).RawData));
        }
    }
}
