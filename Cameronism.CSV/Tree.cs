using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cameronism.Csv
{
	internal class Tree<TKey, TValue>
	{
		public readonly IList<TKey> Path;
		public readonly IList<Tree<TKey, TValue>> Children; // mutable
		public readonly TValue Value;
		public readonly bool HasValue;
		
		private Tree(IList<TKey> path, TValue value)
		{
			Path = path;
			Value = value;
			HasValue = true;
			
			// leave Leafs null
		}
		
		private Tree(IList<TKey> path)
		{
			Path = path;
			Children = new List<Tree<TKey, TValue>>();
		}
		
		static Tree<TKey, TValue> GetParent(Tree<TKey, TValue> root, int depth, IList<TKey> path, IEqualityComparer<TKey> keyComparer)
		{
			//new
			//{
			//	depth,
			//	path
			//}.Dump();
			
			if (path.Count - depth < 1) return root;
			
			
			if (depth > 10)
			{
				throw new Exception("Giving up now.  Lots of recursion.");
			}
			
			
			var step = path[depth];
			Tree<TKey, TValue> next = null;
			foreach (var node in root.Children)
			{
				if (node.Path.Count > depth && keyComparer.Equals(node.Path[depth], path[depth]))
				{
					next = node;
					break;
				}
			}
			
			if (next == null)
			{
				next = new Tree<TKey, TValue>(path.Take(depth + 1).ToList());
				root.Children.Add(next);
			}
			
			return GetParent(next, depth + 1, path, keyComparer);
		}
		
		public static Tree<TKey, TValue> Create(IEnumerable<TValue> items, Func<TValue, IList<TKey>> pathSelector, IEqualityComparer<TKey> keyComparer = null)
		{
			keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
			var root = new Tree<TKey, TValue>(new List<TKey>());
			
			foreach (var item in items)
			{
				var path = pathSelector(item);
				var parent = GetParent(root, 0, path, keyComparer);
				parent.Children.Add(new Tree<TKey, TValue>(path, item));
			}
			
			return root;
		}

		public static Tree<TKey, TValue> CreateSingleton(TValue value)
		{
			return new Tree<TKey, TValue>(null, value);
		}
	}
}
