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

## Feedback

Any feedback is welcome. Feel free to ask a question, send a bug report or feature request.

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
## Where should I use it? 

In cases where the data storage is required, but the file system is not suitable, and large DBMSs have too much overhead. For example: query caches, message queues, large amount of temporary data etc.

## Access methods

Now DataTanker support two access methods: B+Tree and RadixTree.
B+Tree have a good fill factor and demonstrates best performance on small sized keys without hierarchy.
RadixTree is well suited for storing long hierarchical keys like filepaths.

## Keys
Serialization and deserialization methods should be provided for the objects used as keys. Existing key always corresponds to a single value. 
Other features depend on the access method.

**B+Tree storage**

Binary representation of key has size limit, which depends on selected page size. Each index page must have a place for at least three keys in binary format. This requirement follows from the B+Tree properties.
Keys must implement the _IComparable_ interface. 

**RadixTree storage**

Key has no reasonable size limit. Implementation of _IComparable_ and _IEquatable_ interfaces are not required. However, key set is ordered by comparing byte sequences of serialized keys. So, care must be taken to the serialization.

## Values

Values ​​have no restrictions on type. Serialization and deserialization methods should also be provided. The size of value have no reasonable limit. In most cases large values will be limited by the drive size.

## What about ACID?
* _atomicity_ - any single operation is atomic
* _consistency_ - any operation transfer storage to a consistent state 
* _isolation_ - all single operations are isolated from each other
* _durability_ - durability of updates is achieved by calling _storage.Flush()_ method

However, transactions in the sense of unit-of-work are not supported. Thus, we can not produce long series of changes, and then rollback or commit them.

## Create storage

```c#
var factory = new StorageFactory();
var storage = factory.CreateBPlusTreeStorage<int, string>( 
                    BitConverter.GetBytes,               // key serialization
                    p => BitConverter.ToInt32(p, 0),     // key deserialization
                    p => Encoding.UTF8.GetBytes(p),      // value serialization
                    p => Encoding.UTF8.GetString(p),     // value deserialization
                    BPlusTreeStorageSettings.Default(sizeof(int)));
```

Here we provide types for keys and values, serialization methods and storage settings.
Please note that storage instance implements _IDisposible_ interface. We should use storage instance in _using_ block or call _Dispose()_ in appropriate time to successfully shutdown.

To create storage on disk or open an already existing storage use one of

* _storage.OpenExisting(string path)_
* _storage.CreateNew(string path)_
* _storage.OpenOrCreate(string path)_

On-disk storage files:
* _info_ - xml-file containing common information about storage
* _pagemap_ - the mapping of virtual page addresses to on-disk offsets
* _storage_ - the main storage file containing keys, values and index data

## Operations

Storage instance returned by the CreateBPlusTreeStorage() method supports following operations:
* _Get(TKey key)_ - gets the value by its key
* _Set(TKey key, TValue value)_ - inserts or updates key value pair
* _Remove(TKey key)_ - removes key-value pair by key
* _Exists(TKey key)_ - cheks if key-value pair exists
* _GetRawDataLength(TKey key)_ - retrieves the length (in bytes) of binary representation of the value referenced by the specified key
* _GetRawDataSegment(TKey key, long startIndex, long endIndex)_ - retrieves a segment of binary representation of the value referenced by the specified key
* _Min()_ - gets the minimal key
* _Max()_ - gets the maximal key
* _PreviousTo(TKey key)_ - gets the key previous to the specified key, the existence of the specified key is not required
* _NextTo(TKey key)_ - gets the key next to the specified key, the existence of the specified key is not required
* _MinValue()_ - gets a value corresponing to the minimal key
* _MaxValue()_ - gets the value corresponing to the maximal key
* _Count()_ - computes the count of key-value pairs
