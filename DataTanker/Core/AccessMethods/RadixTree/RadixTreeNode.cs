namespace DataTanker.AccessMethods.RadixTree
{
    using System.Collections.Generic;
    using MemoryManagement;

    [System.Diagnostics.DebuggerDisplay("Reference: {Reference} Entries: {Entries.Count} Prefix: {Prefix}")]
    internal class RadixTreeNode : IRadixTreeNode
    {
        /// <summary>
        /// Gets or sets common prefix bytes stored in this node.
        /// </summary>
        public byte[] Prefix { get; set; }

        /// <summary>
        /// Gets or sets a reference to storing value.
        /// </summary>
        public DbItemReference ValueReference { get; set; }

        /// <summary>
        /// Gets a collection of entries of this node
        /// </summary>
        public IList<KeyValuePair<byte, DbItemReference>> Entries { get; }

        /// <summary>
        /// Gets a reference to this node.
        /// </summary>
        public DbItemReference Reference { get; set; }

        /// <summary>
        /// Gets or sets a reference to parent node. Returns -1 for the root node.
        /// </summary>
        public DbItemReference ParentNodeReference { get; set; }


        public RadixTreeNode(int capacity)
        {
            Entries = new List<KeyValuePair<byte, DbItemReference>>(capacity);
        }
    }
}