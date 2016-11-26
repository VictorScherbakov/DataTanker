namespace DataTanker.PageManagement
{
    /// <summary>
    /// Page-level interface of a storage.
    /// </summary>
    internal interface IPageManager
    {
        /// <summary>
        /// Gets the storage instance that operates with storage pages via this page manager.
        /// </summary>
        IStorage Storage { get; set; }

        /// <summary>
        /// Checks if the page exists.
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        bool PageExists(long pageIndex);

        /// <summary>
        /// Fetches the page with specified index.
        /// </summary>
        /// <param name="pageIndex">An index of page</param>
        /// <returns>The fetched page</returns>
        IPage FetchPage(long pageIndex);

        /// <summary>
        /// Fetches the page with specified index using provided array as a container.
        /// Callee should have full control over the sharing of content.
        /// This method should used to reduce buffers reallocation. 
        /// </summary>
        /// <param name="pageIndex">An index of page</param>
        /// <param name="content">The content of creating page</param>
        /// <returns>The fetched page</returns>
        IPage FetchPage(long pageIndex, byte[] content);

        /// <summary>
        /// Updates the page.
        /// </summary>
        /// <param name="page">The page instance to update</param>
        void UpdatePage(IPage page);

        /// <summary>
        /// Creates a new page in storage.
        /// </summary>
        /// <returns>Created page</returns>
        IPage CreatePage();

        /// <summary>
        /// Creates a new page space.
        /// </summary>
        void CreateNewPageSpace();

        /// <summary>
        /// Opens an existing storage.
        /// </summary>
        void OpenExistingPageSpace();

        /// <summary>
        /// Closes the storage if it is open.
        /// </summary>
        void Close();

        /// <summary>
        /// Removes a page from storage.
        /// </summary>
        /// <param name="pageIndex">Index of removing page</param>
        void RemovePage(long pageIndex);

        /// <summary>
        /// Checks if this page manager instance can
        /// operate with the specified page.
        /// This is needed to properly creation of pages.
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        bool CheckPage(IPage page);

        /// <summary>
        /// Gets the size of page in bytes.
        /// </summary>
        int PageSize { get; }

        /// <summary>
        /// Locks current instance.
        /// This should prevent an execution of another threads until Unlock() is called.
        /// </summary>
        void Lock();

        /// <summary>
        /// Unlocks current instance.
        /// This should allow an execution of another threads.
        /// </summary>
        void Unlock();

        /// <summary>
        /// Determines if storage can be created
        /// </summary>
        /// <returns>True if storage can be created, false otherwise</returns>
        bool CanCreate();

        /// <summary>
        /// Switches page manager instance to the atomic operation mode.
        /// In such a mode, all further changes can be applied all at once 
        /// by calling ExitAtomicOperation() method or canceled.
        /// </summary>
        void EnterAtomicOperation();

        /// <summary>
        /// Switches page manager instance to normal mode.
        /// All the changes made since the last EnterAtomicOperation() call are applied.
        /// </summary>
        void ExitAtomicOperation();
    }
}