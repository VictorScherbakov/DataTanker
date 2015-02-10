using System;
using System.Collections.Generic;
using System.Linq;
using DataTanker.PageManagement;
using System.IO;
using System.Threading;

using NUnit.Framework;

using DataTanker;

namespace Tests
{
    [TestFixture]
    public class FileSystemPageManagerTests
    {
        private string _workPath = "..\\..\\Storages";
        private List<IPage> _sharedPages = new List<IPage>();
        private FileSystemPageManager _sharedManager;
        private static readonly object _locker = new object();

        [TearDown]
        public void Cleanup()
        {
            string[] files = Directory.GetFiles(_workPath);
            foreach (string file in files)
                File.Delete(file);
        }

        private bool AreEqualByteArrays(byte[] bytes1, byte[] bytes2)
        {
            return bytes1.Length == bytes2.Length 
                && bytes1.SequenceEqual(bytes2);
        }

        private void IsEqualPages(IPage page, IPage p)
        {
            Assert.AreEqual(page.Index, p.Index);
            AreEqualByteArrays(page.Content, p.Content);
        }

        private void CheckStoragePages(IEnumerable<IPage> pages, IPageManager manager)
        {
            CheckStoragePages(pages, manager, new long[] {});
        }

        private void CheckStoragePages(IEnumerable<IPage> pages, IPageManager manager, long[] deletedPagesIndexes)
        {
            foreach (IPage p in pages)
            {
                if (!deletedPagesIndexes.Contains(p.Index))
                {
                    IPage page = manager.FetchPage(p.Index);
                    IsEqualPages(page, p);
                }
            }
        }

        [Test]
        public void SinglePage()
        {
            var manager = new FileSystemPageManager(4096);
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

                if(!exceptionThrown)
                    Assert.Fail("Exception was not thrown");

                // resurrect and check content
                page = manager.CreatePage();
                Assert.IsTrue(AreEqualByteArrays(new byte[manager.PageSize], page.Content));
            }
        }

        [Test]
        public void MultiPage()
        {
            var manager = new FileSystemPageManager(4096);
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

                // fetch and compare pages
                CheckStoragePages(pages, manager, indexesToDelete);

                // add more pages
                for (int i = 0; i < pageCount; i++)
                {
                    if (indexesToDelete.Contains(pages[i].Index))
                    {
                        pages[i] = manager.CreatePage();
                        r.NextBytes(pages[i].Content);
                        manager.UpdatePage(pages[i]);
                    }
                }

                // check again
                CheckStoragePages(pages, manager);
            }
        }

        [Test]
        public void RandomAccess()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(_workPath);

                var r = new Random();

                // fill storage with random data
                int pageCount = 1000;
                var pages = new List<IPage>();
                for (int i = 0; i < pageCount; i++)
                {
                    pages.Add(manager.CreatePage());
                    r.NextBytes(pages[i].Content);
                    manager.UpdatePage(pages[i]);
                }

                int operationCount = 1000;
                for(int k = 0; k < operationCount; k++)
                {
                    int op = r.Next(3);
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
                    }
                }

                // fetch and compare pages
                CheckStoragePages(pages, manager);

            }
        }

        [Test]
        public void ConcurrentAccess()
        {
            var manager = new FileSystemPageManager(4096);
            using (var storage = new Storage(manager))
            {
                storage.CreateNew(_workPath);
                _sharedManager = manager;

                // create threads
                var threads = new List<Thread>();
                for (int i = 0; i < 5; i++)
                    threads.Add(new Thread(AddPagesRoutine));

                // and start them
                foreach (Thread thread in threads)
                    thread.Start();

                // wait for end of work
                foreach (Thread thread in threads)
                    thread.Join();

                // fetch and compare pages
                CheckStoragePages(_sharedPages, manager);

                threads.Clear();

                // run the same test for random operations
                for (int i = 0; i < 5; i++)
                    threads.Add(new Thread(RandomOperationsRoutine));

                foreach (Thread thread in threads)
                    thread.Start();

                foreach (Thread thread in threads)
                    thread.Join();

                CheckStoragePages(_sharedPages, manager);
            }
        }
        
        private void AddPagesRoutine()
        {
            FileSystemPageManager manager = _sharedManager;

            var r = new Random();

            // fill storage with random data
            int pageCount = 1000;
            for (int i = 0; i < pageCount; i++)
            {
                var page = manager.CreatePage();
                _sharedPages.Add(page);
                r.NextBytes(page.Content);
                manager.UpdatePage(page);
            }
        }

        private void RandomOperationsRoutine()
        {
            var manager = _sharedManager;

            var r = new Random();

            int operationCount = 1000;
            for (int k = 0; k < operationCount; k++)
            {
                int op = r.Next(3);
                lock (_locker)
                {
                    switch (op)
                    {
                        case 0: // update
                            int index = r.Next(_sharedPages.Count - 1);
                            r.NextBytes(_sharedPages[index].Content);
                            manager.UpdatePage(_sharedPages[index]);
                            break;
                        case 1: // add
                            IPage page = manager.CreatePage();
                            r.NextBytes(page.Content);
                            manager.UpdatePage(page);
                            _sharedPages.Add(page);
                            break;
                        case 2: // remove
                            index = r.Next(_sharedPages.Count - 1);
                            manager.RemovePage(_sharedPages[index].Index);
                            _sharedPages.RemoveAt(index);
                            break;
                    }
                }
            }
        }
    }
}
