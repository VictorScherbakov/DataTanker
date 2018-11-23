namespace DataTanker.MemoryManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using BinaryFormat.Page;
    using PageManagement;

    internal class FreeSpaceMap
    {
        private readonly IPageManager _pageManager;
        private long _firstFsmPageIndex;
        private int _entryPerPage;
        private long _fsmPageCount = -1;
        private IPage _lastFsmPage;

        private bool _isInitialized;

        // fsm-page indexes never change 
        // this list prevents sequence iteration 
        // for accessing requested page
        private readonly List<long> _fsmPageIndexes = new List<long>();

        private class LuckyPage
        {
            public IPage Page { get; set; }
            public long LastGoodIndex { get; set; }
        }

        private readonly Dictionary<FsmValue, LuckyPage> _luckyFsmPages = new Dictionary<FsmValue, LuckyPage>();

        private readonly List<FsmValue> _scanned = new List<FsmValue>();

        private IPage GetFsmPageByTargetPageIndex(long pageIndex)
        {
            var fsmPageNumber = (int)pageIndex / _entryPerPage;

            // try to find appropriate fsm-page in the index list
            if (_fsmPageIndexes.Count >= fsmPageNumber + 1)
                return _pageManager.FetchPage(_fsmPageIndexes[fsmPageNumber]);

            // iterate fsm sequence from the last found fsm-page
            _fsmPageCount = _fsmPageIndexes.Count;

            IPage fsmPage = _pageManager.FetchPage(_fsmPageIndexes.Last());

            while (true)
            {
                if(!(PageFormatter.GetPageHeader(fsmPage) is FreeSpaceMapPageHeader header))
                    throw new StorageFormatException("Free space map page not found");

                _lastFsmPage = fsmPage;
                _fsmPageCount++;

                if (header.NextPageIndex == -1)
                    return null;

                fsmPage = _pageManager.FetchPage(header.NextPageIndex);

                if(_fsmPageCount < fsmPageNumber)
                    return fsmPage;

                _fsmPageIndexes.Add(fsmPage.Index);
            }
        }

        private void InitFsmPage(IPage page, long previousPageIndex, long nextPageIndex, long basePageIndex)
        {
            var header = new FreeSpaceMapPageHeader
            {
                StartPageIndex = _firstFsmPageIndex,
                NextPageIndex = nextPageIndex,
                PreviousPageIndex = previousPageIndex,
                BasePageIndex = basePageIndex
            };

            PageFormatter.InitPage(page, header);
            PageFormatter.SetAllFsmValues(page, FsmValue.Full);
        }

        public void Set(long pageIndex, FsmValue value)
        {
            if(!_isInitialized) Init();

            IPage page = GetFsmPageByTargetPageIndex(pageIndex);

            if (page == null)
            {
                // fsm-page is missing for requested page index
                long previousPageIndex = _lastFsmPage.Index;
                var missingPageCount = (int)((pageIndex - (_fsmPageCount - _fsmPageIndexes.Count) * _entryPerPage) / _entryPerPage + 1);

                var pages = new List<IPage>(missingPageCount);

                // allocate new pages
                for (int i = 0; i < missingPageCount; i++)
                    pages.Add(_pageManager.CreatePage());

                var baseIndex = PageFormatter.GetBasePageIndex(_lastFsmPage);

                // initialize them
                for (int i = 0; i < missingPageCount; i++)
                {
                    baseIndex += _entryPerPage;
                    InitFsmPage(pages[i], 
                        previousPageIndex, 
                        i == missingPageCount - 1 ? -1 : pages[i + 1].Index,
                        baseIndex);
                    previousPageIndex = pages[i].Index;
                }

                // and update
                pages.ForEach(_pageManager.UpdatePage);

                // save reference to added pages
                var lastPageHeader = (FreeSpaceMapPageHeader)PageFormatter.GetPageHeader(_lastFsmPage);
                lastPageHeader.NextPageIndex = pages[0].Index;
                lastPageHeader.WriteToPage(_lastFsmPage);
                _pageManager.UpdatePage(_lastFsmPage);

                _lastFsmPage = null;
                _fsmPageCount = -1;

                Set(pageIndex, value);
            }
            else
            {
                PageFormatter.SetFsmValue(page, (int) pageIndex % _entryPerPage, value);
                _pageManager.UpdatePage(page);

                var fsmValuesToUpdate = _luckyFsmPages.Where(item => item.Value.Page.Index == page.Index).Select(item => item.Key).ToList();

                foreach (var fsmValue in fsmValuesToUpdate)
                    _luckyFsmPages[fsmValue].Page = page;

                if (!_luckyFsmPages.ContainsKey(value))
                    _luckyFsmPages[value] = new LuckyPage { Page = page, LastGoodIndex = pageIndex };
                else
                {
                    if (_scanned.Contains(value)) _scanned.Remove(value);
                }
            }
        }

        public FsmValue Get(long pageIndex)
        {
            if (!_isInitialized) Init();

            IPage page = GetFsmPageByTargetPageIndex(pageIndex);

            return page == null 
                ? FsmValue.Full : 
                PageFormatter.GetFsmValue(page, (int)pageIndex % _entryPerPage);
        }

        public long GetFreePageIndex(FsmValue value)
        {
            if (!_isInitialized) Init();

            // try to find in lucky pages
            if (_luckyFsmPages.ContainsKey(value))
            {
                IPage page = _luckyFsmPages[value].Page;

                var requestedFsmIndex = (int) (_luckyFsmPages[value].LastGoodIndex % _entryPerPage);
                int matchingFsmIndex;

                if (PageFormatter.GetFsmValue(page, requestedFsmIndex) == value)
                        matchingFsmIndex = requestedFsmIndex;
                else
                {
                    matchingFsmIndex = PageFormatter.GetIndexOfFirstMatchingFsmValue(page, value);
                    _luckyFsmPages[value].LastGoodIndex = matchingFsmIndex;
                }

                if (matchingFsmIndex == -1)
                {
                    // page becomes unlucky
                    _luckyFsmPages.Remove(value);
                }
                else
                    return PageFormatter.GetBasePageIndex(page) + matchingFsmIndex;
            }
            
            var currentPageIndex = _firstFsmPageIndex;
            long index = 0;

            if (_scanned.Contains(value)) return -1;

            while (true)
            {
                // sequential scan all fsm pages for specified fsm-value
                var currentPage = _pageManager.FetchPage(currentPageIndex);
                int matchingFsmIndex = PageFormatter.GetIndexOfFirstMatchingFsmValue(currentPage, value);
                if (matchingFsmIndex == -1)
                {
                    index += _entryPerPage;
                    var header = (FreeSpaceMapPageHeader)PageFormatter.GetPageHeader(currentPage);
                    if (header.NextPageIndex == -1)
                    {
                        _scanned.Add(value);
                        return -1;
                    }

                    currentPageIndex = header.NextPageIndex;
                }
                else
                {
                    // make found page lucky
                    _luckyFsmPages[value] = new LuckyPage { Page = currentPage, LastGoodIndex = matchingFsmIndex + index };
                    return index + matchingFsmIndex;
                }
            }
        }

        private void Init()
        {
            IPage headingPage = _pageManager.FetchPage(0);
            if (!(PageFormatter.GetPageHeader(headingPage) is HeadingPageHeader header))
                throw new StorageFormatException("Heading page not found");

            _firstFsmPageIndex = header.FsmPageIndex;
            _fsmPageIndexes.Add(_firstFsmPageIndex);

            IPage firstFsmPage = _pageManager.FetchPage(_firstFsmPageIndex);
            if (!(PageFormatter.GetPageHeader(firstFsmPage) is FreeSpaceMapPageHeader))
                throw new StorageFormatException("Free space map page not found");

            _entryPerPage = PageFormatter.GetFsmEntryCount(firstFsmPage);

            _isInitialized = true;
        }

        public FreeSpaceMap(IPageManager pageManager)
        {
            _pageManager = pageManager ?? throw new ArgumentNullException(nameof(pageManager));
        }
    }
}
