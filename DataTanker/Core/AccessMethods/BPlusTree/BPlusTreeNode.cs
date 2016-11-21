namespace DataTanker.AccessMethods.BPlusTree
{
    using System.Collections.Generic;

    using MemoryManagement;

    [System.Diagnostics.DebuggerDisplay("IsLeaf: {IsLeaf} Index: {Index} Entries: {Entries.Count}")]
    internal class BPlusTreeNode<TKey> : IBPlusTreeNode<TKey> 
        where TKey : IComparableKey
    {
        /// <summary>
        /// Gets or sets a value indicating whether this node is leaf.
        /// </summary>
        public bool IsLeaf { get; set; }

        /// <summary>
        /// For leaf nodes gets or sets an index of next sibling node.
        /// </summary>
        public long NextNodeIndex { get; set; }

        /// <summary>
        /// Gets a value indicating whether this node has a next node.
        /// </summary>
        public bool HasNext => NextNodeIndex != -1;

        /// <summary>
        /// Gets a value indicating whether this node has a previous node.
        /// </summary>
        public bool HasPrevious => PreviousNodeIndex != -1;

        /// <summary>
        /// Gets a value indicating whether this node has parent node.
        /// </summary>
        public bool HasParent => ParentNodeIndex != -1;

        /// <summary>
        /// For leaf nodes gets or sets an index of previous sibling node.
        /// </summary>
        public long PreviousNodeIndex { get; set; }

        /// <summary>
        /// Gets or sets an index of parent node. Returns -1 for the root node.
        /// </summary>
        public long ParentNodeIndex { get; set; }

        /// <summary>
        /// Gets a collection of entries of this node
        /// </summary>
        public IList<KeyValuePair<TKey, DbItemReference>> Entries { get; }

        /// <summary>
        /// Gets an index of this node.
        /// </summary>
        public long Index { get; }

        /// <summary>
        /// Initializes a new instance of the BPlusTreeNode.
        /// </summary>
        /// <param name="index">An index of this node</param>
        public BPlusTreeNode(long index) 
            : this (index, 10) // reasonable minimal value
        {
        }

        /// <summary>
        /// Initializes a new instance of the BPlusTreeNode.
        /// </summary>
        /// <param name="index">An index of this node</param>
        /// <param name="capacity">Capacity of node</param>
        public BPlusTreeNode(long index, int capacity)
        {
            Entries = new List<KeyValuePair<TKey, DbItemReference>>(capacity);
            Index = index;
            NextNodeIndex = -1;
            PreviousNodeIndex = -1;
            ParentNodeIndex = -1;
        }
    }
}