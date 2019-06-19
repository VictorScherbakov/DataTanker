using System.IO;
using System.Reflection;
using DataTanker;
using DataTanker.PageManagement;
using DataTanker.Recovery;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class RecoveryFileTests : FileSystemStorageTestBase
    {
        public RecoveryFileTests()
        {
            StoragePath = "..\\..\\Storages";
        }

        [Test]
        public void CorrectlyRecoverResurrectedPage()
        {
            long index;
            long resurrectIndex;

            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            {
                storage.OpenOrCreate(StoragePath);
                var page = fileSystemPageManager.CreatePage();
                index = page.Index;
            }

            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            {
                storage.OpenOrCreate(StoragePath);
                var page = fileSystemPageManager.FetchPage(index);
                Assert.AreEqual(index, page.Index);
                Assert.AreNotEqual(0xff, page.Content[10]);
            }

            string fileName;
            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            using (var recoveryFile = new RecoveryFile(fileSystemPageManager, fileSystemPageManager.PageSize))
            {
                storage.OpenOrCreate(StoragePath);
                var page = fileSystemPageManager.FetchPage(index);
                index = page.Index;

                page.Content[10] = 0xaf;

                fileSystemPageManager.RemovePage(index);
                var resurrectedPage = fileSystemPageManager.CreatePage();
                resurrectIndex = resurrectedPage.Index;

                //check we're not going wrong way
                Assert.AreEqual(index, resurrectIndex);

                resurrectedPage.Content[10] = 0xff;
                Assert.AreNotEqual(0xff, page.Content[10]);

                fileSystemPageManager.Dispose();

                recoveryFile.WriteUpdatePageRecord(page);
                recoveryFile.WriteDeletePageRecord(index);
                recoveryFile.WriteUpdatePageRecord(resurrectedPage);

                recoveryFile.WriteFinalMarker();

                fileName = recoveryFile.FileName;
            }

            Assert.True(File.Exists(fileName));
            Assert.GreaterOrEqual(new FileInfo(fileName).Length, 1);

            using (var manager = new FileSystemPageManager(4096))
            using (var storage = new Storage(manager))
            {
                storage.OpenOrCreate(StoragePath);
                var page = manager.FetchPage(index);
                Assert.AreEqual(resurrectIndex, page.Index);
                Assert.AreEqual(0xff, page.Content[10]);
            }
        }

        [Test]
        public void CorrectlyApplyDeleteRecord()
        {
            long deleteIndex;

            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            {
                storage.OpenOrCreate(StoragePath);
                deleteIndex = fileSystemPageManager.CreatePage().Index;
            }

            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            {
                storage.OpenOrCreate(StoragePath);
                Assert.True(fileSystemPageManager.PageExists(deleteIndex));
            }

            string fileName;
            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            using (var recoveryFile = new RecoveryFile(fileSystemPageManager, fileSystemPageManager.PageSize))
            {
                storage.OpenOrCreate(StoragePath);
                fileSystemPageManager.Dispose();

                recoveryFile.WriteDeletePageRecord(deleteIndex);

                recoveryFile.WriteFinalMarker();
                fileName = recoveryFile.FileName;
            }

            Assert.True(File.Exists(fileName));
            Assert.GreaterOrEqual(new FileInfo(fileName).Length, 1);

            using (var manager = new FileSystemPageManager(4096))
            using (var storage = new Storage(manager))
            {
                storage.OpenOrCreate(StoragePath);
                Assert.False(manager.PageExists(deleteIndex));
                Assert.AreEqual(deleteIndex, manager.CreatePage().Index);
            }
        }

        [Test]
        public void CorrectlyRecoverSequentiallyUpdatedPage()
        {
            long index;

            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            {
                storage.OpenOrCreate(StoragePath);
                var page = fileSystemPageManager.CreatePage();
                index = page.Index;
            }

            string fileName;
            using (var fileSystemPageManager = new FileSystemPageManager(4096))
            using (var storage = new Storage(fileSystemPageManager))
            using (var recoveryFile = new RecoveryFile(fileSystemPageManager, fileSystemPageManager.PageSize))
            {
                storage.OpenOrCreate(StoragePath);
                var page = fileSystemPageManager.FetchPage(index);
                index = page.Index;
                fileSystemPageManager.Dispose();

                Assert.AreEqual(index, page.Index);
                Assert.AreNotEqual(0xff, page.Content[10]);

                // Update twice with different bytes to make sure the latest one wins
                page.Content[9] = 0xaa;
                page.Content[10] = 0xaa;
                recoveryFile.WriteUpdatePageRecord(page);
                page.Content[10] = 0xff;
                recoveryFile.WriteUpdatePageRecord(page);

                recoveryFile.WriteFinalMarker();
                fileName = recoveryFile.FileName;
            }

            Assert.True(File.Exists(fileName));
            Assert.GreaterOrEqual(new FileInfo(fileName).Length, 1);
            Assert.AreNotEqual(index, 0);

            using (var manager = new FileSystemPageManager(4096))
            using (var storage = new Storage(manager))
            {
                storage.OpenOrCreate(StoragePath);
                var page = manager.FetchPage(index);
                Assert.AreEqual(index, page.Index);
                Assert.AreEqual(0xaa, page.Content[9]);
                Assert.AreEqual(0xff, page.Content[10]);
            }
        }
    }
}