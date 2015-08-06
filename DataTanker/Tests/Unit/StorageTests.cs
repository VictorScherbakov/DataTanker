using System;
using DataTanker.Settings;

using NUnit.Framework;

using DataTanker;

namespace Tests
{
    [TestFixture]
    public class StorageTests : FileSystemStorageTestBase
    {
        public StorageTests()
        {
            StoragePath = "..\\..\\Storages";
        }

        [Test]
        public void SuccessfulCreateCloseAndOpenStorage()
        {
            var factory = new StorageFactory();

            using (var storage = factory.CreateBPlusTreeStorage<ComparableKeyOf<Int32>, ValueOf<Int32>>(
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        BPlusTreeStorageSettings.Default()))
            {
                storage.CreateNew(StoragePath);
            }

            using (var storage = factory.CreateBPlusTreeStorage<ComparableKeyOf<Int32>, ValueOf<Int32>>(
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        BPlusTreeStorageSettings.Default()))
            {
                storage.OpenExisting(StoragePath);
            }

            using (var storage = factory.CreateBPlusTreeStorage<ComparableKeyOf<Int32>, ValueOf<Int32>>(
            p => BitConverter.GetBytes(p.Value),
            p => BitConverter.ToInt32(p, 0),
            p => BitConverter.GetBytes(p.Value),
            p => BitConverter.ToInt32(p, 0),
            BPlusTreeStorageSettings.Default()))
            {
                storage.OpenOrCreate(StoragePath);
            }
        }

        [Test]
        [ExpectedException(typeof(StorageFormatException))]
        public void OpenStorageWithWrongPageSizeShouldFail()
        {
            var factory = new StorageFactory();

            using (var storage = factory.CreateBPlusTreeStorage<ComparableKeyOf<Int32>, ValueOf<Int32>>(
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        BPlusTreeStorageSettings.Default()))
            {
                storage.CreateNew(StoragePath);
            }

            var differentSettings = BPlusTreeStorageSettings.Default();
            differentSettings.PageSize = PageSize._8192;

            using (var storage = factory.CreateBPlusTreeStorage<ComparableKeyOf<Int32>, ValueOf<Int32>>(
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        differentSettings))
            {
                storage.OpenExisting(StoragePath);
            }
        }
    }
}
