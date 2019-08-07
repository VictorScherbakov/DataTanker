using System;
using System.Collections.Generic;
using DataTanker.AccessMethods.RadixTree;
using DataTanker.MemoryManagement;

namespace Tests.Emulation
{
    /// <summary>
    /// Simple in-memory node storage. Mainly for testing purposes.
    /// </summary>
    class RadixTreeNodeMemoryStorage : INodeStorage
    {
        private readonly DbItemReference _rootNodeReference;
        private long _index;

        private readonly Dictionary<string, IRadixTreeNode> _nodes = new Dictionary<string, IRadixTreeNode>();

        private void CheckNode(IRadixTreeNode node)
        {
            if (!_nodes.ContainsKey(node.Reference.ToString()))
                throw new ArgumentException("Radix tree node has an invalid index", nameof(node));
        }

        public IDictionary<string, IRadixTreeNode> Nodes
        {
            get { return _nodes; }
        }

        public int MaxPrefixLength 
        {
            get { return 1000; }
        }

        public IRadixTreeNode FetchRoot()
        {
            return _nodes[_rootNodeReference.ToString()];
        }

        public bool Update(IRadixTreeNode node)
        {
            CheckNode(node);
            _nodes[node.Reference.ToString()] = node;

            return true;
        }

        public IRadixTreeNode Fetch(DbItemReference reference)
        {
            var str = reference.ToString();
            return _nodes.ContainsKey(str) ? _nodes[str] : null;
        }

        public IRadixTreeNode Create(int prefixSize, int childCapacity)
        {
            var node = NewNode(DbItemReference.Null);
            _nodes.Add(node.Reference.ToString(), node);

            return node;
        }

        public void Remove(DbItemReference reference)
        {
            if (_rootNodeReference.Equals(reference))
                throw new ArgumentOutOfRangeException(nameof(reference), "Unable to delete the root node");

            _nodes.Remove(reference.ToString());
        }

        private IRadixTreeNode NewNode(DbItemReference parentReference)
        {
            _index++;
            return new RadixTreeNode(0)
            {
                Reference = new DbItemReference(_index, 0),
                ParentNodeReference = parentReference,
            };
        }

        public RadixTreeNodeMemoryStorage()
        {
            // initialize and add root node
            var root = NewNode(DbItemReference.Null);
            _rootNodeReference = root.Reference;
            _nodes.Add(root.Reference.ToString(), root);
        }
    }
}