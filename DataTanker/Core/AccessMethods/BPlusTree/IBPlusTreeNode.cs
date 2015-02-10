namespace DataTanker.AccessMethods.BPlusTree
{
    using System.Collections.Generic;

    using MemoryManagement;

    /// <summary>
    /// Provides methods and properties of B+Tree nodes.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    internal interface IBPlusTreeNode<TKey>
        where TKey : IComparableKey
    {
        /// <summary>
        /// Gets or sets a value indicating whether this node is leaf.
        /// </summary>
        bool IsLeaf { get; set; }

        /// <summary>
        /// Gets or sets an index of next sibling node.
        /// </summary>
        long NextNodeIndex { get; set; }

        /// <summary>
        /// Gets a value indicating whether this node has a next node.
        /// </summary>
        bool HasNext { get; }

        /// <summary>
        /// Gets a value indicating whether this node has a previous node.
        /// </summary>
        bool HasPrevious { get; }

        /// <summary>
        /// Gets a value indicating whether this node has parent node.
        /// </summary>
        bool HasParent { get; }

        /// <summary>
        /// Gets or sets an index of previous sibling node.
        /// </summary>
        long PreviousNodeIndex { get; set; }

        /// <summary>
        /// Gets or sets an index of parent node. Returns -1 for the root node.
        /// </summary>
        long ParentNodeIndex { get; set; }

        /// <summary>
        /// Gets a collection of entries of this node
        /// </summary>
        IList<KeyValuePair<TKey, DbItemReference>> Entries { get; }

        /// <summary>
        /// Gets an index of this node.
        /// </summary>
        long Index { get; }
    }
}
