using System;
using System.Collections.Generic;
using DataTanker;
using DataTanker.AccessMethods.BPlusTree;

namespace Tests.Emulation
{
    /// <summary>
    /// Simple in-memory node storage. Mainly for testing purposes.
    /// </summary>
    /// <typeparam name="TKey">The type of key</typeparam>
    class BPlusTreeNodeMemoryStorage<TKey> : INodeStorage<TKey>
        where TKey : IComparableKey
    {
        private readonly int _nodeCapacity;
        private long _rootNodeIndex;
        private long _nodeIndex;

        private readonly Dictionary<long, IBPlusTreeNode<TKey>> _nodes = new Dictionary<long, IBPlusTreeNode<TKey>>();

        private void CheckNode(IBPlusTreeNode<TKey> node)
        {
            if(!_nodes.ContainsKey(node.Index))
                throw new ArgumentException("B+Tree node has an invalid index", "node");

            if(node.Entries.Count > _nodeCapacity)
                throw new ArgumentException("B+Tree node has too many entries", "node");
        }

        public IDictionary<long, IBPlusTreeNode<TKey>> Nodes
        {
            get { return _nodes; }
        }

        public IBPlusTreeNode<TKey> FetchRoot()
        {
            return _nodes[_rootNodeIndex];
        }

        public void Update(IBPlusTreeNode<TKey> node)
        {
            CheckNode(node);

            _nodes[node.Index] = node;
        }

        public IBPlusTreeNode<TKey> Fetch(long nodeIndex)
        {
            return _nodes.ContainsKey(nodeIndex) ? _nodes[nodeIndex] : null;
        }

        public void SetRoot(long nodeIndex)
        {
            if (!_nodes.ContainsKey(nodeIndex))
                throw new ArgumentException("nodeIndex");

            _rootNodeIndex = nodeIndex;
        }

        public int NodeCapacity
        {
            get { return _nodeCapacity; }
        }

        public IBPlusTreeNode<TKey> Create(bool isLeaf)
        {
            var node = NewNode(-1, isLeaf);
            _nodes.Add(node.Index, node);

            return node;
        }

        public void Remove(long nodeIndex)
        {
            if(nodeIndex == _rootNodeIndex)
                throw new ArgumentOutOfRangeException("nodeIndex", "Unable to delete the root node");

            _nodes.Remove(nodeIndex);
        }

        public void PinNode(long index)
        {
        }

        public void UnpinNode(long index)
        {
        }

        private BPlusTreeNode<TKey> NewNode(long parentIndex, bool isLeaf)
        {
            return new BPlusTreeNode<TKey>(_nodeIndex++)
                       {
                           ParentNodeIndex = parentIndex,
                           IsLeaf = isLeaf
                       };
        }

        public BPlusTreeNodeMemoryStorage(int nodeCapacity)
        {
            if (nodeCapacity < 3)
                throw new ArgumentOutOfRangeException("nodeCapacity", "Node capacity should be greater than 3");

            _nodeCapacity = nodeCapacity;

            // initialize and add root node
            var root = NewNode(-1, true);
            _nodes.Add(root.Index, root);
        }
    }
}
