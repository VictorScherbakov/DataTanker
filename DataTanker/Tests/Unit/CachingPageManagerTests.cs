using DataTanker;
using DataTanker.PageManagement;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class CachingPageManagerTests
    {
        private string _workPath = "..\\..\\Storages";

        private bool AreEqualByteArrays(byte[] bytes1, byte[] bytes2)
        {
            return bytes1.Length == bytes2.Length 
                && bytes1.SequenceEqual(bytes2);
        }

        private void ArePagesEqual(IPage page, IPage p)
        {
            Assert.AreEqual(page.Index, p.Index);
            AreEqualByteArrays(page.Content, p.Content);
        }

        private void CheckStoragePages(IEnumerable<IPage> pages, IPageManager manager)
        {
            foreach (IPage p in pages)
            {
                IPage page = manager.FetchPage(p.Index);
                ArePagesEqual(page, p);
            }
        }


        [TearDown]
        public void Cleanup()
        {
            string[] files = Directory.GetFiles(_workPath);
            foreach (string file in files)
                File.Delete(file);
        }

        [Test]
        public void SinglePage()
        {
            var fsManager = new FileSystemPageManager(4096);
            var manager = new CachingPageManager(fsManager, 100, 100);

            using (var storage = new Storage(manager))
            {
                storage.CreateNew(_workPath);

                // append a page
                IPage page = manager.CreatePage();

                // fill and update it
                var r = new Random();
                r.NextBytes(page.Content);
                manager.UpdatePage(page);

                // fetch updated content
                IPage fetchedPage = manager.FetchPage(page.Index);
                Assert.IsTrue(AreEqualByteArrays(page.Content, fetchedPage.Content));

                // remove it
                manager.RemovePage(page.Index);
                bool exceptionThrown = false;

                // try to fetch removed page
                try
                {
                    manager.FetchPage(page.Index);
                }
                catch (PageMapException)
                {
                    exceptionThrown = true;
                }

                if (!exceptionThrown)
                    Assert.Fail("Exception was not thrown");

                // resurrect and check content
                page = manager.CreatePage();
                Assert.IsTrue(AreEqualByteArrays(new byte[manager.PageSize], page.Content));
            }
        }

        [Test]
        public void MultiPage()
        {
            var fsManager = new FileSystemPageManager(4096);
            var manager = new CachingPageManager(fsManager, 500, 100);

            using (var storage = new Storage(manager))
            {
                storage.CreateNew(_workPath);

                var r = new Random();

                // fill storage with random data
                int pageCount = 1000;
                var pages = new IPage[pageCount];
                for (int i = 0; i < pageCount; i++)
                {
                    pages[i] = manager.CreatePage();
                    r.NextBytes(pages[i].Content);
                    manager.UpdatePage(pages[i]);
                }

                // fetch and compare pages
                CheckStoragePages(pages, manager);

                // generate a random indexes
                int k = 0;
                var indexesToDelete = new long[pageCount / 2];
                while (k < pageCount / 2)
                {
                    int index = r.Next(pageCount);
                    if (!indexesToDelete.Contains(index))
                    {
                        indexesToDelete[k] = index;
                        k++;
                    }
                }

                // remove pages with these indexes
                foreach (long i in indexesToDelete)
                    manager.RemovePage(i);

                // replace deleted pages with new ones
                for (int i = 0; i < pageCount; i++)
                {
                    if (indexesToDelete.Contains(pages[i].Index))
                    {
                        pages[i] = manager.CreatePage();
                        r.NextBytes(pages[i].Content);
                        manager.UpdatePage(pages[i]);
                    }
                }

                // fetch and compare pages
                CheckStoragePages(pages, manager);
            }
        }

        [Test]
        public void RandomAccess()
        {
            var fsManager = new FileSystemPageManager(4096);
            var manager = new CachingPageManager(fsManager, 1000, 500);

            var pages = new List<IPage>();
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(_workPath);

                var r = new Random();

                // fill storage with random data
                int pageCount = 1000;
                for (int i = 0; i < pageCount; i++)
                {
                    pages.Add(manager.CreatePage());
                    r.NextBytes(pages[i].Content);
                    manager.UpdatePage(pages[i]);
                }

                int operationCount = 1000;
                for (int k = 0; k < operationCount; k++)
                {
                    int op = r.Next(4);
                    switch (op)
                    {
                        case 0: // update
                            int index = r.Next(pages.Count - 1);
                            r.NextBytes(pages[index].Content);
                            manager.UpdatePage(pages[index]);
                            break;
                        case 1: // add
                            IPage page = manager.CreatePage();
                            r.NextBytes(page.Content);
                            manager.UpdatePage(page);
                            pages.Add(page);
                            break;
                        case 2: // remove
                            index = r.Next(pages.Count - 1);
                            manager.RemovePage(pages[index].Index);
                            pages.RemoveAt(index);
                            break;
                        case 3: // fetch
                            index = r.Next(pages.Count - 1);
                            manager.FetchPage(pages[index].Index);
                            break;
                    }
                }
            }

            var m = new FileSystemPageManager(4096);
            using (var storage = new Storage(m))
            {
                storage.OpenExisting(_workPath);

                // fetch and compare pages by filesystem manager
                CheckStoragePages(pages, m);
            }
        }
    }
}
