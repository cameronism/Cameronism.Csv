/* Cameronism.Csv
 * Copyright © 2014 Cameronism.com.  All Rights Reserved.
 * 
 * Apache License 2.0 - http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Serialization;

namespace Cameronism.Csv
{
	public static class Serializer
	{
		#region TypePair
		struct TypePair
		{
			public TypePair(Type a, Type b)
			{
				_A = a;
				_B = b;
			}

			private Type _A;
			private Type _B;

			public Type Item1 { get { return _A; } }
			public Type Item2 { get { return _B; } }

			public override bool Equals(object obj)
			{
				var that = (TypePair)obj;
				return that._A == this._A && that._B == this._B;
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hash = 27;
					if (_A != null) hash = (13 * hash) + _A.GetHashCode();
					if (_B != null) hash = (13 * hash) + _B.GetHashCode();
					return hash;
				}
			}
		}
		#endregion

		static Dictionary<Type, Action<IEnumerable, TextWriter>> _Delegates = new Dictionary<Type, Action<IEnumerable, TextWriter>>();
		static Dictionary<TypePair, Action<IEnumerable, Array, TextWriter>> _DelegatesDynamic = new Dictionary<TypePair, Action<IEnumerable, Array, TextWriter>>();
		public static void Serialize<T>(Stream destination, IEnumerable<T> items)
		{
			Serialize(new StreamWriter(destination), items);
		}

		public static void Serialize<T>(TextWriter destination, IEnumerable<T> items)
		{
			Serialize(typeof(T), destination, items);
		}

		public static void Serialize<TItem, TColumn>(Stream destination, IEnumerable<TItem> items, KeyValuePair<string, TColumn>[] columns)
		{
			Serialize(new StreamWriter(destination), items, columns);
		}

		public static void Serialize<TItem, TColumn>(TextWriter destination, IEnumerable<TItem> items, KeyValuePair<string, TColumn>[] columns)
		{
			Action<IEnumerable, Array, TextWriter> writer;
			var pair = new TypePair(typeof(TItem), typeof(TColumn));
			lock (_Delegates)
			{
				if (!_DelegatesDynamic.TryGetValue(pair, out writer))
				{
					var members = LocalMemberInfo.FindAll(typeof(TItem)).ToList();
					var expression = BuildFlattener.CreateWriterExpression(typeof(TItem), typeof(TColumn), members, ',');
					writer = expression.Compile();
					_DelegatesDynamic.Add(pair, writer);
				}
			}

			writer.Invoke(items, columns, destination);
		}

		public static void Serialize(Type type, TextWriter destination, IEnumerable items)
		{
			Action<IEnumerable, TextWriter> writer;
			lock (_Delegates)
			{
				if (!_Delegates.TryGetValue(type, out writer))
				{
					var members = LocalMemberInfo.FindAll(type).ToList();
					var expression = BuildFlattener.CreateWriterExpression(type, members, ',');
					writer = expression.Compile();
					_Delegates.Add(type, writer);
				}
			}

			writer.Invoke(items, destination);
		}

		public static Expression<Action<IEnumerable, TextWriter>> CreateExpression(Type type, string separator = ",")
		{
			var members = LocalMemberInfo.FindAll(type).ToList();
			var expression = BuildFlattener.CreateWriterExpression(type, members, ',');
			return expression;
		}

		public static IEnumerable<IMemberInfo> FindAllMembers(Type type)
		{
			return LocalMemberInfo.FindAll(type);
		}

		public static IFlattener CreateFlattener(Type type)
		{
			var members = LocalMemberInfo.FindAll(type).ToList();
			return new Flattener(type, members);
		}
	}
}
