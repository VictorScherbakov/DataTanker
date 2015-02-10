using System;
using System.Collections.Generic;
using System.Linq;
using DataTanker.Settings;
using NUnit.Framework;

using DataTanker.BinaryFormat.Page;
using DataTanker.MemoryManagement;
using DataTanker.PageManagement;

namespace Tests
{
    [TestFixture]
    public class PageFormatterTests
    {
        private bool AreEqualByteArrays(byte[] bytes1, byte[] bytes2)
        {
            return bytes1.Length == bytes2.Length && bytes1.SequenceEqual(bytes2);
        }

        [Test]
        public void CorrectWriteAndReadAllHeaders()
        {
            int pageSize = 4096;
            var dummyPageManager = new FileSystemPageManager(pageSize);
            var p = new Page(dummyPageManager, 0, new byte[pageSize]);

            var hph = new HeadingPageHeader();

            PageFormatter.InitPage(p, hph);
            PageHeaderBase phb = PageFormatter.GetPageHeader(p);
            Assert.IsInstanceOf<HeadingPageHeader>(phb);
            Assert.AreEqual(hph.Length, phb.Length);
            Assert.AreEqual(hph.PageType, phb.PageType);
            Assert.AreEqual(hph.SizeClass, phb.SizeClass);

            var fsiph = new FixedSizeItemsPageHeader();

            PageFormatter.InitPage(p, fsiph);
            phb = PageFormatter.GetPageHeader(p);
            Assert.IsInstanceOf<FixedSizeItemsPageHeader>(phb);
            Assert.AreEqual(fsiph.Length, phb.Length);
            Assert.AreEqual(fsiph.PageType, phb.PageType);
            Assert.AreEqual(fsiph.SizeClass, phb.SizeClass);

            var mpph = new MultipageItemPageHeader();

            PageFormatter.InitPage(p, mpph);
            phb = PageFormatter.GetPageHeader(p);
            Assert.IsInstanceOf<MultipageItemPageHeader>(phb);
            Assert.AreEqual(mpph.Length, phb.Length);
            Assert.AreEqual(mpph.PageType, phb.PageType);
            Assert.AreEqual(mpph.SizeClass, phb.SizeClass);
            Assert.AreEqual(mpph.StartPageIndex, ((MultipageItemPageHeader)phb).StartPageIndex);
            Assert.AreEqual(mpph.PreviousPageIndex, ((MultipageItemPageHeader)phb).PreviousPageIndex);
            Assert.AreEqual(mpph.NextPageIndex, ((MultipageItemPageHeader)phb).NextPageIndex);

            var fsmph = new FreeSpaceMapPageHeader();

            PageFormatter.InitPage(p, fsmph);
            phb = PageFormatter.GetPageHeader(p);
            Assert.IsInstanceOf<FreeSpaceMapPageHeader>(phb);
            Assert.AreEqual(fsmph.Length, phb.Length);
            Assert.AreEqual(fsmph.PageType, phb.PageType);
            Assert.AreEqual(fsmph.SizeClass, phb.SizeClass);
            Assert.AreEqual(fsmph.BasePageIndex, ((FreeSpaceMapPageHeader)phb).BasePageIndex);

            var tnph = new BPlusTreeNodePageHeader();

            PageFormatter.InitPage(p, tnph);
            phb = PageFormatter.GetPageHeader(p);
            Assert.IsInstanceOf<BPlusTreeNodePageHeader>(phb);
            Assert.AreEqual(tnph.Length, phb.Length);
            Assert.AreEqual(tnph.PageType, phb.PageType);
            Assert.AreEqual(tnph.SizeClass, phb.SizeClass);
            Assert.AreEqual(tnph.ParentPageIndex, ((BPlusTreeNodePageHeader)phb).ParentPageIndex);
            Assert.AreEqual(tnph.PreviousPageIndex, ((BPlusTreeNodePageHeader)phb).PreviousPageIndex);
            Assert.AreEqual(tnph.NextPageIndex, ((BPlusTreeNodePageHeader)phb).NextPageIndex);

            var rtnph = new RadixTreeNodesPageHeader();

            PageFormatter.InitPage(p, rtnph);
            phb = PageFormatter.GetPageHeader(p);
            Assert.IsInstanceOf<RadixTreeNodesPageHeader>(phb);
            Assert.AreEqual(rtnph.Length, phb.Length);
            Assert.AreEqual(rtnph.PageType, phb.PageType);
            Assert.AreEqual(rtnph.SizeClass, phb.SizeClass);
            Assert.AreEqual(rtnph.FreeSpace, ((RadixTreeNodesPageHeader)phb).FreeSpace);
        }

