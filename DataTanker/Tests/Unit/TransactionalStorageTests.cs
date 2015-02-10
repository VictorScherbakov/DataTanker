using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using DataTanker.PageManagement;
using DataTanker.Transactions;
using DataTanker.Versioning;
using DataTanker;

namespace Tests
{
    [TestFixture]
    public class TransactionalStorageTests
    {
        private string _workPath = "..\\..\\Storages";

        [TearDown]
        public void Cleanup()
        {
            string[] files = Directory.GetFiles(_workPath);
            foreach (string file in files)
                File.Delete(file);
        }

        [Test]
        public void CreatedTransactionShouldBeActive()
        {
            using (var storage = new TransactionalStorage(new FileSystemPageManager(4096)))
            {
                storage.CreateNew(_workPath);
                ISnapshotData snapshotData;
                var number = storage.CreateTransaction(out snapshotData);
                Assert.AreEqual(TransactionState.Active, storage.GetState(number));
            }
        }

        [Test]
        public void SuccessfulySetState()
        {
            using (var storage = new TransactionalStorage(new FileSystemPageManager(4096)))
            {
                storage.CreateNew(_workPath);

                ISnapshotData snapshotData;
                var number = storage.CreateTransaction(out snapshotData);

                storage.Mark(number, TransactionState.Commited);
                Assert.AreEqual(TransactionState.Commited, storage.GetState(number));

                storage.Mark(number, TransactionState.RolledBack);
                Assert.AreEqual(TransactionState.RolledBack, storage.GetState(number));

                storage.Mark(number, TransactionState.Prepared);
                Assert.AreEqual(TransactionState.Prepared, storage.GetState(number));
            }
        }

        [Test]
        public void ManyTransactions()
        {
            int count = 1000;
            var dictionary = new Dictionary<int, TransactionState>();

            using (var storage = new TransactionalStorage(new FileSystemPageManager(4096)))
            {
                storage.CreateNew(_workPath);

                for (int i = 0; i < count; i++)
                {
                    ISnapshotData snapshotData;
                    dictionary.Add(storage.CreateTransaction(out snapshotData), TransactionState.Active);
                }

                // check that all are active
                foreach (var number in dictionary.Keys)
                    Assert.AreEqual(TransactionState.Active, storage.GetState(number));

                var r = new Random();
                var state = TransactionState.Active;

                var newDictionary = new Dictionary<int, TransactionState>();
                foreach (var number in dictionary.Keys)
                {
                    switch (r.Next(0, 2))
                    {
                        case 0: state = TransactionState.Commited;
                            break;
                        case 1: state = TransactionState.RolledBack;
                            break;
                        case 2: state = TransactionState.Prepared;
                            break;
                    }

                    newDictionary[number] = state;
                    storage.Mark(number, state);
                }

                foreach (var number in newDictionary.Keys)
                    Assert.AreEqual(newDictionary[number], storage.GetState(number));
            }
        }
    }
}
