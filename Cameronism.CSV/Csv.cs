/* Cameronism.Csv
 * Copyright © 2012 Cameronism.com.  All Rights Reserved.
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
			Action<IEnumerable, TextWriter> writer;
			lock (_Delegates)
			{
				if (!_Delegates.TryGetValue(typeof(T), out writer))
				{
					writer = Builder.BuildWriter(typeof(T), ",").Compile();
					_Delegates.Add(typeof(T), writer);
				}
			}

			writer.Invoke(items, destination);
		}

		public static Expression<Action<IEnumerable, TextWriter>> CreateExpression(Type type, string separator = ",")
		{
			return Builder.BuildWriter(type, separator);
		}
	}

	class Builder
	{
		Builder() {	}
		
		public static Expression<Action<IEnumerable, TextWriter>> BuildWriter(Type type, string separator)
		{
			return new Builder().BuildInternal(type, separator[0]);
		}

		static Type[] _SingleString = { typeof(string) };

		static MethodInfo _WriteLineString =     typeof(TextWriter).GetMethod("WriteLine",     _SingleString);
		static MethodInfo _WriteString =         typeof(TextWriter).GetMethod("Write",         _SingleString);
		static MethodInfo _WriteLineTerminator = typeof(TextWriter).GetMethod("WriteLine",     Type.EmptyTypes);
		static MethodInfo _ObjectToString =      typeof(Object)    .GetMethod("ToString",      Type.EmptyTypes);
		static MethodInfo _StringIsNullOrEmpty = typeof(String)    .GetMethod("IsNullOrEmpty", _SingleString);
		static MethodInfo _StringConcat =        typeof(String)    .GetMethod("Concat",        new[] { typeof(string), typeof(string), typeof(string) });
		static MethodInfo _IndexOfAnyChar =      typeof(String)    .GetMethod("IndexOfAny",    new[] { typeof(char[]) });
		static MethodInfo _ReplaceString =       typeof(String)    .GetMethod("Replace",       new[] { typeof(string), typeof(string) });
		
		ParameterExpression writerEx;
		ParameterExpression itemEx;
		ParameterExpression tmpStringEx;
		List<Expression> body;
		List<Expression> notNullBlock;
		ParameterExpression _CharsToEscapeEx = Expression.Variable(typeof(char[]), "charsToEscape");
		const string DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss.ffffff";

		
		Expression<Action<IEnumerable, TextWriter>> BuildInternal(Type type, char separator)
		{
			var columns = GetColumns(type).ToList();
			
			List<Expression> statements = new List<Expression>();
			List<ParameterExpression> variables = new List<ParameterExpression>();
			var untypedEnumerable = Expression.Parameter(typeof(IEnumerable), "enumerableParam");
			writerEx = Expression.Parameter(typeof(TextWriter), "writer");
			
			Type enumerableType = typeof(IEnumerable<>).MakeGenericType(type);
			var separatorEx = Expression.Variable(typeof(string), "seperator");
			var enumerableEx = Expression.Variable(enumerableType, "enumerable");
			var enumeratorEx = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(type), "enumerator");
			itemEx = Expression.Variable(type, "item");
			tmpStringEx = Expression.Variable(typeof(string), "tmpString");
			
			variables.AddRange(new[] { separatorEx, enumerableEx, itemEx, enumeratorEx, tmpStringEx, _CharsToEscapeEx });
			
			// separator = {separator};
			statements.Add(	
				Expression.Assign(
					separatorEx,
					Expression.Constant(separator.ToString(), typeof(string))));

			// charsToEscape = new[] { separator, '\n', '\r', '"' };  /* this variable is not always used */
			statements.Add(
				Expression.Assign(
					_CharsToEscapeEx,
					Expression.NewArrayInit(
						typeof(char), 
						Expression.Constant(separator), 
						Expression.Constant('\n'), 
						Expression.Constant('\r'), 
						Expression.Constant('"'))));
			
			// enumerable = (IEnumerable<{type})enumerableParam;
			statements.Add(
				Expression.Assign(
					enumerableEx,
					Expression.Convert(untypedEnumerable, enumerableType)));
					
			// enumerator = enumerable.GetEnumerator();
			statements.Add(
				Expression.Assign(	
					enumeratorEx,
					Expression.Call(
						enumerableEx,
						"GetEnumerator",
						null)));
					
			// writer.WriteLine({headings});
			statements.Add(
				Expression.Call(
					writerEx,
					_WriteLineString,
					Expression.Constant(GetHeadingsLine(columns, separator.ToString()), typeof(string))));
			
			
			bool nullCheck = type.IsClass || Nullable.GetUnderlyingType(type) != null;
			body = new List<Expression>();
			if (nullCheck)
			{
				notNullBlock = new List<Expression>();
			}
			else
			{
				notNullBlock = body;
			}
			/* BEGIN LOOP BODY */
			
			// item = enumerator.Current;
			body.Add(
				Expression.Assign(
					itemEx,
					Expression.Property(
						enumeratorEx,
						"Current")));
			
			/* BEGIN ITEM NOT NULL BLOCK */
			for (int i = 0; i < columns.Count; i++)
			{
				if (i > 0)
				{
					// writer.Write({separator});
					notNullBlock.Add(
						Expression.Call(
							writerEx,
							_WriteString,
							separatorEx));
				}
			
				AddValueWriter(columns[i]);
			}
			
			// writer.WriteLine();
			notNullBlock.Add(
				Expression.Call(
					writerEx,
					_WriteLineTerminator));
			/* END ITEM NOT NULL BLOCK */
			
			if (nullCheck)
			{
			    // write loop body inside an if block
			    //if (item == null)
			    //    writer.Write({the correct number of commas});
			    //else
			    //    {write values}
			    body.Add(
			        Expression.IfThenElse(
			            Expression.Equal(itemEx, Expression.Constant(null, type)),
			            Expression.Call(
			                writerEx,
			                _WriteLineString,
			                Expression.Constant(String.Concat(Enumerable.Repeat(separator, columns.Count - 1)))),
			            Expression.Block(notNullBlock)));
			}

			/* END LOOP BODY */
			
			// while (enumerator.MoveNext()) {body}
			var breakLabel = Expression.Label();
			statements.Add(
				Expression.Loop(
					Expression.IfThenElse(
						Expression.Call(
							enumeratorEx,
							typeof(IEnumerator).GetMethod("MoveNext")),
						Expression.Block(body),
						Expression.Break(breakLabel)),
					breakLabel));

			// writer.Flush();
			statements.Add(
				Expression.Call(
					writerEx,
					"Flush",
					null));
			
			return Expression.Lambda<Action<IEnumerable, TextWriter>>(
				Expression.Block(variables, statements),
				"WriteSeparated_" + Regex.Replace(type.FullName, @"[^\w]+", "_"), 
				new[] { untypedEnumerable, writerEx });
		}
		
		void AddValueWriter(Column column)
		{
			Expression value = itemEx;
			List<BinaryExpression> valueTests = new List<BinaryExpression>();
			List<Expression> successBlock = new List<Expression>();
			
			if (column.MemberPath != null)
			{
				foreach (var member in column.MemberPath)
				{
					value = Expression.PropertyOrField(
						value,
						member.Name);

					if (member.Type.IsClass || Nullable.GetUnderlyingType(member.Type) != null)
					{
						// {value} != null
						valueTests.Add(Expression.NotEqual(
									value,
									Expression.Constant(null, member.Type)));
					}
				}
			}

			// get the expression that will ultimately create the value string
			Tuple<MethodInfo, Expression[]> toStringArgs = GetToStringArgs(column.Type);
			Expression valueToString = Expression.Call(
				value,
				toStringArgs.Item1,
				toStringArgs.Item2);

			// tmpString = item{...};
			successBlock.Add(
				Expression.Assign(tmpStringEx, valueToString));

			bool needsEscapeCheck = 
				// Strings always need to be checked
				column.Type == typeof(string) || 
				column.Type == typeof(char) || 
				// All known types are currently safe except for string and char
				!_KnownTypes.Contains(column.Type);


			if (needsEscapeCheck)
			{
				// !String.IsNullOrEmpty(tmpString) && tmpString.IndexOfAny(new char[] { ... }) != -1
				Expression needsEscape = 
					Expression.AndAlso(
						Expression.Not(
							Expression.Call(_StringIsNullOrEmpty, tmpStringEx)),
						Expression.NotEqual(
							Expression.Call(
								tmpStringEx,
								_IndexOfAnyChar,
								_CharsToEscapeEx),
							Expression.Constant(-1, typeof(int))));

				// String.Concat("\"", tmpString.Replace("\"", "\"\""), "\"")
				Expression escapeString =
					Expression.Call(
						_StringConcat,
						Expression.Constant("\"", typeof(string)),
						Expression.Call(
							tmpStringEx,
							_ReplaceString,
							Expression.Constant("\""),
							Expression.Constant("\"\"")
						),
						Expression.Constant("\"", typeof(string)));

				// if ({needsEscape})
				//     tmpString = {escapeString};
				successBlock.Add(
					Expression.IfThen(
						needsEscape,
						Expression.Assign(tmpStringEx, escapeString)));
			}
			
			// writer.Write({tmpString});
			successBlock.Add(
				Expression.Call(
					writerEx,
					_WriteString,
					tmpStringEx));
			

			if (valueTests.Any())
			{
				// if ({valueTests}) {successBlock}
				notNullBlock.Add(
					Expression.IfThen(
						valueTests.Aggregate((t, v) => Expression.AndAlso(t, v)),
						Expression.Block(
							successBlock)));
			}
			else
			{
				notNullBlock.AddRange(successBlock);
			}
		}

		Tuple<MethodInfo, Expression[]> GetToStringArgs(Type type)
		{
			switch (type.ToString())
			{
				case "System.DateTime":
					return Tuple.Create(typeof(DateTime).GetMethod("ToString", _SingleString), new Expression[] { Expression.Constant(DATETIME_FORMAT) });
				default:
					return Tuple.Create(_ObjectToString, new Expression[0]);
			}
		}
		
		static string GetHeadingsLine(IList<Column> columns, string separator)
		{
			// FIXME titles may require escaping
			return String.Join(separator, columns.Select(c => c.Title));
		}
	
		class Column
		{
			public Type Type { get; set; }
			public string Title { get; set; }
			public Stack<Member> MemberPath { get; set; }
		}

		class Member
		{
			public Member(FieldInfo fi)
			{
				this.FieldInfo = fi;
				this.Type = fi.FieldType;
				this.Name = fi.Name;
			}

			public Member(PropertyInfo pi)
			{
				this.PropertyInfo = pi;
				this.Type = pi.PropertyType;
				this.Name = pi.Name;
			}

			public string Name { get; set; }
			public Type Type { get; set; }
			public FieldInfo FieldInfo { get; set; }
			public PropertyInfo PropertyInfo { get; set; }
		}
		
		Func<Type, bool> ShouldOmitType = type =>
		{
			string ns = type.Namespace ?? "";
			return ns == "System" || ns.StartsWith("System.") || type.IsArray;
		};
		
		IEnumerable<Column> GetColumns(Type type)
		{
		//	type.Dump("loop");
			if (type.IsEnum || _KnownTypes.Contains(type))
			{
				yield return new Column
				{
					Type = type,
				};
				yield break;
			}
			else if (ShouldOmitType(type))
			{
				yield break;
			}
			
			foreach (var fi in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				// FIXME other cyclical structures will still catch us
				if (fi.FieldType == type)
				{
					continue;
				}
				
				foreach (Column col in GetColumns(fi.FieldType))
				{
					// TODO enforce attribute presence
					if (col.MemberPath == null)
					{
						col.Title = fi.Name;
						col.MemberPath = new Stack<Member>();
					}
					col.MemberPath.Push(new Member(fi));
					yield return col;
				}
			}
			
			
			foreach (var pi in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				// FIXME other cyclical structures will still catch us
				if (!pi.CanRead || pi.GetIndexParameters().Any() || pi.PropertyType == type)
				{
					continue;
				}
				
				foreach (Column col in GetColumns(pi.PropertyType))
				{
					// TODO enforce attribute presence
					if (col.MemberPath == null)
					{
						col.Title = pi.Name;
						col.MemberPath = new Stack<Member>();
					}
					col.MemberPath.Push(new Member(pi));
					yield return col;
				}
			}
		}

		static HashSet<Type> _KnownTypes = new HashSet<Type>
		{
			typeof(DateTime),
			typeof(DateTimeOffset),
			typeof(TimeSpan),
			typeof(sbyte),
			typeof(byte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(float),
			typeof(double),
			typeof(string),
			typeof(char),
		};

		
		

		[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
		public class SummaryAttribute : Attribute
		{
			
		}
	}
}
