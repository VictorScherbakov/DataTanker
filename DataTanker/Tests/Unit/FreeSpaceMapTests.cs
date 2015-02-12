using System;
using System.IO;
using DataTanker.BinaryFormat.Page;
using DataTanker.MemoryManagement;
using DataTanker.PageManagement;

using NUnit.Framework;

using DataTanker;

namespace Tests
{
    [TestFixture]
    public class FreeSpaceMapTests : FileSystemStorageTestBase
    {
        public FreeSpaceMapTests()
        {
            StoragePath = "..\\..\\Storages";
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConstructorShouldThrowExceptionWhenPageManagerIsNull()
        {
            new FreeSpaceMap(null);
        }

        [Test]
        public void ShouldSuccessfulyCreateFsm()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);
                new FreeSpaceMap(manager);
            }
        }

        [Test]
        public void OnEmptyStorageShouldReturnMinusOneForRequestedFreePageIndex()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);
                var fsm = new FreeSpaceMap(manager);

                foreach (var fsmValue in EnumHelper.FixedSizeItemsFsmValues())
                    Assert.AreEqual(-1, fsm.GetFreePageIndex(fsmValue));
            }
        }

        [Test]
        public void OnEmptyStorageAllRequestedPagesShouldBeFull()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);
                var fsm = new FreeSpaceMap(manager);

                int entryCount = PageFormatter.GetFsmEntryCount(manager.FetchPage(1));

                for (int i = 0; i < entryCount; i++)
                    Assert.AreEqual(FsmValue.Full, fsm.Get(i));
            }
        }

        [Test]
        public void SinglePageOperations()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);
                var fsm = new FreeSpaceMap(manager);

                var r = new Random();
                int entryCount = PageFormatter.GetFsmEntryCount(manager.FetchPage(1));

                for (int i = 0; i < 100000; i++)
                {
                    var value = (FsmValue)r.Next((int) FsmValue.Max);
                    var index = r.Next(entryCount);
                    fsm.Set(index, value);    

                    Assert.AreEqual(value, fsm.Get(index));
                }
            }
        }

        [Test]
        public void MultiPageOperations()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);
                var fsm = new FreeSpaceMap(manager);

                int entryPerPage = PageFormatter.GetFsmEntryCount(manager.FetchPage(1));
                int entryCount = entryPerPage * 3;

                // check writing to the last page
                var value = FsmValue.Class0;
                var index = entryCount - 1;
                fsm.Set(index, value);
                Assert.AreEqual(value, fsm.Get(index));

                // middle page
                value = FsmValue.Class1;
                index = entryPerPage + 1;
                fsm.Set(index, value);
                Assert.AreEqual(value, fsm.Get(index));

                // the first page
                value = FsmValue.Class2;
                index = entryPerPage - 1;
                fsm.Set(index, value);
                Assert.AreEqual(value, fsm.Get(index));

                for (int i = 0; i < entryCount; i++)
                    fsm.Set(i, FsmValue.Class3);

                for (int i = 0; i < entryCount; i++)
                {
                    long pageIndex = fsm.GetFreePageIndex(FsmValue.Class3);
                    Assert.AreNotEqual(-1, pageIndex);
                    fsm.Set(i, FsmValue.Full);
                }
            }
        }
    }
}
