using System;
using System.IO;
using DataTanker.Settings;

using NUnit.Framework;

using DataTanker;

namespace Tests
{
    [TestFixture]
    public class StorageTests
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
                storage.CreateNew(_workPath);
            }

            using (var storage = factory.CreateBPlusTreeStorage<ComparableKeyOf<Int32>, ValueOf<Int32>>(
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        p => BitConverter.GetBytes(p.Value),
                        p => BitConverter.ToInt32(p, 0),
                        BPlusTreeStorageSettings.Default()))
            {
                storage.OpenExisting(_workPath);
            }

            using (var storage = factory.CreateBPlusTreeStorage<ComparableKeyOf<Int32>, ValueOf<Int32>>(
            p => BitConverter.GetBytes(p.Value),
            p => BitConverter.ToInt32(p, 0),
            p => BitConverter.GetBytes(p.Value),
            p => BitConverter.ToInt32(p, 0),
            BPlusTreeStorageSettings.Default()))
            {
                storage.OpenOrCreate(_workPath);
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
                storage.CreateNew(_workPath);
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
                storage.OpenExisting(_workPath);
            }
        }
    }
}