        [Test]
        public void FixedSizeItemsPage()
        {
            int pageSize = 32768;
            var dummyPageManager = new FileSystemPageManager(pageSize);
            var p = new Page(dummyPageManager, 0, new byte[pageSize]);

            var header = new FixedSizeItemsPageHeader();
            var r = new Random();

            foreach (var sizeClass in EnumHelper.FixedSizeItemsSizeClasses())
            {
                header.SizeClass = sizeClass;

                PageFormatter.InitPage(p, header);

                var item = new DbItem(new byte[DbItem.GetMaxSize(header.SizeClass)]);
                r.NextBytes(item.RawData);

                // fill the page with the items
                short count = 0;
                bool spaceRemains = true;
                while (PageFormatter.HasFreeSpaceForFixedSizeItem(p))
                {
                    Assert.IsTrue(spaceRemains);
                    PageFormatter.AddFixedSizeItem(p, item, out spaceRemains);
                    count++;
                    Assert.AreEqual(count, PageFormatter.ReadFixedSizeItemsCount(p));
                }

                Assert.IsFalse(spaceRemains);

                // check if fetched objects are equal to originals
                for(short j = 0; j < PageFormatter.ReadFixedSizeItemsCount(p); j++)
                {
                    DbItem readItem = PageFormatter.ReadFixedSizeItem(p, j);
                    Assert.IsTrue(AreEqualByteArrays(item.RawData, readItem.RawData));
                }

                // delete all added items
                short itemindex = 0;
                while (PageFormatter.ReadFixedSizeItemsCount(p) > 0)
                {
                    PageFormatter.DeleteFixedSizeItem(p, itemindex);
                    count--;
                    itemindex++;
                    Assert.AreEqual(count, PageFormatter.ReadFixedSizeItemsCount(p));
                }
            }
        }

        [Test]
        public void FsmPage()
        {
            int pageSize = 32768;
            var dummyPageManager = new FileSystemPageManager(pageSize);
            var p = new Page(dummyPageManager, 0, new byte[pageSize]);

            var fsmh = new FreeSpaceMapPageHeader();
            var r = new Random();

            PageFormatter.InitPage(p, fsmh);
            int fsmEntryCount = PageFormatter.GetFsmEntryCount(p);

            // set all values to "full"
            PageFormatter.SetAllFsmValues(p, FsmValue.Full);

            // check if all values are actually "full"
            for (int i = 0; i < fsmEntryCount; i++)
                Assert.AreEqual(FsmValue.Full, PageFormatter.GetFsmValue(p, i));

            var values = new FsmValue[fsmEntryCount];

            // set and keep random values
            for (int i = 0; i < fsmEntryCount; i++)
            {
                var value = (byte)r.Next((byte)FsmValue.Full + 1);
                PageFormatter.SetFsmValue(p, i, (FsmValue)value);
                values[i] = (FsmValue)value;
            }

            // compare it
            for (int i = 0; i < fsmEntryCount; i++)
                Assert.AreEqual(values[i], PageFormatter.GetFsmValue(p, i));
        }

