using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TestUtils
{
	public class Trie<TKey, TValue>
	{
		private TrieNode<TKey, TValue> nodes;

		public Trie()
		{
			nodes = new TrieNode<TKey, TValue>();
		}

		public void AddSequence(List<TKey> sequence, TValue value)
		{
			// Go through the tree of nodes like an n-ary tree
			TrieNode<TKey, TValue> currentNode = nodes;

			foreach (TKey key in sequence)
			{
				currentNode.AddNewNode(key);
				currentNode = currentNode.GetNextNode(key);
			}

			currentNode.SetValueAtNode(value);
		}

		public TValue GetValueOfSequence(List<TKey> sequence)
		{
			// Go through the tree of nodes like an n-ary tree
			TrieNode<TKey, TValue> currentNode = nodes;

			foreach (TKey key in sequence)
			{
				currentNode = currentNode.GetNextNode(key);

				if (currentNode == null)
				{
					return default;
				}
			}

			return currentNode.GetValueAtNode();
		}

		// This is a Trie
		private class TrieNode<TKey2, TValue2> {
			private Dictionary<TKey2, TrieNode<TKey2, TValue2>> collectionOfNodes;
			private TValue2 valueAtNode;

			public TrieNode()
			{
				collectionOfNodes = new Dictionary<TKey2, TrieNode<TKey2, TValue2>>();
				valueAtNode = default;
			}

			public TValue2 GetValueAtNode()
			{
				return valueAtNode;
			}

			public void SetValueAtNode(TValue2 value)
			{
				valueAtNode = value;
			}

			public TrieNode<TKey2, TValue2> GetNextNode(TKey2 key)
			{
				if (!collectionOfNodes.ContainsKey(key))
				{
					return null;
				}

				return collectionOfNodes[key];
			}

			public void AddNewNode(TKey2 key)
			{
				if (!collectionOfNodes.ContainsKey(key))
				{
					TrieNode<TKey2, TValue2> newNode = new TrieNode<TKey2, TValue2>();
					collectionOfNodes.Add(key, newNode);
				}
			}
		}
	}
}
