namespace DataTanker.AccessMethods.RadixTree
{
    using MemoryManagement;

    /// <summary>
    /// Provides methods that performs storage 
    /// related operations over the B+Tree nodes.
    /// </summary>
    internal interface INodeStorage
    {
        /// <summary>
        /// Gets the maximum length of prefix 
        /// that can be stored in node
        /// </summary>
        int MaxPrefixLength { get; }

        /// <summary>
        /// Fetches a root node of the tree from storage.
        /// </summary>
        /// <returns>The root node of tree</returns>
        IRadixTreeNode FetchRoot();

        /// <summary>
        /// Updates a node in storage.
        /// </summary>
        /// <param name="node">A node to save</param>
        bool Update(IRadixTreeNode node);

        /// <summary>
        /// Fetches a node by its reference.
        /// </summary>
        /// <param name="reference">An index of node</param>
        /// <returns>Fetched node</returns>
        IRadixTreeNode Fetch(DbItemReference reference);

        /// <summary>
        /// Creates a new tree node in storage.
        /// </summary>
        /// <returns>Created node</returns>
        IRadixTreeNode Create(int prefixSize, int childCapacity);

        /// <summary>
        /// Removes a node by its reference.
        /// </summary>
        /// <param name="reference">The index of node</param>
        void Remove(DbItemReference reference);
    }
}