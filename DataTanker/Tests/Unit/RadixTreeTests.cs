using System;
using System.Globalization;
using System.Text;

using NUnit.Framework;

using DataTanker;
using DataTanker.AccessMethods.RadixTree;

using Tests.Emulation;

namespace Tests
{
    [TestFixture]
    public class RadixTreeTests
    {
        private RadixTree<ComparableKeyOf<string>, ValueOf<string>> GetTree()
        {
            var nodeStorage = new RadixTreeNodeMemoryStorage();
            return new RadixTree<ComparableKeyOf<string>, ValueOf<string>>(
                nodeStorage,
                new MemoryValueStorage<ValueOf<string>>(),
                new Serializer<ComparableKeyOf<string>>(k => Encoding.ASCII.GetBytes(k), v => Encoding.ASCII.GetString(v)));
        }

        private RadixTree<ComparableKeyOf<string>, ValueOf<string>> GetTree(int prefixLength)
        {
            var nodeStorage = new RadixTreeNodeMemoryStorage();
            return new RadixTree<ComparableKeyOf<string>, ValueOf<string>>(
                nodeStorage,
                new MemoryValueStorage<ValueOf<string>>(),
                new Serializer<ComparableKeyOf<string>>(k => Encoding.ASCII.GetBytes(k), v => Encoding.ASCII.GetString(v)), prefixLength);
        }


        [Test]
        public void GetValueInsertedInEmptyTree()
        {
            var tree = GetTree();

            const string key = "0001";
            tree.Set(key, key);

            Assert.AreEqual(key, tree.Get(key).Value);
        }

        [Test]
        public void GetUpdatedValue()
        {
            var tree = GetTree();

            const string key = "0001";
            tree.Set(key, "1");
            tree.Set(key, "2");

            Assert.AreEqual("2", tree.Get(key).Value);
        }

        [Test]
        public void GetValuesAfterSplittingInsert()
        {
            var tree = GetTree();

            const string key1 = "0001";
            tree.Set(key1, "1");

            const string key2 = "0010";
            tree.Set(key2, "2");

            Assert.AreEqual("1", tree.Get(key1).Value);
            Assert.AreEqual("2", tree.Get(key2).Value);
        }

        [Test]
        public void GetValuesAfterKeepingInsert()
        {
            var tree = GetTree();

            const string key1 = "0001";
            tree.Set(key1, "1");

            const string key2 = "00010"; // one more byte
            tree.Set(key2, "2");

            Assert.AreEqual("1", tree.Get(key1).Value);
            Assert.AreEqual("2", tree.Get(key2).Value);
        }

        [Test]
        public void GetRemovedValueReturnsNull()
        {
            var tree = GetTree();

            const string key = "0001";
            tree.Set(key, "1");
            tree.Remove(key);

            Assert.IsNull(tree.Get(key));
        }

        [Test]
        public void SuccessfulyHandleEmptyKey()
        {
            var tree = GetTree(1);

            string key = string.Empty;

            tree.Set(key, "1");
            Assert.AreEqual("1", tree.Get(key).Value);

            tree.Remove(key);
            Assert.IsNull(tree.Get(key));
        }

        [Test]
        public void GetValuesAfterJoiningRemove()
        {
            var tree = GetTree();

            const string key1 = "1";
            const string key2 = "12";
            const string key3 = "123";

            tree.Set(key1, key1);
            tree.Set(key2, key2);
            tree.Set(key3, key3);

            tree.Remove(key2);

            Assert.AreEqual(key1, tree.Get(key1).Value);
            Assert.IsNull(tree.Get(key2));
            Assert.AreEqual(key3, tree.Get(key3).Value);
        }

        [Test]
        public void PreviousValuesAreCorrectInAllCases()
        {
            // root
            // |
            // 1
            // |
            // 1--
            // |\ \
            // 1 3 444

            var tree = GetTree(2);

            tree.Set("1", "1");
            tree.Set("11", "11");
            tree.Set("111", "111");
            tree.Set("113", "113");
            tree.Set("11444", "11444");

            Assert.AreEqual(null, tree.PreviousTo("1"));
            Assert.AreEqual("11", tree.PreviousTo("111").Value);
            Assert.AreEqual("111", tree.PreviousTo("112").Value);
            Assert.AreEqual("111", tree.PreviousTo("113").Value);
            Assert.AreEqual(null, tree.PreviousTo("000"));
            Assert.AreEqual("113", tree.PreviousTo("114").Value);
            Assert.AreEqual("113", tree.PreviousTo("1145").Value);
            Assert.AreEqual("111", tree.PreviousTo("1111").Value);
            Assert.AreEqual("11444", tree.PreviousTo("2").Value);
        }

        [Test]
        public void NextValuesAreCorrectInAllCases()
        {
            // root
            // |
            // 1
            // |
            // 1--
            // |\ \
            // 1 3 444

            var tree = GetTree(2);

            tree.Set("1", "1");
            tree.Set("11", "11");
            tree.Set("111", "111");
            tree.Set("113", "113");
            tree.Set("11444", "11444");

            Assert.AreEqual("1", tree.NextTo("").Value);
            Assert.AreEqual("111", tree.NextTo("11").Value);
            Assert.AreEqual("113", tree.NextTo("111").Value);
            Assert.AreEqual("113", tree.NextTo("112").Value);
            Assert.AreEqual("11444", tree.NextTo("113").Value);
            Assert.AreEqual("1", tree.NextTo("000").Value);
            Assert.AreEqual("11444", tree.NextTo("114").Value);
            Assert.AreEqual(null, tree.NextTo("1145"));
            Assert.AreEqual("113", tree.NextTo("1111").Value);
            Assert.AreEqual(null, tree.NextTo("2"));
        }

        [Test]
        public void InsertKeysExceedingMaxPrefixLength()
        {
            var tree = GetTree(4);

            const string key1 = "00000";
            tree.Set(key1, key1);

            const string key2 = "00111";
            tree.Set(key2, key2);

            Assert.AreEqual(key1, tree.Get(key1).Value);
            Assert.AreEqual(key2, tree.Get(key2).Value);
        }

        [Test]
        public void MassiveInserts()
        {
            var tree = GetTree();

            const int count = 100000;
            var keys = new string[count];

            var r = new Random();
            for (int i = 0; i < count; i++)
            {
                keys[i] = r.Next(count).ToString(CultureInfo.InvariantCulture);
                tree.Set(keys[i], keys[i]);
            }

            for (int i = 0; i < count; i++)
                Assert.AreEqual(keys[i], tree.Get(keys[i]).Value);
        }
    }
}