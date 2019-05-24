namespace DataTanker.AccessMethods.BPlusTree
{
    using System.Diagnostics;

    using System.Linq;
    using System.Collections.Generic;

    using MemoryManagement;

    /// <summary>
    /// Implementation of B+Tree.
    /// </summary>
    /// <typeparam name="TKey">The type of keys</typeparam>
    /// <typeparam name="TValue">The type of values</typeparam>
    internal class BPlusTree<TKey, TValue> : IBPlusTree<TKey, TValue> 
        where TKey : IComparableKey 
        where TValue : IValue
    {
        private readonly INodeStorage<TKey> _nodeStorage;
        private readonly IValueStorage<TValue> _valueStorage;

        private double _fillFactor = 0.5;

        private static KeyValuePair<TKey, DbItemReference>? FindSuitableEntry(IList<KeyValuePair<TKey, DbItemReference>> entries, TKey key, out int index)
        {
            int first = 0;
            int last = entries.Count;

            if (last == 0)
            {
                index = 0;
                return null;
            }

            if (entries[0].Key.CompareTo(key) > 0)
            {
                index = 0;
                return null;
            }

            if (entries[last - 1].Key.CompareTo(key) < 0)
            {
                index = last;
                return null;
            }

            while (first < last)
            {
                int mid = first + (last - first) / 2;

                if (key.CompareTo(entries[mid].Key) <= 0)
                    last = mid;
                else
                    first = mid + 1;
            }

            index = last;

            if (entries[last].Key.CompareTo(key) == 0)
                return entries[last];

            return null;
        }

        private KeyValuePair<TKey, DbItemReference>? FindSuitableEntry(TKey key)
        {
            var node = GetSuitableLeafNodeForKeyIfRangeExists(key);
            if (node == null) 
                return null;

            return FindSuitableEntry(node.Entries, key, out _);
        }

        private IBPlusTreeNode<TKey> NextNodeForKey(TKey key, IBPlusTreeNode<TKey> node, bool returnNullIfOutOfRange = false)
        {
            int index;
            FindSuitableEntry(node.Entries, key, out index);
            if (index < node.Entries.Count)
                return FetchNode(node.Entries[index].Value.PageIndex);

            if(returnNullIfOutOfRange) 
                return null;

            if (index == node.Entries.Count) // tricky last node
                return FetchNode(node.Entries[index - 1].Value.PageIndex);

            Debug.Fail("Should never get here");
            return null;
        }

        private IBPlusTreeNode<TKey> GetSuitableLeafNodeForKeyIfRangeExists(TKey key)
        {
            var node = FetchRoot();

            while (node != null && !node.IsLeaf)
            {
                node = NextNodeForKey(key, node, true);
            }

            return node;
        }

        private IBPlusTreeNode<TKey> GetSuitableLeafNodeForKey(TKey key)
        {
            var node = FetchRoot();

            while (!node.IsLeaf)
            {
                node = NextNodeForKey(key, node);
            }

            return node;
        }

        private List<IBPlusTreeNode<TKey>> GetPathForKey(TKey key)
        {
            var result = new List<IBPlusTreeNode<TKey>>();
            var node = FetchRoot();
            result.Add(node);

            while (!node.IsLeaf)
            {
                node = NextNodeForKey(key, node);
                result.Add(node);
            }

            return result;
        }

        /// <summary>
        /// Gets the minimal key.
        /// </summary>
        /// <returns>The minimal key</returns>
        public TKey Min()
        {
            long index = GetMinValueNodeIndex();
            var node = FetchNode(index);

            return node.Entries.Any() ? node.Entries.First().Key : default(TKey);
        }

        /// <summary>
        /// Gets the maximal key.
        /// </summary>
        /// <returns>The maximal key</returns>
        public TKey Max()
        {
            long index = GetMaxValueNodeIndex();
            var node = FetchNode(index);

            return node.Entries.Any() ? node.Entries.Last().Key : default(TKey);
        }

        /// <summary>
        /// Gets the key previous to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key previous to specified key</returns>
        public TKey PreviousTo(TKey key)
        {
            var node = GetSuitableLeafNodeForKey(key);

            int index;

            FindSuitableEntry(node.Entries, key, out index);

            if (index == 0)
            {
                if (node.PreviousNodeIndex != -1)
                {
                    node = FetchNode(node.PreviousNodeIndex);
                    return node.Entries.Last().Key;
                }
                return default(TKey);
            }

            return node.Entries[index - 1].Key;
        }

        /// <summary>
        /// Gets the key next to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key next to specified key</returns>
        public TKey NextTo(TKey key)
        {
            var node = GetSuitableLeafNodeForKey(key);

            int index;

            FindSuitableEntry(node.Entries, key, out index);

            if (index == node.Entries.Count)
                return default(TKey);

            bool isExistingKey = node.Entries[index].Key.CompareTo(key) == 0;

            if (isExistingKey && index == node.Entries.Count - 1)
            {
                if (node.NextNodeIndex != -1)
                {
                    node = FetchNode(node.NextNodeIndex);
                    return node.Entries.First().Key;
                }
                return default(TKey);
            }

            return isExistingKey ? node.Entries[index + 1].Key : node.Entries[index].Key;
        }

        /// <summary>
        /// Gets the value by its key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The value corresponding to the given key</returns>
        public TValue Get(TKey key)
        {
            var entry = FindSuitableEntry(key);
            return entry.HasValue ? _valueStorage.Fetch(entry.Value.Value) : default(TValue);
        }

        /// <summary>
        /// Checks if key-value pair exists in tree.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if key-value pair exists, false otherwise</returns>
        public bool Exists(TKey key)
        {
            return FindSuitableEntry(key).HasValue;
        }

        /// <summary>
        /// Gets a value corresponing to the minimal key.
        /// </summary>
        /// <returns>The value corresponding to the minimal key</returns>
        public TValue MinValue()
        {
            long index = GetMinValueNodeIndex();
            var node = FetchNode(index);

            return node.Entries.Any()
                ? _valueStorage.Fetch(node.Entries.First().Value)
                : default(TValue);
        }

        /// <summary>
        /// Gets the value corresponing to the maximal key.
        /// </summary>
        /// <returns>The value corresponding to the maximal key</returns>
        public TValue MaxValue()
        {
            long index = GetMaxValueNodeIndex();
            var node = FetchNode(index);

            return node.Entries.Any()
                ? _valueStorage.Fetch(node.Entries.Last().Value)
                : default(TValue);
        }


        /// <summary>
        /// Inserts or updates key value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public void Set(TKey key, TValue value)
        {
            var keyPath = GetPathForKey(key);
            var leafNode = keyPath.Last();

            int index;

            var entry = FindSuitableEntry(leafNode.Entries, key, out index);

            if (entry.HasValue)
            {
                // update found value
                leafNode.Entries[index] =
                    new KeyValuePair<TKey, DbItemReference>(key, _valueStorage.Reallocate(entry.Value.Value, value));
                    
                _nodeStorage.Update(leafNode);
                return;
            }

            var newEntry = new KeyValuePair<TKey, DbItemReference>(key, _valueStorage.AllocateNew(value));
            var nodesToUpdate = new Dictionary<long, IBPlusTreeNode<TKey>>();
            leafNode.Entries.Insert(index, newEntry);

            // node could be in the inconsistent state while inserting new value
            // pin it to prevent writing of the corresponding page
            _nodeStorage.PinNode(leafNode.Index);
            
            AlignMaximalValues(key, keyPath, nodesToUpdate);

            if (IsOverflow(leafNode))
            {
                // the node is overflow, we should try to rotate it
                // or split if rotation is impossible
                if (!Rotate(leafNode, nodesToUpdate))
                    Split(leafNode, nodesToUpdate);
            }
            else
                _nodeStorage.Update(leafNode);

            foreach (var nodeToUpdate in nodesToUpdate)
                _nodeStorage.Update(nodeToUpdate.Value);
        }

        /// <summary>
        /// Removes key-value pair by key.
        /// </summary>
        /// <param name="key">The key</param>
        public void Remove(TKey key)
        {
            var leafNode = GetSuitableLeafNodeForKeyIfRangeExists(key);
            if(leafNode == null)
                return;

            int index;

            var entry = FindSuitableEntry(leafNode.Entries, key, out index);
            if (entry.HasValue)
            {
                // free referenced value
                _valueStorage.Free(leafNode.Entries[index].Value);

                // delete reference from node
                leafNode.Entries.RemoveAt(index);

                if (IsSufficientlyFilled(leafNode))
                {
                    _nodeStorage.Update(leafNode);
                    return;
                }

                var nodesToUpdate = new Dictionary<long, IBPlusTreeNode<TKey>>();
                CombineIfNeeded(leafNode, nodesToUpdate);

                foreach (var nodeToUpdate in nodesToUpdate)
                    _nodeStorage.Update(nodeToUpdate.Value);
            }
        }

        /// <summary>
        /// Retrieves a segment of binary representation
        /// of the value referenced by the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="startIndex">The index in binary representation where the specified segment starts</param>
        /// <param name="endIndex">The index in binary representation where the specified segment ends</param>
        /// <returns></returns>
        public byte[] GetRawDataSegment(TKey key, long startIndex, long endIndex)
        {
            var entry = FindSuitableEntry(key);
            return entry.HasValue ? _valueStorage.GetRawDataSegment(entry.Value.Value, startIndex, endIndex) : null;
        }

        /// <summary>
        /// Computes count of key-value pairs in the tree.
        /// </summary>
        /// <returns>The count of key-value pairs</returns>
        public long Count()
        {
            var index = GetMinValueNodeIndex();
            if (index == -1)
                return 0;

            long result = 0;

            var node = FetchNode(index);
            while (node != null)
            {
                result += node.Entries.Count;
                node = _nodeStorage.Fetch(node.NextNodeIndex);
            }

            return result;
        }

        /// <summary>
        /// Retrieves the length (in bytes) of binary representation
        /// of the value referenced by the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The length of binary representation</returns>
        public long GetRawDataLength(TKey key)
        {
            var entry = FindSuitableEntry(key);
            return entry.HasValue ? _valueStorage.GetRawDataLength(entry.Value.Value) : 0;
        }

        private void MoveSecondHalfEntries(IBPlusTreeNode<TKey> source, IBPlusTreeNode<TKey> receiver)
        {
            for (int i = _nodeStorage.NodeCapacity / 2; i < source.Entries.Count; i++)
                receiver.Entries.Add(source.Entries[i]);

            for (int i = source.Entries.Count - 1; i >= _nodeStorage.NodeCapacity / 2; i--)
                source.Entries.RemoveAt(i);
        }

        private IBPlusTreeNode<TKey> FetchNode(long index)
        {
            return _nodeStorage.Fetch(index);
        }

        private IBPlusTreeNode<TKey> FetchRoot()
        {
            return _nodeStorage.FetchRoot();
        }

        private IBPlusTreeNode<TKey> FetchNode(long index, IDictionary<long, IBPlusTreeNode<TKey>> list)
        {
            return list.ContainsKey(index) 
                ? list[index]
                : FetchNode(index);
        }

        private void Split(IBPlusTreeNode<TKey> node, IDictionary<long, IBPlusTreeNode<TKey>> nodesToUpdate)
        {
            if (!IsOverflow(node)) return;

            nodesToUpdate[node.Index] = node;

            var newNode = _nodeStorage.Create(node.IsLeaf);
            nodesToUpdate[newNode.Index] = newNode;

            // link nodes together
            newNode.PreviousNodeIndex = node.Index;
            newNode.NextNodeIndex = node.NextNodeIndex;
            node.NextNodeIndex = newNode.Index;

            if (newNode.HasNext)
            {
                var nextNode = FetchNode(newNode.NextNodeIndex, nodesToUpdate);
                nextNode.PreviousNodeIndex = newNode.Index;
                nodesToUpdate[nextNode.Index] = nextNode;
            }

            // move half of the entries to new node
            MoveSecondHalfEntries(node, newNode);

            // prepare new entries for parent node
            var entry1ForParent = 
                new KeyValuePair<TKey, DbItemReference>(node.Entries.Last().Key,
                        new DbItemReference(node.Index, 0));

            var entry2ForParent = 
                new KeyValuePair<TKey, DbItemReference>(newNode.Entries.Last().Key,
                        new DbItemReference(newNode.Index, 0));

            // change links in childs to new node
            ChangeParentReferencesInChildNodes(newNode, newNode.Index, nodesToUpdate);

            IBPlusTreeNode<TKey> parentNode;

            if(!node.HasParent)
            {
                // handle case of new root 
                parentNode =_nodeStorage.Create(false); 
                _nodeStorage.SetRoot(parentNode.Index);

                node.ParentNodeIndex = parentNode.Index;
                newNode.ParentNodeIndex = parentNode.Index;

                nodesToUpdate[parentNode.Index] = parentNode;

                parentNode.Entries.Add(entry1ForParent);
                parentNode.Entries.Add(entry2ForParent);

                return;
            }

            parentNode = FetchNode(node.ParentNodeIndex, nodesToUpdate);
            _nodeStorage.PinNode(parentNode.Index);
            nodesToUpdate[parentNode.Index] = parentNode;

            newNode.ParentNodeIndex = node.ParentNodeIndex;

            int index;
            FindSuitableEntry(parentNode.Entries, node.Entries.First().Key, out index);
            parentNode.Entries.RemoveAt(index);
            parentNode.Entries.Insert(index, entry2ForParent);
            parentNode.Entries.Insert(index, entry1ForParent);

            Split(parentNode, nodesToUpdate);
        }

        private void ChangeParentReferencesInChildNodes(IBPlusTreeNode<TKey> node, long newIndex, IDictionary<long, IBPlusTreeNode<TKey>> nodesToUpdate)
        {
            if (node.IsLeaf) 
                return;

            foreach (var entry in node.Entries)
            {
                var childNode = FetchNode(entry.Value.PageIndex, nodesToUpdate);
                childNode.ParentNodeIndex = newIndex;
                nodesToUpdate[childNode.Index] = childNode;
            }
        }

        private void Combine(IBPlusTreeNode<TKey> left, IBPlusTreeNode<TKey> right, IDictionary<long, IBPlusTreeNode<TKey>> nodesToUpdate)
        {
            var parent = FetchNode(right.ParentNodeIndex, nodesToUpdate);
            int index;

            // one node can be empty
            if (left.Entries.Any())
                FindSuitableEntry(parent.Entries, left.Entries.First().Key, out index);
            else
            {
                FindSuitableEntry(parent.Entries, right.Entries.First().Key, out index);
                index--;
            }

            ChangeParentReferencesInChildNodes(right, left.Index, nodesToUpdate);

            _nodeStorage.Remove(right.Index);
            nodesToUpdate.Remove(right.Index);
            nodesToUpdate[left.Index] = left;
            left.NextNodeIndex = right.NextNodeIndex;

            foreach (var entry in right.Entries)
                left.Entries.Add(entry);

            if (right.HasNext)
            {
                var nextNode = FetchNode(right.NextNodeIndex, nodesToUpdate);
                nextNode.PreviousNodeIndex = left.Index;
                nodesToUpdate[nextNode.Index] = nextNode;
            }

            var entryForParent =
                new KeyValuePair<TKey, DbItemReference>(left.Entries.Last().Key,
                    new DbItemReference(left.Index, 0));

            parent.Entries[index] = entryForParent;
            parent.Entries.RemoveAt(index + 1);

            if (!parent.HasParent && parent.Entries.Count == 1)
            {
                // there are no more significant entries in the root node
                // we have a new root
                _nodeStorage.SetRoot(left.Index);
                left.ParentNodeIndex = -1;
                _nodeStorage.Remove(parent.Index);
                nodesToUpdate.Remove(parent.Index);
            }
            else
            {
                nodesToUpdate[parent.Index] = parent;
                CombineIfNeeded(parent, nodesToUpdate);
            }
        }

        private void CombineIfNeeded(IBPlusTreeNode<TKey> node, IDictionary<long, IBPlusTreeNode<TKey>> nodesToUpdate)
        {
            if(IsSufficientlyFilled(node))
                return;

            nodesToUpdate[node.Index] = node;

            if (node.HasPrevious)
            {
                var leftSibling = FetchNode(node.PreviousNodeIndex, nodesToUpdate);
                if (leftSibling.ParentNodeIndex == node.ParentNodeIndex)
                {
                    if (leftSibling.Entries.Count + node.Entries.Count <= _nodeStorage.NodeCapacity)
                    {
                        Combine(leftSibling, node, nodesToUpdate);
                        return;
                    }
                }
            }

            if (node.HasNext)
            {
                var rightSibling = FetchNode(node.NextNodeIndex, nodesToUpdate);
                if (rightSibling.ParentNodeIndex == node.ParentNodeIndex)
                {
                    if (rightSibling.Entries.Count + node.Entries.Count <= _nodeStorage.NodeCapacity)
                    {
                        Combine(node, rightSibling, nodesToUpdate);
                    }
                }
            }
        }

        private void AlignMaximalValues(TKey key, IEnumerable<IBPlusTreeNode<TKey>> path, IDictionary<long, IBPlusTreeNode<TKey>> nodesToUpdate)
        {
            foreach (var pathNode in path)
            {
                var lastEntry = pathNode.Entries.Last();
                if (lastEntry.Key.CompareTo(key) == -1)
                {
                    pathNode.Entries[pathNode.Entries.Count - 1] = new KeyValuePair<TKey, DbItemReference>(key, lastEntry.Value);
                    nodesToUpdate[pathNode.Index] = pathNode;
                }
            }
        }


        private bool Rotate(IBPlusTreeNode<TKey> node, IDictionary<long, IBPlusTreeNode<TKey>> nodesToUpdate)
        {
            if (node.HasPrevious)
            {
                var leftSibling = FetchNode(node.PreviousNodeIndex);
                if(leftSibling.ParentNodeIndex == node.ParentNodeIndex)
                {
                    if (!IsFull(leftSibling))
                    {
                        var parentNode = FetchNode(node.ParentNodeIndex, nodesToUpdate);

                        RedistributeEntries(leftSibling, node, parentNode);

                        nodesToUpdate[leftSibling.Index] = leftSibling;
                        nodesToUpdate[node.Index] = node;
                        nodesToUpdate[parentNode.Index] = parentNode;

                        return true;
                    }
                }
            }

            if(node.HasNext)
            {
                var rightSibling = FetchNode(node.NextNodeIndex);
                if (rightSibling.ParentNodeIndex == node.ParentNodeIndex)
                {
                    if (!IsFull(rightSibling))
                    {
                        var parentNode = FetchNode(node.ParentNodeIndex, nodesToUpdate);
                        RedistributeEntries(node, rightSibling, parentNode);

                        nodesToUpdate[node.Index] = node;
                        nodesToUpdate[rightSibling.Index] = rightSibling;
                        nodesToUpdate[parentNode.Index] = parentNode;

                        return true;
                    }
                }
            }

            return false;
        }

        private void RedistributeEntries(IBPlusTreeNode<TKey> left, IBPlusTreeNode<TKey> right, IBPlusTreeNode<TKey> parent)
        {
            // get the index of parent entry referencing to the left node
            int index;
            FindSuitableEntry(parent.Entries, left.Entries.First().Key, out index);

            // get concatenated sequence of leaf entries
            var entries = left.Entries.Concat(right.Entries).ToList();

            // clear entries of leaf nodes
            left.Entries.Clear();
            right.Entries.Clear();

            // break sequence in half
            int halfCount = entries.Count / 2;
            entries.Take(halfCount).ToList().ForEach(left.Entries.Add);
            entries.Skip(halfCount).ToList().ForEach(right.Entries.Add);

            var entry1ForParent =
                new KeyValuePair<TKey, DbItemReference>(left.Entries.Last().Key,
                    new DbItemReference(left.Index, 0));

            var entry2ForParent =
                new KeyValuePair<TKey, DbItemReference>(right.Entries.Last().Key,
                        new DbItemReference(right.Index, 0));
            
            // update references in parent node
            parent.Entries[index] = entry1ForParent;
            parent.Entries[index + 1] = entry2ForParent;
        }

        private bool IsOverflow(IBPlusTreeNode<TKey> node)
        {
            return node.Entries.Count > _nodeStorage.NodeCapacity;
        }

        private bool IsFull(IBPlusTreeNode<TKey> node)
        {
            return node.Entries.Count == _nodeStorage.NodeCapacity;
        }

        private bool IsSufficientlyFilled(IBPlusTreeNode<TKey> node)
        {
            return (double)node.Entries.Count / _nodeStorage.NodeCapacity > _fillFactor;
        }

        #region Consistency checks

        private bool CheckNodeReferencesAreValid(IBPlusTreeNode<TKey> node, out string message)
        {
            message = string.Empty;

            if (node.HasParent)
            {
                if (FetchNode(node.ParentNodeIndex) == null)
                {
                    message = $"Node: {node.Index} has invalid reference ({node.ParentNodeIndex}) to parent node.";
                    return false;
                }
            }

            if (node.HasPrevious)
            {
                var previous = FetchNode(node.PreviousNodeIndex);
                if (previous == null)
                {
                    message = $"Node: {node.Index} has invalid reference ({node.PreviousNodeIndex}) to previous node.";
                    return false;
                }
                if(previous.NextNodeIndex != node.Index)
                {
                    message =
                        $"Node: {node.Index} has reference to previous node: {node.PreviousNodeIndex}. But this node references to next node: {previous.NextNodeIndex}";
                    return false;
                }
            }

            if (node.HasNext)
            {
                var next = FetchNode(node.NextNodeIndex);

                if (next == null)
                {
                    message = $"Node: {node.Index} has invalid reference ({node.NextNodeIndex}) to next node.";
                    return false;
                }
                if (next.PreviousNodeIndex != node.Index)
                {
                    message =
                        $"Node: {node.Index} has reference to next node: {node.NextNodeIndex}. But this node references to previous node: {next.PreviousNodeIndex}";
                    return false;
                }
            }

            return true;            
        }

        private bool CheckNodeEntriesAreOrdered(IBPlusTreeNode<TKey> node, out string message)
        {
            message = string.Empty;
            for (int i = 0; i < node.Entries.Count - 1; i++)
            {
                if (node.Entries[i].Key.CompareTo(node.Entries[i + 1].Key) >= 0)
                {
                    message = $"Disordered entries in node: {node.Index}";
                    return false;
                }
            }

            return true;
        }

        private bool CheckNodeRanges(IBPlusTreeNode<TKey> node, out string message)
        {
            message = string.Empty;

            //if (node.HasParent)
            //{
            //    var parent = FetchNode(node.ParentNodeIndex);
            //    var largestKey = node.Entries.Last().Key;
            //    var parentLargestKey = parent.Entries.Last().Key;
            //    if(parentLargestKey.CompareTo(largestKey) < 0)
            //    {
            //        message = string.Format("Largest key ({0}) of node: {1} is greater than the largest key ({2}) of its parent node: {3}",
            //                    largestKey, node.Index, parentLargestKey, node.ParentNodeIndex);
            //        return false;
            //    }
            //}

            if (node.HasPrevious)
            {
                var previous = FetchNode(node.PreviousNodeIndex);
                var smallestKey = node.Entries.First().Key;
                var largestKey = previous.Entries.Last().Key;

                if (previous.Entries.Last().Key.CompareTo(node.Entries.First().Key) >= 0)
                {
                    message =
                        $"Smallest key ({smallestKey}) of node: {node.Index} is smaller than the largest key ({largestKey}) of previous node: {node.PreviousNodeIndex}";
                    return false;
                }
            }

            return true;
        }

        private bool CheckNode(IBPlusTreeNode<TKey> node, out string message)
        {
            if(node.Entries.Count == 0 && node.HasParent)
            {
                message = $"Empty node: {node.Index}";
                return false;
            }

            if (!CheckNodeEntriesAreOrdered(node, out message))
                return false;

            if (!CheckNodeReferencesAreValid(node, out message))
                return false;

            if (!CheckNodeRanges(node, out message))
                return false;

            if (!node.IsLeaf)
            {
                foreach (var entry in node.Entries)
                {
                    var child = FetchNode(entry.Value.PageIndex);
                    if (child == null)
                    {
                        message = $"Invalid reference ({entry.Value.PageIndex}) to child in node: {node.Index}";
                        return false;
                    }

                    if (!CheckNode(child, out message))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks the tree for consistency.
        /// </summary>
        /// <param name="message">Diagnostic message describing the specific inconsistencies</param>
        /// <returns>True if the tree is consisternt, false otherwise</returns>
        public bool CheckConsistency(out string message)
        {
            var rootNode = FetchRoot();
            if(rootNode == null)
            {
                message = "Root node not found";
                return false;
            }

            return CheckNode(rootNode, out message);
        }

        #endregion

        private long GetMinValueNodeIndex()
        {
            var node = FetchRoot();

            while (node.Entries.Any() && !node.IsLeaf)
                node = FetchNode(node.Entries.First().Value.PageIndex);

            return node.Index;
        }

        private long GetMaxValueNodeIndex()
        {
            var node = FetchRoot();

            while (node.Entries.Any() && !node.IsLeaf)
                node = FetchNode(node.Entries.Last().Value.PageIndex);

            return node.Index;
        }

        public BPlusTree(INodeStorage<TKey> nodeStorage, IValueStorage<TValue> valueStorage)
        {
            _nodeStorage = nodeStorage;
            _valueStorage = valueStorage;
        }
    }
}
