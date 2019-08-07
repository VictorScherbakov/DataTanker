using System;
using System.Globalization;
using DataTanker;
using DataTanker.Settings;

namespace Performance
{
    class BPlusTreeStorageTests
    {
        public static string StoragePath { get; set; }

        private static IKeyValueStorage<ComparableKeyOf<int>, ValueOf<byte[]>> GetByteArrayStorage()
        {
            var factory = new StorageFactory();
            return factory.CreateBPlusTreeByteArrayStorage<int>(BPlusTreeStorageSettings.Default(sizeof(int)));
        }

        private static IKeyValueStorage<ComparableKeyOf<int>, ValueOf<int>> GetIntStorage()
        {
            var factory = new StorageFactory();

            return factory.CreateBPlusTreeStorage<int, int>(
                BitConverter.GetBytes,
                p => BitConverter.ToInt32(p, 0),
                BPlusTreeStorageSettings.Default(sizeof(int)));
        }

        public static void InsertAndReadMillionRecords(Action<string> writeInfo)
        {
            const int count = 1000000;
            var r = new Random();

            using (var storage = GetByteArrayStorage())
            {
                storage.CreateNew(StoragePath);
                var bytes = new byte[20];
                for (int i = 0; i < count; i++)
                {
                    r.NextBytes(bytes);
                    storage.Set(i, bytes);

                    if (i % 1000 == 0)
                    {
                        writeInfo(i.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }

            using (var storage = GetByteArrayStorage())
            {
                storage.OpenExisting(StoragePath);
                for (int i = 0; i < count; i++)
                {
                    storage.Get(i);

                    if (i % 1000 == 0)
                    {
                        writeInfo("check " + i.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }
        }

        public static void InsertLargeValues(Action<string> writeInfo)
        {
            const int count = 100000;
            var r = new Random();

            using (var storage = GetByteArrayStorage())
            {
                storage.CreateNew(StoragePath);

                for (int i = 0; i < count; i++)
                {
                    var bytes = new byte[200 + r.Next(5000)];
                    r.NextBytes(bytes);
                    storage.Set(i, bytes);

                    if (i % 1000 == 0)
                    {
                        writeInfo(i.ToString(CultureInfo.InvariantCulture));
                    }
                }
            }
        }

        public static void RandomOperations(Action<string> writeInfo)
        {
            const int keyCount = 100000;
            const int operationCount = 1000000;
            var flags = new bool[keyCount];
            var r = new Random();

            using (var storage = GetIntStorage())
            {
                storage.CreateNew(StoragePath);

                for (int i = 0; i < operationCount; i++)
                {
                    var key = r.Next(keyCount);

                    if (flags[key])
                        storage.Remove(key);
                    else
                        storage.Set(key, key);

                    flags[key] = !flags[key];

                    if (i % 1000 == 0)
                        writeInfo($"operation {i}");
                }

                for (int i = 0; i < keyCount; i++)
                {
                    if (i % 100 == 0)
                        writeInfo($"checking key {i}");

                    var value = storage.Get(i);
                    if (flags[i])
                        if (i != value.Value)
                            throw new ApplicationException($"RandomOperations failed: expected {i} but found {value.Value}");

                }
            }
        }
    }
}