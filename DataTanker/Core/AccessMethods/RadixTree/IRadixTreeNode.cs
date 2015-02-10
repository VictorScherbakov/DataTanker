namespace DataTanker.AccessMethods.RadixTree
{
    using System.Collections.Generic;

    using MemoryManagement;

    internal interface IRadixTreeNode
    {
        /// <summary>
        /// Gets or sets common prefix bytes stored in this node.
        /// </summary>
        byte[] Prefix { get; set; }

        /// <summary>
        /// Gets a collection of entries of this node
        /// </summary>
        IList<KeyValuePair<byte, DbItemReference>> Entries { get; }

        /// <summary>
        /// Gets a reference to this node.
        /// </summary>
        DbItemReference Reference { get; set; }

        /// <summary>
        /// Gets or sets an index of parent node. Returns -1 for the root node.
        /// </summary>
        DbItemReference ParentNodeReference { get; set; }

        /// <summary>
        /// Gets or sets a reference to storing value.
        /// </summary>
        DbItemReference ValueReference { get; set; }
    }
}