﻿using System;
using System.Collections.Generic;
using System.Linq;
using DataTanker.MemoryManagement;
using DataTanker.BinaryFormat.Page;
using DataTanker.PageManagement;
using NUnit.Framework;

using DataTanker;


namespace Tests
{
    [TestFixture]
    public class MemoryManagerTests : FileSystemStorageTestBase
    {
        public MemoryManagerTests()
        {
            StoragePath = "..\\..\\Storages";
        }

        private bool AreEqualByteArrays(byte[] bytes1, byte[] bytes2)
        {
            return bytes1.Length == bytes2.Length
                   && bytes1.SequenceEqual(bytes2);
        }

        private static readonly Random _r = new Random();

        private byte[] GenerateRandomSequence(SizeRange sizeRange)
        {
            return
                GenerateRandomSequence(sizeRange == SizeRange.MultiPage
                                           ? _r.Next(327681)
                                           : DbItem.GetMaxSize(sizeRange));
        }

        private byte[] GenerateRandomSequence(int length)
        {
            var result = new byte[length];
            _r.NextBytes(result);

            return result;
        }

        private SizeRange GetRandomFixedSizeItemsSizeRange()
        {
            return (SizeRange) _r.Next((int) SizeRange.Range8);
        }

        private SizeRange GetRandomMultipageItemsSizeRange()
        {
            return (SizeRange) _r.Next((int) SizeRange.Range9, (int) SizeRange.MultiPage);
        }

        [Test]
        public void FixedSizeItemsAllocation()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);

                var fsm = new FreeSpaceMap(manager);

                var memoryManager = new MemoryManager(fsm, manager);

                int count = 100000;

                var items = new Dictionary<DbItemReference, byte[]>();

                // allocate items
                for (int i = 0; i < count; i++)
                {
                    var sizeRange = GetRandomFixedSizeItemsSizeRange();
                    var content = GenerateRandomSequence(DbItem.GetMaxSize(sizeRange));
                    var reference = memoryManager.Allocate(content);
                    items[reference] = content;
                }

                // check contents
                foreach (var reference in items.Keys)
                    Assert.IsTrue(AreEqualByteArrays(items[reference], memoryManager.Get(reference).RawData));

                // free half of the items
                var freedItems = new Dictionary<DbItemReference, byte[]>();

                foreach (var reference in items.Keys)
                {
                    if (_r.NextDouble() > 0.5)
                    {
                        memoryManager.Free(reference);
                        freedItems[reference] = items[reference];
                    }
                }

                // check that the items do not exist
                foreach (var reference in freedItems.Keys)
                    Assert.IsNull(memoryManager.Get(reference));

                // and the remaining items still available
                foreach (var reference in items.Keys.Where(item => !freedItems.ContainsKey(item)))
                    Assert.IsTrue(AreEqualByteArrays(items[reference], memoryManager.Get(reference).RawData));
            }
        }

        [Test]
        public void MultipageItemsAllocation()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);

                var fsm = new FreeSpaceMap(manager);

                var memoryManager = new MemoryManager(fsm, manager);

                int count = 10000;

                var items = new Dictionary<DbItemReference, byte[]>();

                // allocate items
                for (int i = 0; i < count; i++)
                {
                    var content = GenerateRandomSequence(GetRandomMultipageItemsSizeRange());
                    var reference = memoryManager.Allocate(content);
                    items[reference] = content;
                }

                // check contents
                foreach (var reference in items.Keys)
                    Assert.IsTrue(AreEqualByteArrays(items[reference], memoryManager.Get(reference).RawData));

                // free half of the items
                var r = new Random();
                var freedItems = new Dictionary<DbItemReference, byte[]>();

                foreach (var reference in items.Keys)
                {
                    if (r.NextDouble() > 0.5)
                    {
                        memoryManager.Free(reference);
                        freedItems[reference] = items[reference];
                    }
                }

                // check that the items do not exist
                foreach (var reference in freedItems.Keys)
                    Assert.IsNull(memoryManager.Get(reference));

                // and the remaining items still available
                foreach (var reference in items.Keys.Where(item => !freedItems.ContainsKey(item)))
                    Assert.IsTrue(AreEqualByteArrays(items[reference], memoryManager.Get(reference).RawData));
            }
        }

        [Test]
        public void MultipageItemsLength()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);

                var fsm = new FreeSpaceMap(manager);

                var memoryManager = new MemoryManager(fsm, manager);

                int count = 1000;

                var items = new Dictionary<DbItemReference, byte[]>();

                // allocate items
                for (int i = 0; i < count; i++)
                {
                    var content = GenerateRandomSequence(GetRandomMultipageItemsSizeRange());
                    var reference = memoryManager.Allocate(content);
                    items[reference] = content;
                }

                // check lengths
                foreach (var reference in items.Keys)
                    Assert.AreEqual(items[reference].Length, memoryManager.GetLength(reference));
            }
        }

        [Test]
        public void FixedSizeItemsLength()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);

                var fsm = new FreeSpaceMap(manager);

                var memoryManager = new MemoryManager(fsm, manager);

                int count = 1000;

                var items = new Dictionary<DbItemReference, byte[]>();

                var r = new Random();

                // allocate items
                for (int i = 0; i < count; i++)
                {
                    var sizeRange = GetRandomFixedSizeItemsSizeRange();
                    var content = GenerateRandomSequence(DbItem.GetMaxSize(sizeRange) - r.Next(5));
                    var reference = memoryManager.Allocate(content);
                    items[reference] = content;
                }

                // check lengths
                foreach (var reference in items.Keys)
                    Assert.AreEqual(items[reference].Length, memoryManager.GetLength(reference));
            }
        }

        [Test]
        public void MultipageItemSegments()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(StoragePath);

                var fsm = new FreeSpaceMap(manager);

                var memoryManager = new MemoryManager(fsm, manager);

                var content = GenerateRandomSequence(4096 * 5 + 100);
                DbItemReference reference = memoryManager.Allocate(content);

                // check full content
                Assert.IsTrue(AreEqualByteArrays(content, memoryManager.GetItemSegment(reference, 0, content.Length - 1)));

                var bytes = new byte[2];

                // check first two bytes
                Array.Copy(content, 0, bytes, 0, 2);
                Assert.IsTrue(AreEqualByteArrays(bytes, memoryManager.GetItemSegment(reference, 0, 1)));

                // check next two bytes
                Array.Copy(content, 2, bytes, 0, 2);
                Assert.IsTrue(AreEqualByteArrays(bytes, memoryManager.GetItemSegment(reference, 2, 3)));

                // check last two bytes
                Array.Copy(content, content.Length - 2, bytes, 0, 2);
                Assert.IsTrue(AreEqualByteArrays(bytes, memoryManager.GetItemSegment(reference, content.Length - 2, content.Length - 1)));

                //last but two bytes
                Array.Copy(content, content.Length - 4, bytes, 0, 2);
                Assert.IsTrue(AreEqualByteArrays(bytes, memoryManager.GetItemSegment(reference, content.Length - 4, content.Length - 3)));

                //all except first two bytes and last two bytes
                bytes = new byte[content.Length - 4];
                Array.Copy(content, 2, bytes, 0, content.Length - 4);
                Assert.IsTrue(AreEqualByteArrays(bytes, memoryManager.GetItemSegment(reference, 2, content.Length - 3)));
            }
        }
    }
}

