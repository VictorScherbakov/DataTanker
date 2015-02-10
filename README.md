# DataTanker
Embedded persistent key-value store for .NET.
Pure C# code, B+Tree and RadixTree features, MIT License.

## Features
* simple and lightweight
* fast enough
* variable length values
* concurrent access
* atomic operations
* user-defined serialization routines

## Performance
I did not perform detailed comparison with competitors. The benchmarks provided by competing developers are usually biased and misleading.
However, I provide some performance values. To ensure reliability you can run the Performance.exe test util.
* sequential insert and read 1 000 000 integer keys with 20-byte values - 16sec
* insert 100 000 integer keys with large values (from 200 to 5000 bytes) - 12sec
* perform 1 000 000 random operations on 100 000 key set - 8sec

## Usage
```c#
            var factory = new StorageFactory();

            // create storage with integer keys and byte[] values
            using (var storage = factory.CreateBPlusTreeByteArrayStorage<int>(
                BitConverter.GetBytes,            // key serialization
                p => BitConverter.ToInt32(p, 0),  // key deserialization
                BPlusTreeStorageSettings.Default(sizeof(int))))
            {
                storage.OpenOrCreate(Directory.GetCurrentDirectory());

                var r = new Random();
                var bytes = new byte[20];
                const int count = 1000000;

                for (int i = 0; i < count; i++)
                {
                    // fill value with random bytes
                    r.NextBytes(bytes);

                    // insert
                    storage.Set(i, bytes);
                }

                for (int i = 0; i < count; i++)
                {
                    // read
                    bytes = storage.Get(i);
                }
            }
```
