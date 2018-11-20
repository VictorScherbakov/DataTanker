namespace DataTanker.PageManagement
{
    using System;

    /// <summary>
    /// Represents a page.
    /// Page is a data block that is written and read entirely. 
    /// </summary>
    internal class Page : IPage
    {
        private readonly long _index;
        private Func<byte[]> _getContent;
        private readonly IPageManager _manager;
        private byte[] _content;
        private object _backingObject;

        #region IPage Members

        /// <summary>
        /// Gets the storage instance that owns this page.
        /// </summary>
        public IStorage Storage => _manager.Storage;

        /// <summary>
        /// Gets the index of this page.
        /// </summary>
        public long Index => _index;

        /// <summary>
        /// Gets the content of this page.
        /// </summary>
        public byte[] Content
        {
            get
            {
                if (_content == null)
                {
                    _content = _getContent(); // perform lazy serialization
                    _getContent = null;
                    _backingObject = null;

                    if (!_manager.CheckPage(this))
                        throw new InvalidOperationException("Page manager does not accept page");
                }

                return _content;
            }
        }

        /// <summary>
        /// Gets a copy of page content.
        /// </summary>
        public byte[] ContentCopy
        {
            get
            {
                //if (_content == null)
                //    _content = _getContent();

                //return (byte[])_content.Clone();

                if (_content == null)
                    return _getContent();

                return (byte[])_content.Clone();
            }
        }

        /// <summary>
        /// Gets the object representing this page.
        /// </summary>
        public object BackingObject
        {
            get { return _backingObject; }
            set { _backingObject = value; }
        }

        /// <summary>
        /// Gets the byte length of this page.
        /// </summary>
        public int Length => _manager.PageSize;

        #endregion

        /// <summary>
        /// Initializes a new instance of page.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="index"></param>
        /// <param name="content"></param>
        internal Page(IPageManager manager, long index, byte[] content)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Page index should not be negative");

            _index = index;
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));

            if (!manager.CheckPage(this))
                throw new InvalidOperationException("Page manager does not accept page");
        }

        /// <summary>
        /// Initializes a new instance of page.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="index"></param>
        /// <param name="getContent">Method that should perform lazy serialization</param>
        internal Page(IPageManager manager, long index, Func<byte[]> getContent) 
            : this(manager, index, getContent, null)

        {
        }

        /// <summary>
        /// Initializes a new instance of page.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="index"></param>
        /// <param name="getContent">Method that should perform lazy serialization</param>
        /// <param name="backingObject"></param>
        internal Page(IPageManager manager, long index, Func<byte[]> getContent, object backingObject)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Page index should not be negative");

            _index = index;
            _getContent = getContent ?? throw new ArgumentNullException(nameof(getContent));
            _backingObject = backingObject;
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public object Clone()
        {
            var cloneContent = new byte[Length];
            Buffer.BlockCopy(_content, 0, cloneContent, 0, _content.Length);
            return new Page(_manager, _index, cloneContent);
        }
    }
}