        [Test]
        public void ReadVariableSizeItems()
        {
            int pageSize = 4096;
            var dummyPageManager = new FileSystemPageManager(pageSize);
            var p = new Page(dummyPageManager, 0, new byte[pageSize]);

            var header = new RadixTreeNodesPageHeader
                             {
                                 FreeSpace = (ushort) PageFormatter.GetMaximalFreeSpace((PageSize) p.Length)
                             };

            PageFormatter.InitPage(p, header);

            var r = new Random();

            var items = new List<byte[]>();

            var item = new byte[r.Next(100) + 1];
            r.NextBytes(item);

            while (PageFormatter.AddVariableSizeItem(p, item) != -1)
            {
                items.Add(item);

                item = new byte[r.Next(100) + 1];
                r.NextBytes(item); 
            }

            for (short i = 0; i < items.Count; i++)
            {
                item = PageFormatter.ReadVariableSizeItem(p, i);
                Assert.IsTrue(AreEqualByteArrays(items[i], item));
            }

            bool hasRemainingItems;
            PageFormatter.DeleteVariableSizeItem(p, 0, out hasRemainingItems);
            PageFormatter.DeleteVariableSizeItem(p, 2, out hasRemainingItems);
            PageFormatter.DeleteVariableSizeItem(p, (short)(items.Count - 1), out hasRemainingItems);

            Assert.Throws<PageFormatException>(() => PageFormatter.ReadVariableSizeItem(p, 0));
            Assert.Throws<PageFormatException>(() => PageFormatter.ReadVariableSizeItem(p, 2));
            Assert.Throws<PageFormatException>(() => PageFormatter.ReadVariableSizeItem(p, (short)(items.Count - 1)));
        }

        [Test]
        public void InsertVariableSizeItemAfterDelete()
        {
            int pageSize = 4096;
            var dummyPageManager = new FileSystemPageManager(pageSize);
            var p = new Page(dummyPageManager, 0, new byte[pageSize]);

            var header = new RadixTreeNodesPageHeader
            {
                FreeSpace = (ushort)PageFormatter.GetMaximalFreeSpace((PageSize)p.Length)
            };

            PageFormatter.InitPage(p, header);

            var r = new Random();

            var items = new List<byte[]>();

            var item = new byte[r.Next(100) + 1];
            r.NextBytes(item);

            short itemIndex = 0;

            while (itemIndex != -1)
            {
                itemIndex = PageFormatter.AddVariableSizeItem(p, item);
                items.Add(item);

                item = new byte[r.Next(100) + 1];
                r.NextBytes(item);
            }

            bool hasRemainingItems;

            // replace with the same
            PageFormatter.DeleteVariableSizeItem(p, 1, out hasRemainingItems);
            Assert.AreEqual(1, PageFormatter.AddVariableSizeItem(p, items[1]));

            item = PageFormatter.ReadVariableSizeItem(p, 1);
            Assert.IsTrue(AreEqualByteArrays(items[1], item));

            // replace with smaller one
            PageFormatter.DeleteVariableSizeItem(p, 1, out hasRemainingItems);
            var smallItem = new byte[items[1].Length / 2 + 1];
            r.NextBytes(smallItem);
            Assert.AreEqual(1, PageFormatter.AddVariableSizeItem(p, smallItem));

            item = PageFormatter.ReadVariableSizeItem(p, 1);
            Assert.IsTrue(AreEqualByteArrays(smallItem, item));

            // and put original again
            PageFormatter.DeleteVariableSizeItem(p, 1, out hasRemainingItems);
            PageFormatter.AddVariableSizeItem(p, items[1]);

            item = PageFormatter.ReadVariableSizeItem(p, 1);
            Assert.IsTrue(AreEqualByteArrays(items[1], item));
        }

        [Test]
        public void PageHasNoRemainingItemsAfterDelete()
        {
            int pageSize = 4096;
            var dummyPageManager = new FileSystemPageManager(pageSize);
            var p = new Page(dummyPageManager, 0, new byte[pageSize]);

            var header = new RadixTreeNodesPageHeader
            {
                FreeSpace = (ushort)PageFormatter.GetMaximalFreeSpace((PageSize)p.Length)
            };

            PageFormatter.InitPage(p, header);

            var r = new Random();

            var items = new List<byte[]>();

            var item = new byte[r.Next(100) + 1];
            r.NextBytes(item);

            short itemIndex = 0;

            while (itemIndex != -1)
            {
                itemIndex = PageFormatter.AddVariableSizeItem(p, item);
                if (itemIndex == -1) continue;

                items.Add(item);

                item = new byte[r.Next(100) + 1];
                r.NextBytes(item);
            }

            bool hasRemainingItems;

            for (short i = 0; i < items.Count; i++)
            {
                PageFormatter.DeleteVariableSizeItem(p, i, out hasRemainingItems);
                if(i < items.Count - 1)
                    Assert.IsTrue(hasRemainingItems);
                else
                    Assert.IsFalse(hasRemainingItems);        
            }
        }
    }
}
