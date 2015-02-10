namespace DataTanker.AccessMethods.BPlusTree
{
    /// <summary>
    /// Provides methods that performs storage 
    /// related operations over the B+Tree nodes.
    /// </summary>
    internal interface INodeStorage<TKey> 
        where TKey : IComparableKey
    {
        /// <summary>
        /// Fetches a root node of the tree from storage.
        /// </summary>
        /// <returns>The root node of tree</returns>
        IBPlusTreeNode<TKey> FetchRoot();

        /// <summary>
        /// Updates a node in storage.
        /// </summary>
        /// <param name="node">A node to save</param>
        void Update(IBPlusTreeNode<TKey> node);

        /// <summary>
        /// Fetches a node by index.
        /// </summary>
        /// <param name="nodeIndex">An index of node</param>
        /// <returns>Fetched node</returns>
        IBPlusTreeNode<TKey> Fetch(long nodeIndex);

        /// <summary>
        /// Sets the node with the specified index as new root.
        /// </summary>
        /// <param name="nodeIndex">An index of new root node</param>
        void SetRoot(long nodeIndex);


        /// <summary>
        /// Gets a maximum entry count that can be placed to the node.
        /// </summary>
        int NodeCapacity { get; }

        /// <summary>
        /// Creates a new tree node in storage.
        /// </summary>
        /// <param name="isLeaf">Value indicating whether a created node is leaf node</param>
        /// <returns>Created node</returns>
        IBPlusTreeNode<TKey> Create(bool isLeaf);

        /// <summary>
        /// Removes a node with the specified index from storage.
        /// </summary>
        /// <param name="nodeIndex">The index of node</param>
        void Remove(long nodeIndex);

        /// <summary>
        /// Pins the node
        /// </summary>
        void PinNode(long index);

        /// <summary>
        /// Unpins the node
        /// </summary>
        void UnpinNode(long index);
    }
}