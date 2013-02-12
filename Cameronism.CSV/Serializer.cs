/* Cameronism.Csv
 * Copyright © 2013 Cameronism.com.  All Rights Reserved.
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
		static Dictionary<Type, Action<IEnumerable, TextWriter>> _Delegates = new Dictionary<Type, Action<IEnumerable, TextWriter>>();
		public static void Serialize<T>(Stream destination, IEnumerable<T> items)
		{
			Serialize(new StreamWriter(destination), items);
		}

		public static void Serialize<T>(TextWriter destination, IEnumerable<T> items)
		{
			Serialize(typeof(T), destination, items);
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

		public static Func<object, IList<object>> CreateFlattener(Type type)
		{
			var members = LocalMemberInfo.FindAll(type).ToList();
			return BuildFlattener.Create(type, members);
		}
	}
}
