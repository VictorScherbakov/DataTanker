namespace DataTanker.AccessMethods.RadixTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using MemoryManagement;

    /// <summary>
    /// Implements radix tree with R=256
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class RadixTree<TKey, TValue> : IRadixTree<TKey, TValue>
        where TValue : IValue 
        where TKey : IKey
    {
        private readonly INodeStorage _nodeStorage;
        private readonly IValueStorage<TValue> _valueStorage;
        private readonly ISerializer<TKey> _keySerializer;

        public int MaxPrefixLength { get; }

        private static KeyValuePair<byte, DbItemReference>? FindSuitableEntry(IList<KeyValuePair<byte, DbItemReference>> entries, byte b, out int index)
        {
            int first = 0;
            int last = entries.Count;

            if (last == 0)
            {
                index = 0;
                return null;
            }

            if (entries[0].Key > b)
            {
                index = 0;
                return null;
            }

            if (entries[last - 1].Key < b)
            {
                index = last;
                return null;
            }

            while (first < last)
            {
                int mid = first + (last - first) / 2;

                if (b <= entries[mid].Key)
                    last = mid;
                else
                    first = mid + 1;
            }

            index = last;

            if (entries[last].Key == b)
                return entries[last];

            return null;
        }

        /// <summary>
        /// Gets the minimal key.
        /// </summary>
        /// <returns>The minimal key</returns>
        public TKey Min()
        {
            var prefixes = new List<byte[]>();

            IRadixTreeNode node = _nodeStorage.FetchRoot();
            while (true)
            {
                prefixes.Add(node.Prefix);
                if(node.ValueReference != null)
                    break;

                if (node.Entries.Any())
                    node = _nodeStorage.Fetch(node.Entries.First().Value);
            }

            using (var ms = new MemoryStream())
            {
                foreach (var prefix in prefixes.Where(p => p != null))
                    ms.Write(prefix, 0, prefix.Length);

                return _keySerializer.Deserialize(ms.ToArray());
            }
        }

        /// <summary>
        /// Gets the maximal key.
        /// </summary>
        /// <returns>The maximal key</returns>
        public TKey Max()
        {
            var prefixes = new List<byte[]>();

            IRadixTreeNode node = _nodeStorage.FetchRoot();
            while (true)
            {
                prefixes.Add(node.Prefix);

                if (node.Entries.Any())
                    node = _nodeStorage.Fetch(node.Entries.Last().Value);
                else
                    break;
            }

            using (var ms = new MemoryStream())
            {
                foreach (var prefix in prefixes.Where(p => p != null))
                    ms.Write(prefix, 0, prefix.Length);

                return _keySerializer.Deserialize(ms.ToArray());
            }
        }

        /// <summary>
        /// Determines if the specified key has subkeys.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasSubkeys(TKey key)
        {
            var binaryKey = _keySerializer.Serialize(key);
            bool isFullMatch;
            IRadixTreeNode node = FindMostSuitableNode(binaryKey, out _, out _, out isFullMatch);

            return isFullMatch && node.Entries.Any();
        }


        /// <summary>
        /// Computes the number of child key-value pairs for a given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public long SubkeysCount(TKey key)
        {
            var binaryKey = _keySerializer.Serialize(key);
            bool isFullMatch;
            IRadixTreeNode node = FindMostSuitableNode(binaryKey, out _, out _, out isFullMatch);

            long result = 0;

            if(isFullMatch) 
                Count(node, ref result);

            return result;
        }

        private IRadixTreeNode GetChildPreviousTo(IRadixTreeNode node, byte b)
        {
            int index;
            FindSuitableEntry(node.Entries, b, out index);
            if (index <= 0 || index > node.Entries.Count)
                return null;
            return _nodeStorage.Fetch(node.Entries[index - 1].Value);
        }

        private IRadixTreeNode GetChildNextTo(IRadixTreeNode node, byte b)
        {
            int index;
            var entry = FindSuitableEntry(node.Entries, b, out index);
            if (entry != null)
                index++;

            if (index < 0 || index >= node.Entries.Count)
                return null;
            return _nodeStorage.Fetch(node.Entries[index].Value);
        }

        private TKey BuildKeyForNode(IRadixTreeNode node)
        {
            var prefixes = new List<byte[]>();

            while (!DbItemReference.IsNull(node.ParentNodeReference))
            {
                prefixes.Add(node.Prefix);
                node = _nodeStorage.Fetch(node.ParentNodeReference);
            }

            prefixes.Reverse();

            using (var ms = new MemoryStream())
            {
                foreach (var prefix in prefixes.Where(p => p != null))
                    ms.Write(prefix, 0, prefix.Length);

                return _keySerializer.Deserialize(ms.ToArray());
            }
        }

        /// <summary>
        /// Gets the key previous to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key previous to specified key</returns>
        public TKey PreviousTo(TKey key)
        {
            var binaryKey = _keySerializer.Serialize(key);
            int keyOffset;
            int nodePrefixOffset;
            bool isFullMatch;
            IRadixTreeNode targetNode = FindMostSuitableNode(binaryKey, out keyOffset, out nodePrefixOffset, out isFullMatch);

            bool goDown = true;
            if (isFullMatch || keyOffset == binaryKey.Length || 
                (targetNode.Prefix != null && nodePrefixOffset < targetNode.Prefix.Length))
            {
                while (!DbItemReference.IsNull(targetNode.ParentNodeReference))
                {
                    var parent = _nodeStorage.Fetch(targetNode.ParentNodeReference);
                    targetNode = GetChildPreviousTo(parent, targetNode.Prefix[0]);
                    if (targetNode != null) break;
                    targetNode = parent;

                    if (DbItemReference.IsNull(targetNode.ValueReference)) continue;

                    goDown = false;
                    break;
                }
            }
            else
            {
                byte b = binaryKey[keyOffset];
                while (true)
                {
                    var child = GetChildPreviousTo(targetNode, b);
                    if (child != null)
                    {
                        targetNode = child;
                        break;
                    }

                    if(DbItemReference.IsNull(targetNode.ParentNodeReference))
                        break;

                    if (!DbItemReference.IsNull(targetNode.ValueReference))
                    {
                        goDown = false;
                        break;
                    }

                    b = targetNode.Prefix[0];
                    targetNode = _nodeStorage.Fetch(targetNode.ParentNodeReference);
                }
            }

            if (DbItemReference.IsNull(targetNode.ParentNodeReference))
                return default(TKey);

            while (goDown && targetNode.Entries.Any())
                targetNode = _nodeStorage.Fetch(targetNode.Entries.Last().Value);

            return BuildKeyForNode(targetNode);
        }

        /// <summary>
        /// Gets the key next to the specified key.
        /// The existence of the specified key is not required.
        /// </summary>
        /// <returns>The key next to specified key</returns>
        public TKey NextTo(TKey key)
        {
            var binaryKey = _keySerializer.Serialize(key);
            int keyOffset;
            bool isFullMatch;
            IRadixTreeNode targetNode = FindMostSuitableNode(binaryKey, out keyOffset, out _, out isFullMatch);

            if (isFullMatch || keyOffset == binaryKey.Length)
            {
                if (targetNode.Entries.Any())
                {
                    targetNode = _nodeStorage.Fetch(targetNode.Entries.First().Value);
                }
                else
                {
                    while (!DbItemReference.IsNull(targetNode.ParentNodeReference))
                    {
                        var parent = _nodeStorage.Fetch(targetNode.ParentNodeReference);
                        targetNode = GetChildNextTo(parent, targetNode.Prefix[0]);
                        if (targetNode != null) break;
                        targetNode = parent;
                    }
                }
            }
            else
            {
                byte b = binaryKey[keyOffset];
                while (true)
                {
                    var child = GetChildNextTo(targetNode, b);
                    if (child != null)
                    {
                        targetNode = child;
                        break;
                    }

                    if (DbItemReference.IsNull(targetNode.ParentNodeReference))
                        break;

                    b = targetNode.Prefix[0];
                    targetNode = _nodeStorage.Fetch(targetNode.ParentNodeReference);
                }
            }

            if (DbItemReference.IsNull(targetNode.ParentNodeReference))
                return default(TKey);

            while (DbItemReference.IsNull(targetNode.ValueReference) && targetNode.Entries.Any())
                targetNode = _nodeStorage.Fetch(targetNode.Entries.First().Value);

            return BuildKeyForNode(targetNode);
        }

        /// <summary>
        /// Gets the value by its key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The value corresponding to the given key</returns>
        public TValue Get(TKey key)
        {
            var binaryKey = _keySerializer.Serialize(key);
            bool isFullMatch;

            IRadixTreeNode node = FindMostSuitableNode(binaryKey, out _, out _, out isFullMatch/*, treeNode => references.Add(treeNode.Reference)*/);

            if (isFullMatch)
            {
                if(node.ValueReference != null)
                    return _valueStorage.Fetch(node.ValueReference);
            }

            return default(TValue);
        }

        /// <summary>
        /// Gets a value corresponing to the minimal key.
        /// </summary>
        /// <returns>The value corresponding to the minimal key</returns>
        public TValue MinValue()
        {
            IRadixTreeNode node = _nodeStorage.FetchRoot();
            while (true)
            {
                if (node.ValueReference != null)
                    break;

                if (node.Entries.Any())
                    node = _nodeStorage.Fetch(node.Entries.First().Value);
            }

            return _valueStorage.Fetch(node.ValueReference);

        }

        /// <summary>
        /// Gets the value corresponing to the maximal key.
        /// </summary>
        /// <returns>The value corresponding to the maximal key</returns>
        public TValue MaxValue()
        {
            IRadixTreeNode node = _nodeStorage.FetchRoot();
            while (true)
            {
                if (node.Entries.Any())
                    node = _nodeStorage.Fetch(node.Entries.Last().Value);
                else
                    break;
            }

            return _valueStorage.Fetch(node.ValueReference);
        }

        private IRadixTreeNode UpdateOrReallocateNode(IRadixTreeNode node, out bool reallocated)
        {
            if (!_nodeStorage.Update(node))
            {
                reallocated = true;
                _nodeStorage.Remove(node.Reference);

                var reallocatedNode = _nodeStorage.Create(node.Prefix.Length, node.Entries.Count);
                reallocatedNode.Prefix = node.Prefix;
                reallocatedNode.ValueReference = node.ValueReference;
                reallocatedNode.ParentNodeReference = node.ParentNodeReference;
                foreach (var entry in node.Entries)
                    reallocatedNode.Entries.Add(entry);

                _nodeStorage.Update(reallocatedNode);

                return reallocatedNode;
            }

            reallocated = false;
            return node;
        }

        /// <summary>
        /// This method is used to guarantee the updating.
        /// Use this if the updating node is not increased in size.
        /// </summary>
        /// <param name="node"></param>
        private void UpdateOrFail(IRadixTreeNode node)
        {
            if (!_nodeStorage.Update(node))
            {
                throw new DataTankerException("Update failed. Node should be reallocated");
            }
        }

        /// <summary>
        /// Removes key-value pair by key.
        /// </summary>
        /// <param name="key">The key</param>
        public void Remove(TKey key)
        {
            var binaryKey = _keySerializer.Serialize(key);
            bool isFullMatch;
            IRadixTreeNode node = FindMostSuitableNode(binaryKey, out _, out _, out isFullMatch);

            if (isFullMatch)
            {
                _valueStorage.Free(node.ValueReference);
                node.ValueReference = null;

                if (node.Entries.Any())
                {
                    if (node.Prefix != null && node.Entries.Count == 1)
                    {
                        var child = _nodeStorage.Fetch(node.Entries[0].Value);

                        var newPrefixLength = node.Prefix.Length + child.Prefix.Length;
                        if (newPrefixLength <= MaxPrefixLength)
                        {
                            var newPrefix = new byte[newPrefixLength];

                            Buffer.BlockCopy(node.Prefix, 0, newPrefix, 0, node.Prefix.Length);
                            Buffer.BlockCopy(child.Prefix, 0, newPrefix, node.Prefix.Length, child.Prefix.Length);

                            child.Prefix = newPrefix;
                            child.ParentNodeReference = node.ParentNodeReference;

                            _nodeStorage.Remove(node.Reference);
                            child = UpdateOrReallocateNode(child, out _);

                            var parent = _nodeStorage.Fetch(child.ParentNodeReference);
                            int index;
                            FindSuitableEntry(parent.Entries, child.Prefix[0], out index);
                            parent.Entries[index] = new KeyValuePair<byte, DbItemReference>(child.Prefix[0], child.Reference);

                            UpdateOrFail(parent);
                        }
                        else UpdateOrFail(node);
                    }
                    else
                    {
                        UpdateOrFail(node);
                    }
                }
                else
                {
                    while (!DbItemReference.IsNull(node.ParentNodeReference))
                    {
                        var parentNode = _nodeStorage.Fetch(node.ParentNodeReference);
                        int index;
                        FindSuitableEntry(parentNode.Entries, node.Prefix[0], out index);
                        parentNode.Entries.RemoveAt(index);
                        _nodeStorage.Remove(node.Reference);

                        if (parentNode.Entries.Any() || parentNode.ValueReference != null
                            || DbItemReference.IsNull(parentNode.ParentNodeReference))
                        {
                            UpdateOrFail(parentNode);
                            break;
                        }

                        node = parentNode;
                    }
                }
            }
        }

        private List<IRadixTreeNode> CreateNodeChain(byte[] remainingKeyPart)
        {
            var result = new List<IRadixTreeNode>();

            var offset = 0;
            IRadixTreeNode previousNode = null;
            while (offset < remainingKeyPart.Length)
            {
                var prefixLength = Math.Min(MaxPrefixLength, remainingKeyPart.Length - offset);
                var newNode = _nodeStorage.Create(prefixLength, 1);

                newNode.Prefix = new byte[prefixLength];
                Buffer.BlockCopy(remainingKeyPart, offset, newNode.Prefix, 0, newNode.Prefix.Length);

                if (previousNode != null)
                {
                    previousNode.Entries.Add(new KeyValuePair<byte, DbItemReference>(newNode.Prefix[0], newNode.Reference));
                    newNode.ParentNodeReference = previousNode.Reference;
                }

                result.Add(newNode);

                previousNode = newNode;

                offset += MaxPrefixLength;
                if (offset > remainingKeyPart.Length)
                    offset = remainingKeyPart.Length;
            }

            return result;
        }

        private static void SplitPrefix(byte[] prefix, int offset, out byte[] parentPrefix, out byte[] remainingPrefix)
        {
            remainingPrefix = new byte[prefix.Length - offset];
            parentPrefix = new byte[offset];

            Buffer.BlockCopy(prefix, 0, parentPrefix, 0, parentPrefix.Length);
            Buffer.BlockCopy(prefix, offset, remainingPrefix, 0, remainingPrefix.Length);
        }

        private static IEnumerable<KeyValuePair<byte, DbItemReference>> GetChildLinks(IRadixTreeNode node1, IRadixTreeNode node2)
        {
            KeyValuePair<byte, DbItemReference>[] result;

            if (node1 != null)
            {
                result = new[]
                                {
                                    new KeyValuePair<byte, DbItemReference>(node2.Prefix[0], (DbItemReference)(node2.Reference.Clone())),
                                    new KeyValuePair<byte, DbItemReference>(node1.Prefix[0], (DbItemReference)(node1.Reference.Clone()))
                                };

                if (node2.Prefix[0] > node1.Prefix[0])
                    Array.Reverse(result);
            }
            else
                result = new[]
                                {
                                    new KeyValuePair<byte, DbItemReference>(node2.Prefix[0], (DbItemReference)(node2.Reference.Clone()))
                                };

            return result;
        }

        private void AdjustParentNodeLink(IRadixTreeNode node, bool updateNode)
        {
            var parentNode = _nodeStorage.Fetch(node.ParentNodeReference);

            var b = node.Prefix[0];
            int index;
            if (FindSuitableEntry(parentNode.Entries, b, out index).HasValue)
            {
                parentNode.Entries[index] = new KeyValuePair<byte, DbItemReference>(b, (DbItemReference)(node.Reference.Clone()));
                if(updateNode)
                    UpdateOrFail(parentNode);
            }   
        }

        private void AdjustLinksInChildNodes(IRadixTreeNode node, bool update)
        {
            if (node.Entries.Any())
            {
                foreach (var entry in node.Entries)
                {
                    var child = _nodeStorage.Fetch(entry.Value);
                    child.ParentNodeReference = node.Reference;
                    if (update)
                        UpdateOrFail(child);
                }
            }
        }

        /// <summary>
        /// Inserts or updates key value pair.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public void Set(TKey key, TValue value)
        {
            var binaryKey = _keySerializer.Serialize(key);
            int keyOffset;
            int nodePrefixOffset;
            bool isFullMatch;

            IRadixTreeNode targetNode = FindMostSuitableNode(binaryKey, out keyOffset, out nodePrefixOffset, out isFullMatch);

            if (isFullMatch)
            {
                // here we needn't any manipulation with key prefixes
                // simply update the existing or allocate a new value
                targetNode.ValueReference =
                    targetNode.ValueReference != null
                        ? _valueStorage.Reallocate(targetNode.ValueReference, value)
                        : _valueStorage.AllocateNew(value);

                UpdateOrFail(targetNode);
                return;
            }
            
            // we have a partial match
            var remainingKeyPart = new byte[binaryKey.Length - keyOffset];

            var nodeChain = new List<IRadixTreeNode>();

            if(remainingKeyPart.Length > 0)
            {
                Buffer.BlockCopy(binaryKey, keyOffset, remainingKeyPart, 0, remainingKeyPart.Length);

                // the remaining prefix may exceed the _maxPrefixLength
                // we must create a chain of nodes, where each node 
                // contain part of the prefix remaining 
                nodeChain = CreateNodeChain(remainingKeyPart);
            }

            if (targetNode.Prefix != null && nodePrefixOffset < targetNode.Prefix.Length)
            {
                // we got into splitting update:
                // the targetNode is split into newParentNode and targetNode itself
                // nodeChain (if has any items) is attached to newParentNode as a child

                // split prefix
                byte[] remainingPrefix;
                byte[] prefixForNewParent;

                SplitPrefix(targetNode.Prefix, nodePrefixOffset, out prefixForNewParent, out remainingPrefix);

                targetNode.Prefix = remainingPrefix;

                var newParentNode = _nodeStorage.Create(prefixForNewParent.Length, 2);

                newParentNode.Prefix = prefixForNewParent;
                newParentNode.ParentNodeReference = targetNode.ParentNodeReference;
                targetNode.ParentNodeReference = newParentNode.Reference;

                var valueReference = _valueStorage.AllocateNew(value);
                if (nodeChain.Any())
                {
                    nodeChain.First().ParentNodeReference = newParentNode.Reference;
                    nodeChain.Last().ValueReference = valueReference;
                }
                else
                    newParentNode.ValueReference = valueReference;

                // add links to child nodes
                foreach (var item in GetChildLinks(nodeChain.FirstOrDefault(), targetNode))
                    newParentNode.Entries.Add(item);

                if (nodeChain.Any())
                {
                    foreach (var node in nodeChain)
                        UpdateOrFail(node);    
                }

                UpdateOrFail(newParentNode);
                UpdateOrFail(targetNode);

                // change link in parent node if needed
                if (!DbItemReference.IsNull(newParentNode.ParentNodeReference))
                    AdjustParentNodeLink(newParentNode, true);
            }
            else
            {
                // here is keeping update
                // all nodes above remain intact

                byte b = remainingKeyPart[0];
                int index;
                FindSuitableEntry(targetNode.Entries, b, out index);

                targetNode.Entries.Insert(index, new KeyValuePair<byte, DbItemReference>(b, (DbItemReference)(nodeChain.First().Reference.Clone())));
                bool reallocated;
                targetNode = UpdateOrReallocateNode(targetNode, out reallocated);

                if (reallocated)
                {
                    AdjustParentNodeLink(targetNode, true);
                    AdjustLinksInChildNodes(targetNode, true);
                }

                nodeChain.First().ParentNodeReference = targetNode.Reference;
                nodeChain.Last().ValueReference = _valueStorage.AllocateNew(value);

                foreach (var node in nodeChain)
                    UpdateOrFail(node);  
            }
        }

        /// <summary>
        /// Cheks if key-value pair exists in tree.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if key-value pair exists, false otherwise</returns>
        public bool Exists(TKey key)
        {
            var binaryKey = _keySerializer.Serialize(key);
            bool isFullMatch;
            IRadixTreeNode node = FindMostSuitableNode(binaryKey, out _, out _, out isFullMatch);

            if (isFullMatch)
            {
                if (node.ValueReference != null)
                    return true;
            }

            return false;
        }

        private void Count(IRadixTreeNode node, ref long count)
        {
            if (node.ValueReference != null)
                count++;

            foreach (var entry in node.Entries)
            {
                var child = _nodeStorage.Fetch(entry.Value);
                Count(child, ref count);
            }
        }

        /// <summary>
        /// Computes the count of key-value pairs in tree.
        /// </summary>
        /// <returns>the count of key-value pairs</returns>
        public long Count()
        {
            long result = 0;

            var root = _nodeStorage.FetchRoot();
            Count(root, ref result);

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
            var binaryKey = _keySerializer.Serialize(key);
            bool isFullMatch;
            IRadixTreeNode node = FindMostSuitableNode(binaryKey, out _, out _, out isFullMatch);

            if (isFullMatch)
            {
                if (node.ValueReference != null)
                    return _valueStorage.GetRawDataLength(node.ValueReference);
            }

            return 0;
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
            var binaryKey = _keySerializer.Serialize(key);
            bool isFullMatch;
            IRadixTreeNode node = FindMostSuitableNode(binaryKey, out _, out _, out isFullMatch);

            if (!isFullMatch) return null;

            return node.ValueReference != null ? _valueStorage.GetRawDataSegment(node.ValueReference, startIndex, endIndex) : null;
        }

        /// <summary>
        /// Checks the tree for consistency.
        /// </summary>
        /// <param name="message">Diagnostic message describing the specific inconsistencies</param>
        /// <returns>True if the tree is consisternt, false otherwise</returns>
        public bool CheckConsistency(out string message)
        {
            var rootNode = _nodeStorage.FetchRoot();
            if (rootNode == null)
            {
                message = "Root node not found";
                return false;
            }

            return CheckNode(rootNode, out message);
        }

        private bool CheckNode(IRadixTreeNode node, out string message)
        {
            message = string.Empty;

            if (node.ParentNodeReference != null)
            {
                if(!node.Entries.Any() && node.ValueReference == null)
                {
                    message = $"Node: {node.Reference} has no value and child nodes";
                    return false;
                }

                if (_nodeStorage.Fetch(node.ParentNodeReference) == null)
                {
                    message = $"Node: {node.Reference} has invalid reference ({node.ParentNodeReference}) to parent node.";
                    return false;
                }

            }

            foreach (var entry in node.Entries)
            {
                var childNode = _nodeStorage.Fetch(entry.Value);
                if (childNode == null)
                {
                    message = $"Invalid reference ({entry.Value}) to child in node: {node.Reference}";
                    return false;
                }

            }

            return true;
        }

        private IRadixTreeNode FindMostSuitableNode(byte[] binaryKey, out int keyOffset, out int nodePrefixOffset, out bool isFullMatch, Action<IRadixTreeNode> processNode = null)
        {
            keyOffset = 0;
            nodePrefixOffset = 0;
            var currentNode = _nodeStorage.FetchRoot();

            processNode?.Invoke(currentNode);

            var currentPrefix = currentNode.Prefix;
            while(true)
            {
                if (currentPrefix != null && currentPrefix.Length > nodePrefixOffset)
                {
                    if (keyOffset < binaryKey.Length && currentPrefix[nodePrefixOffset] == binaryKey[keyOffset])
                    {
                        nodePrefixOffset++;
                        keyOffset++;
                    }
                    else
                    {
                        isFullMatch = keyOffset == binaryKey.Length && nodePrefixOffset == currentPrefix.Length;
                        return currentNode;
                    }    
                }
                else
                {
                    if (keyOffset == binaryKey.Length)
                    {
                        isFullMatch = keyOffset == binaryKey.Length &&
                            ((currentPrefix != null && nodePrefixOffset == currentPrefix.Length) ||
                            (currentPrefix == null && nodePrefixOffset == 0));
                        return currentNode;
                    }

                    int index;
                    if (FindSuitableEntry(currentNode.Entries, binaryKey[keyOffset], out index).HasValue)
                    {
                        currentNode = _nodeStorage.Fetch(currentNode.Entries[index].Value);

                        processNode?.Invoke(currentNode);

                        currentPrefix = currentNode.Prefix;
                        nodePrefixOffset = 0;
                    }
                    else
                    {
                        isFullMatch = keyOffset == binaryKey.Length &&
                            ((currentPrefix != null && nodePrefixOffset == currentPrefix.Length) ||
                            (currentPrefix == null && nodePrefixOffset == 0));
                        return currentNode;
                    }
                }
            }
        }

        public RadixTree(INodeStorage nodeStorage, IValueStorage<TValue> valueStorage, ISerializer<TKey> keySerializer)
            : this(nodeStorage, valueStorage, keySerializer, nodeStorage.MaxPrefixLength)
        {
        }

        public RadixTree(INodeStorage nodeStorage, IValueStorage<TValue> valueStorage, ISerializer<TKey> keySerializer, int maxPrefixLength)
        {
            _nodeStorage = nodeStorage;
            _valueStorage = valueStorage;
            _keySerializer = keySerializer;
            MaxPrefixLength = maxPrefixLength;
        }
    }
}

