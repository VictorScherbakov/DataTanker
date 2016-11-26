using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using DataTanker.Settings;
using System.IO;

using NUnit.Framework;

using DataTanker;

namespace Tests
{
    [TestFixture]
    public class BPlusTreeKeyValueStorageTests : FileSystemStorageTestBase
    {
        public BPlusTreeKeyValueStorageTests()
        {
            StoragePath = "..\\..\\Storages";
        }

        private IBPlusTreeKeyValueStorage<ComparableKeyOf<String>, ValueOf<String>> GetStorage()
        {
            var factory = new StorageFactory();

            var settings = BPlusTreeStorageSettings.Default();
            settings.CacheSettings.MaxCachedPages = 3000;
            settings.CacheSettings.MaxDirtyPages = 2000;
            settings.ForcedWrites = false;
            settings.MaxKeySize = 16;
            settings.PageSize = PageSize._4096;

            return (IBPlusTreeKeyValueStorage<ComparableKeyOf<String>, ValueOf<String>>)factory.CreateBPlusTreeStorage(
                p => Encoding.UTF8.GetBytes(p),
                p => Encoding.UTF8.GetString(p),
                p => Encoding.UTF8.GetBytes(p),
                p => Encoding.UTF8.GetString(p),
                settings);
        }

        [Test]
        public void SingleKeyOperations()
        {
            using (var storage = GetStorage())
            {
                storage.CreateNew(StoragePath);
                storage.Set("1", "1");
                string value = storage.Get("1");

                Assert.AreEqual("1", value);

                storage.Set("1", "2");
                value = storage.Get("1");

                Assert.AreEqual("2", value);

                storage.Remove("1");

                value = storage.Get("1");

                Assert.IsNull(value);
            }
        }

        [Test]
        public void ShouldReadSavedData()
        {
            using (var storage = GetStorage())
            {
                storage.CreateNew(StoragePath);
                storage.Set("1", "1");
            }

            using (var storage = GetStorage())
            {
                storage.OpenExisting(StoragePath);
                string value = storage.Get("1");
                Assert.AreEqual("1", value);
            }
        }

        [Test]
        public void TestInsert()
        {
            int count = 10000;
            var r = new Random();

            var pairs = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
                pairs[i] = r.Next(1000000);

            using (var storage = GetStorage())
            {
                storage.CreateNew(StoragePath);

                foreach (var pair in pairs)
                    storage.Set(pair.Key.ToString(CultureInfo.InvariantCulture), pair.Value.ToString(CultureInfo.InvariantCulture));
            }

            using (var storage = GetStorage())
            {
                storage.OpenExisting(StoragePath);

                foreach (var pair in pairs)
                {
                    string value = storage.Get(pair.Key.ToString(CultureInfo.InvariantCulture));
                    Assert.AreEqual(pair.Value.ToString(CultureInfo.InvariantCulture), value);
                }
            }
        }

        [Test]
        public void TestRemove()
        {
            int count = 100000;
            var r = new Random();

            var pairs = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
                pairs[i] = r.Next(1000000);

            using (var storage = GetStorage())
            {
                storage.CreateNew(StoragePath);

                foreach (var pair in pairs)
                    storage.Set(pair.Key.ToString(CultureInfo.InvariantCulture), pair.Value.ToString(CultureInfo.InvariantCulture));
            }

            var removedPairs = new Dictionary<int, int>();

            using (var storage = GetStorage())
            {
                storage.OpenExisting(StoragePath);

                foreach (var pair in pairs)
                {
                    if (r.NextDouble() > 0.5)
                    {
                        removedPairs.Add(pair.Key, pair.Value);
                        storage.Remove(pair.Key.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            using (var storage = GetStorage())
            {
                storage.OpenExisting(StoragePath);

                foreach (var pair in pairs)
                {
                    string value = storage.Get(pair.Key.ToString(CultureInfo.InvariantCulture));

                    if(removedPairs.ContainsKey(pair.Key))
                        Assert.IsNull(value);
                    else 
                        Assert.AreEqual(pair.Value.ToString(CultureInfo.InvariantCulture), value);
                }
            }
        }

        private IBPlusTreeKeyValueStorage<ComparableKeyOf<String>, ValueOf<String>> _sharedStorage;
        Dictionary<int, int> _sharedPairs = new Dictionary<int, int>();

        [Test]
        [TestCase(100000, 5)]
        [TestCase(100000, 10)]
        [TestCase(100000, 20)]
        [TestCase(1000000, 4)]
        public void ConcurrentAccess(int count, int threadCount)
        {
            var r = new Random();
            var pairs = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
                pairs[i] = r.Next(1000000);

            _sharedPairs = pairs;

            using (var storage = GetStorage())
            {
                _sharedStorage = storage;
                storage.CreateNew(StoragePath);

                // create threads
                var threads = new List<Thread>();
                for (int i = 0; i < threadCount; i++)
                    threads.Add(new Thread(WorkerRoutine));

                int startNumber = 0;
                // and start them
                foreach (Thread thread in threads)
                {
                    thread.Start(new Tuple<int, int>(startNumber, count / threadCount));
                    startNumber += count / threadCount;
                }

                // wait for the end of work
                foreach (Thread thread in threads)
                    thread.Join();

                foreach (var pair in pairs)
                {
                    string value = storage.Get(pair.Key.ToString(CultureInfo.InvariantCulture));
                    Assert.AreEqual(pair.Value.ToString(CultureInfo.InvariantCulture), value);
                }
            }
        }

        private void WorkerRoutine(object startInfo)
        {
            var range = (Tuple<int, int>) startInfo;
            var startNumber = range.Item1;

            var storage = _sharedStorage;

            for (int i = startNumber; i < startNumber + range.Item2; i++)
                storage.Set(i.ToString(CultureInfo.InvariantCulture), _sharedPairs[i].ToString(CultureInfo.InvariantCulture));
        }
    }
}
