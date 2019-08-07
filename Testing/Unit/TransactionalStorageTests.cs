using System;
using System.Collections.Generic;
using NUnit.Framework;

using DataTanker.PageManagement;
using DataTanker.Transactions;
using DataTanker;

namespace Tests
{
    [TestFixture]
    public class TransactionalStorageTests : FileSystemStorageTestBase
    {
        public TransactionalStorageTests()
        {
            StoragePath = "..\\..\\Storages";
        }

        [Test]
        public void CreatedTransactionShouldBeActive()
        {
            using (var storage = new TransactionalStorage(new FileSystemPageManager(4096)))
            {
                storage.CreateNew(StoragePath);
                var number = storage.CreateTransaction(out _);
                Assert.AreEqual(TransactionState.Active, storage.GetState(number));
            }
        }

        [Test]
        public void SuccessfulySetState()
        {
            using (var storage = new TransactionalStorage(new FileSystemPageManager(4096)))
            {
                storage.CreateNew(StoragePath);

                var number = storage.CreateTransaction(out _);

                storage.Mark(number, TransactionState.Committed);
                Assert.AreEqual(TransactionState.Committed, storage.GetState(number));

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
                storage.CreateNew(StoragePath);

                for (int i = 0; i < count; i++)
                {
                    dictionary.Add(storage.CreateTransaction(out _), TransactionState.Active);
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
                        case 0: state = TransactionState.Committed;
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
