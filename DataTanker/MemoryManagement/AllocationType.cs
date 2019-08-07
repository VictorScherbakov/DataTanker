namespace DataTanker.MemoryManagement
{
    /// <summary>
    /// Defines how the object is allocated.
    /// </summary>
    public enum AllocationType
    {
        /// <summary>
        /// Object is allocated on single page.
        /// </summary>
        SinglePage,

        /// <summary>
        /// Object is allocated on multiple pages.
        /// </summary>
        MultiPage
    }
}