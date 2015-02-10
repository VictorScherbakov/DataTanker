using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using NUnit.Framework;

using DataTanker;
using DataTanker.AccessMethods.BPlusTree;

using Tests.Emulation;

namespace Tests
{
    [TestFixture]
    public class BPlusTreeTests
    {
        private void SingleNodeOperations(int capacity)
        {
            var nodeStorage = new BPlusTreeNodeMemoryStorage<ComparableKeyOf<Int32>>(capacity);
            var tree = new BPlusTree<ComparableKeyOf<Int32>, ValueOf<Int32>>(nodeStorage, new MemoryValueStorage<ValueOf<Int32>>());

            // place sequential numbers
            for (int i = 0; i < capacity; i++)
                tree.Set(i, i);

            // check if we can retrieve correct values
            for (int i = 0; i < capacity; i++)
            {
                var value = tree.Get(i);
                Assert.AreEqual(i, value.Value);
            }

            Assert.AreEqual(capacity, tree.Count());

            // update with the other values
            for (int i = 0; i < capacity; i++)
                tree.Set(i, capacity - 1 - i);

            // check again
            for (int i = 0; i < capacity; i++)
            {
                var value = tree.Get(i);
                Assert.AreEqual(capacity - 1 - i, value.Value);
            }

            // delete all keys
            for (int i = 0; i < capacity; i++)
                tree.Remove(i);

            Assert.AreEqual(0, tree.Count());

            // check if every key returns null
            for (int i = 0; i < capacity; i++)
            {
                var value = tree.Get(i);
                Assert.IsNull(value);
            }
        }

        public void Insert(int capacity)
        {
            var nodeStorage = new BPlusTreeNodeMemoryStorage<ComparableKeyOf<Int32>>(capacity);
            var tree = new BPlusTree<ComparableKeyOf<Int32>, ValueOf<Int32>>(nodeStorage, new MemoryValueStorage<ValueOf<Int32>>());

            int count = capacity * capacity * capacity;

            // place sequential numbers
            for (int i = 0; i < count; i++)
                tree.Set(i, i);

            // check if we can retrieve correct values
            for (int i = 0; i < count; i++)
            {
                var value = tree.Get(i);
                Assert.IsNotNull(value);
                Assert.AreEqual(i, value.Value);
            }

            Assert.AreEqual(count, tree.Count());

            string message;
            Assert.IsTrue(tree.CheckConsistency(out message));
        }

        public void Remove(int capacity)
        {
            var nodeStorage = new BPlusTreeNodeMemoryStorage<ComparableKeyOf<Int32>>(capacity);
            var tree = new BPlusTree<ComparableKeyOf<Int32>, ValueOf<Int32>>(nodeStorage, new MemoryValueStorage<ValueOf<Int32>>());

            int count = capacity * capacity * capacity;

            for (int i = 0; i < count; i++)
                tree.Set(i, i);

            for (int i = 0; i < count; i++)
                tree.Remove(i);

            Assert.AreEqual(0, tree.Count());

            for (int i = 0; i < count; i++)
                Assert.IsNull(tree.Get(i));

            string message;
            Assert.IsTrue(tree.CheckConsistency(out message));
        }

        [Test]
        public void TestMillionRecords()
        {
            int count = 1000000;
            var r = new Random();

            var pairs = new Dictionary<int, int>();
            for (int i = 0; i < count; i++)
                pairs[i] = r.Next(1000000);

            var nodeStorage = new BPlusTreeNodeMemoryStorage<ComparableKeyOf<String>>(250);
            var tree = new BPlusTree<ComparableKeyOf<String>, ValueOf<String>>(nodeStorage, new MemoryValueStorage<ValueOf<String>>());

            foreach (var pair in pairs)
                tree.Set(pair.Key.ToString(CultureInfo.InvariantCulture), pair.Value.ToString(CultureInfo.InvariantCulture));

            var removedPairs = new Dictionary<int, int>();

            foreach (var pair in pairs)
            {
                if (r.NextDouble() > 0.5)
                {
                    removedPairs.Add(pair.Key, pair.Value);
                    tree.Remove(pair.Key.ToString(CultureInfo.InvariantCulture));
                }
            }

            foreach (var pair in pairs)
            {
                string value = tree.Get(pair.Key.ToString(CultureInfo.InvariantCulture));

                if (removedPairs.ContainsKey(pair.Key))
                    Assert.IsNull(value);
                else
                    Assert.AreEqual(pair.Value.ToString(CultureInfo.InvariantCulture), value);
            }
        }

        [Test]
        public void TestSingleNodeOperations()
        {
            for (int i = 5; i < 100; i++)
            {
                SingleNodeOperations(i);
            }
        }

        [Test]
        public void InsertOverCapacityCausesSplit()
        {
            int capacity = 10;
            var nodeStorage = new BPlusTreeNodeMemoryStorage<ComparableKeyOf<Int32>>(capacity);
            var tree = new BPlusTree<ComparableKeyOf<Int32>, ValueOf<Int32>>(nodeStorage, new MemoryValueStorage<ValueOf<Int32>>());

            int count = capacity + 1;

            // place sequential numbers
            for (int i = 0; i < count; i++)
                tree.Set(i, i);

            // check that node was splitted
            Assert.AreEqual(3, nodeStorage.Nodes.Count);

            string message;
            Assert.IsTrue(tree.CheckConsistency(out message));
        }

        [Test]
        public void TestInsert()
        {
            for (int i = 5; i < 50; i++)
            {
                Insert(i);
            }
        }

        [Test]
        public void TestRemove()
        {
            for (int i = 5; i < 50; i++)
            {
                Remove(i);
            }
        }

        [Test]
        public void TestRandomOperations()
        {
            int capacity = 10;
            var nodeStorage = new BPlusTreeNodeMemoryStorage<ComparableKeyOf<Int32>>(capacity);
            var tree = new BPlusTree<ComparableKeyOf<Int32>, ValueOf<Int32>>(nodeStorage, new MemoryValueStorage<ValueOf<Int32>>());

            int keyCount = capacity * capacity * capacity;
            int operationCount = keyCount * keyCount;
            var flags = new bool[keyCount];
            var r = new Random();

            for (int i = 0; i < operationCount; i++)
            {
                var nextIndex = r.Next(keyCount);

                if(flags[nextIndex])
                    tree.Remove(nextIndex);
                else
                    tree.Set(nextIndex, nextIndex);

                flags[nextIndex] = !flags[nextIndex];
            }

            for (int i = 0; i < keyCount; i++)
            {
                    var value = tree.Get(i);
                    if(flags[i])
                        Assert.AreEqual(i, value.Value);
                    else
                        Assert.IsNull(value);
            }

            Assert.AreEqual(flags.Count(f => f), tree.Count());
            string message;
            Assert.IsTrue(tree.CheckConsistency(out message));
        }
    }
}
