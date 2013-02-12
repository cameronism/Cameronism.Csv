﻿/* Cameronism.Csv
 * Copyright © 2013 Cameronism.com.  All Rights Reserved.
 * 
 * Apache License 2.0 - http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Cameronism.Csv
{
	internal class BuildFlattener
	{
		class MemberInfoComparer : EqualityComparer<IMemberInfo>
		{
			public override bool Equals(IMemberInfo x, IMemberInfo y)
			{
				if (x == null || y == null)
				{
					return x == null && y == null;
				}

				return 
					Object.Equals(x.MemberInfo, y.MemberInfo) &&
					(
						(x.MemberPath == null && y.MemberPath == null) ||
						Enumerable.SequenceEqual(x.MemberPath, y.MemberPath, this)
					);
			}

			public override int GetHashCode(IMemberInfo obj)
			{
				int hash = 17;

				var mi = obj.MemberInfo;
				if (mi != null) hash = hash * 23 + mi.GetHashCode();

				return hash;
			}
		}

		static readonly Func<object, IList<object>> _SingleValueDelegate = val => new[] { val };

		public static Func<object, IList<object>> Create(Type type, IList<IMemberInfo> members)
		{
			if (IsNullOrEmpty(members))
			{
				return _SingleValueDelegate;
			}

			var expression = CreateExpression(type, members);
			return expression.Compile();
		}

		public static Expression<Func<object, IList<object>>> CreateExpression(Type type, IList<IMemberInfo> members)
		{
			if (IsNullOrEmpty(members))
			{
				throw new NotSupportedException("Will not build expression for type with no members.");
			}

			var builder = new BuildFlattener
			{
				_Type = type,
				_Members = members,
			};
			return builder.CreateDelegate();
		}

		public static Action<IEnumerable, TextWriter> CreateWriter(Type type, IList<IMemberInfo> members, char separator)
		{
			var expression = CreateWriterExpression(type, members, separator);
			return expression.Compile();
		}

		public static Expression<Action<IEnumerable, TextWriter>> CreateWriterExpression(Type type, IList<IMemberInfo> members, char separator)
		{
			var builder = new BuildFlattener
			{
				_Type = type,
				_Members = members,
				_Separator = separator,
			};
			return builder.CreateWriterDelegate();
		}

		private Type _Type;
		private IList<IMemberInfo> _Members;

		private BuildFlattener()
		{
		}

		static bool IsNullOrEmpty<T>(ICollection<T> items)
		{
			return items == null || items.Count == 0;
		}

		static bool IsNullable(Type type)
		{
			return type.IsClass || Nullable.GetUnderlyingType(type) != null;
		}

		private static readonly IList<IMemberInfo> _EmptyMemberInfo = new List<IMemberInfo>();

		private ParameterExpression _ResultEx;
		private List<ParameterExpression> _Variables;

		Expression<Func<object, IList<object>>> CreateDelegate()
		{
			var body = new List<Expression>();

			var paramEx = Expression.Parameter(typeof(object), "param");
			var valueEx = Expression.Variable(_Type, "value");
			_ResultEx = Expression.Variable(typeof(object[]), "result");
			_Variables = new List<ParameterExpression> { valueEx, _ResultEx };


			int memberIndex = 0;
			var memberTree = 
				Tree<IMemberInfo, KeyValuePair<int, IMemberInfo>>.Create(
					_Members.Select(m => new KeyValuePair<int, IMemberInfo>(memberIndex++, m)).ToList(),
					kvp => kvp.Value.MemberPath ?? _EmptyMemberInfo,
					new MemberInfoComparer());


			// value = ({_Type})param;
			body.Add(
				Expression.Assign(
					valueEx,
					Expression.Convert(
						paramEx,
						_Type)));

			// result = new object[{memberIndex}];
			body.Add(
				Expression.Assign(
					_ResultEx,
					Expression.New(
						typeof(object[]).GetConstructor(new[] { typeof(int) }),
						Expression.Constant(memberIndex, typeof(int)))));

			GetMemberValues(memberTree, valueEx, body, MemberToResult);

			// return (IList<object>)result;
			body.Add(
				Expression.Convert(
				_ResultEx,
				typeof(IList<object>)));

			return Expression.Lambda<Func<object, IList<object>>>(
				Expression.Block(
					typeof(IList<object>),
					_Variables,
					body),
				paramEx);
		}

		void MemberToResult(List<Expression> block, IMemberInfo member, int memberIndex, Expression value)
		{
			if (member.Type != typeof(object))
			{
				value = Expression.Convert(value, typeof(object));
			}

			block.Add(
				Expression.Assign(
					Expression.ArrayAccess(
						_ResultEx,
						Expression.Constant(memberIndex)),
					value));
		}

		void GetMemberValues(Tree<IMemberInfo, KeyValuePair<int, IMemberInfo>> leaf, Expression value, List<Expression> block, Action<List<Expression>, IMemberInfo, int, Expression> writeValue, Func<Tree<IMemberInfo, KeyValuePair<int, IMemberInfo>>, Expression> onNodeNull = null)
		{
			if (leaf.HasValue)
			{
				var member = leaf.Value.Value;
				int memberIndex = leaf.Value.Key;

				Expression rhs = String.IsNullOrEmpty(member.Name) ?
					value : // this should only happen for single type and no (real) members
					Expression.PropertyOrField(value, member.Name);
				writeValue(block, member, memberIndex, rhs);
			}
			else
			{
				var member = leaf.Path.LastOrDefault();
				Expression variable;

				if (member != null)
				{
					variable = GetTemporary(member);
					block.Add(
						Expression.Assign(
							variable,
							Expression.PropertyOrField(value, member.Name)));

				}
				else
				{
					// we should only hit this for the top level
					variable = value;
				}

				var innerBlock = block;
				if (IsNullable(variable.Type))
				{
					innerBlock = new List<Expression>();
				}

				foreach (var child in leaf.Children)
				{
					GetMemberValues(child, variable, innerBlock, writeValue, onNodeNull);
				}

				if (IsNullable(variable.Type))
				{
					Expression condition = Expression.NotEqual(variable, Expression.Constant(null, variable.Type));
					Expression ifTrue = Expression.Block(innerBlock);
					Expression ifFalse = null;

					if (onNodeNull != null)
					{
						ifFalse = onNodeNull(leaf);
					}

					block.Add(
						ifFalse == null ?
						Expression.IfThen(condition, ifTrue) :
						Expression.IfThenElse(condition, ifTrue, ifFalse));
				}
			}
		}

		private char _Separator;
		private ParameterExpression _TextWriterEx;
		private ParameterExpression _TmpStringEx;
		private ParameterExpression _CharsToEscape; // new[] { ',', '"', '\r', '\n' }
		private ParameterExpression _FormatProvider;

		readonly string _DateTimeFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

		static readonly MethodInfo _WriteChar = typeof(TextWriter).GetMethod("Write", new[] { typeof(char) });
		static readonly MethodInfo _WriteString = typeof(TextWriter).GetMethod("Write", new[] { typeof(string) });
		static readonly MethodInfo _ObjectToString = typeof(Object).GetMethod("ToString", Type.EmptyTypes);
		static readonly MethodInfo _StringConcat3 = typeof(String).GetMethod("Concat", new[] { typeof(string), typeof(string), typeof(string), });
		static readonly MethodInfo _StringReplace = typeof(string).GetMethod("Replace", new[] { typeof(string), typeof(string) });
		static readonly MethodInfo _IndexOfAny = typeof(string).GetMethod("IndexOfAny", new[] { typeof(char[]) });

		// not sure of a better way to find a generic method
		static readonly MethodInfo _IndexOfChar = typeof(Array).GetMethods()
			.FirstOrDefault(mi => mi.Name == "IndexOf" && mi.IsGenericMethod && mi.GetParameters().Length == 2)
			.MakeGenericMethod(typeof(char));

		Expression<Action<IEnumerable, TextWriter>> CreateWriterDelegate()
		{
			var untypedEnumerable = Expression.Parameter(typeof(IEnumerable), "enumerableParam");
			_TextWriterEx = Expression.Parameter(typeof(TextWriter), "textWriter");

			//Type enumerableType = typeof(IEnumerable<>).MakeGenericType(_Type);
			//var enumerableEx = Expression.Variable(enumerableType, "enumerable");
			var enumeratorEx = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(_Type), "enumerator");
			var itemEx = Expression.Variable(_Type, "item");

			_TmpStringEx = Expression.Variable(typeof(string), "tmpString");
			_CharsToEscape = Expression.Variable(typeof(char[]), "charsToEscape");
			_FormatProvider = Expression.Variable(typeof(IFormatProvider), "formatProvider");

			var body = new List<Expression>();
			var loop = new List<Expression>();

			_Variables = new List<ParameterExpression> 
			{
				//enumerableEx,
				enumeratorEx,
				itemEx,
				_TmpStringEx, 
				_CharsToEscape, 
				_FormatProvider,
			};

			int memberIndex = 0;
			var memberTree = 
				Tree<IMemberInfo, KeyValuePair<int, IMemberInfo>>.Create(
					_Members.Select(m => new KeyValuePair<int, IMemberInfo>(memberIndex++, m)).ToList(),
					kvp => kvp.Value.MemberPath ?? _EmptyMemberInfo,
					new MemberInfoComparer());

			if (memberIndex == 0)
			{
				memberTree = Tree<IMemberInfo, KeyValuePair<int, IMemberInfo>>.CreateSingleton(
					new KeyValuePair<int,IMemberInfo>(
						0, 
						new Flattener.FakeMemberInfo(_Type)));
			}

			// if (enumerableParam == null) throw new ArgumentNullException("enumerableParam");
			body.Add(
				Expression.IfThen(
					Expression.Equal(
						untypedEnumerable,
						Expression.Constant(null, typeof(IEnumerable))),
					Expression.Throw(
						Expression.New(
							typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) }), 
							Expression.Constant("enumerableParam", typeof(string))))));


			// formatProvider = (IFormatProvider)CultureInfo.InvariantCulture;
			body.Add(
				Expression.Assign(
					_FormatProvider,
					Expression.Convert(
						Expression.Property(
							null,
							typeof(System.Globalization.CultureInfo).GetProperty("InvariantCulture")),
						typeof(IFormatProvider))));

			// charsToEscape = new[] { ',', '"', '\r', '\n' }
			body.Add(
				Expression.Assign(
					_CharsToEscape,
					Expression.NewArrayInit(
						typeof(char),
						Expression.Constant(_Separator),
						Expression.Constant('"'),
						Expression.Constant('\n'),
						Expression.Constant('\r'))));

			// writer.WriteLine({headings});
			body.Add(
				Expression.Call(
					_TextWriterEx,
					typeof(TextWriter).GetMethod("WriteLine", new[] { typeof(string) }),
					Expression.Constant(GetHeadingsLine(_Members, _Separator), typeof(string))));

			// enumerator = ((IEnumerable<>)enumerable).GetEnumerator();
			body.Add(
				Expression.Assign(	
					enumeratorEx,
					Expression.Call(
						Expression.Convert(untypedEnumerable, typeof(IEnumerable<>).MakeGenericType(_Type)),
						"GetEnumerator",
						null)));

			#region loop

			// item = enumerator.Current;
			loop.Add(
				Expression.Assign(
					itemEx,
					Expression.Property(
						enumeratorEx,
						"Current")));

			GetMemberValues(memberTree, itemEx, loop, MemberToTextWriter, OnWriterNodeNull);

			// writer.WriteLine();
			loop.Add(
				Expression.Call(
					_TextWriterEx,
					"WriteLine",
					null));


			// while (enumerator.MoveNext()) {body}
			var breakLabel = Expression.Label();
			var whileLoop = 
				Expression.Loop(
					Expression.IfThenElse(
						Expression.Call(
							enumeratorEx,
							typeof(IEnumerator).GetMethod("MoveNext")),
						Expression.Block(loop),
						Expression.Break(breakLabel)),
					breakLabel);

			#endregion

			// try {loop} finally enumerator.Dispose()
			body.Add(
				Expression.TryFinally(
					whileLoop,
					Expression.Call(
						enumeratorEx,
						typeof(IDisposable).GetMethod("Dispose"))));


			// writer.Flush();
			body.Add(
				Expression.Call(
					_TextWriterEx,
					"Flush",
					null));

			return Expression.Lambda<Action<IEnumerable, TextWriter>>(
				Expression.Block(_Variables, body),
				untypedEnumerable, _TextWriterEx);
		}

		static string GetHeadingsLine(IList<IMemberInfo> _Members, char _Separator)
		{
			return String.Join(_Separator.ToString(), _Members.Select(m => m.Name));
		}

		static IEnumerable<KeyValuePair<int, IMemberInfo>> Flatten(Tree<IMemberInfo, KeyValuePair<int, IMemberInfo>> node)
		{
			if (node.HasValue)
			{
				yield return node.Value;
			}
			else
			{
				foreach (var child in node.Children.SelectMany(Flatten))
				{
					yield return child;
				}
			}
		}

		/// <summary>
		/// Write out the appropriate number of separators since a block is being skipped as null
		/// </summary>
		/// <param name="skippedNode"></param>
		/// <returns></returns>
		Expression OnWriterNodeNull(Tree<IMemberInfo, KeyValuePair<int, IMemberInfo>> skippedNode)
		{
			var members = Flatten(skippedNode).ToList();
			var minIndex = members.Min(kvp => kvp.Key);
			int separatorCount = members.Count;

			// TODO test this
			if (minIndex == 0)
			{
				separatorCount--;
			}

			return Expression.Call(
				_TextWriterEx,
				_WriteString,
				Expression.Constant(
					String.Concat(Enumerable.Repeat(_Separator, separatorCount))));
		}

		void MemberToTextWriter(List<Expression> block, IMemberInfo member, int memberIndex, Expression value)
		{
			if (memberIndex != 0)
			{
				// writer.Write(',');
				block.Add(
					Expression.Call(
						_TextWriterEx,
						_WriteChar,
						Expression.Constant(_Separator)));
			}

			bool needsEscapeCheck;
			Expression tempVar = null;

			if (IsNullable(member.Type))
			{
				tempVar = GetTemporary(member);

				// tmp = value;
				block.Add(
					Expression.Assign(
						tempVar,
						value));

				Expression rhs;
				Type underlyingNullable = Nullable.GetUnderlyingType(member.Type);
				if (underlyingNullable != null)
				{
					// tmp.HasValue ? tmp.GetValueOrDefault().ToString(...) : "";
					rhs = Expression.Condition(
						Expression.Property(tempVar, "HasValue"), 
						ValueToString(
							underlyingNullable,
							Expression.Call(tempVar, "GetValueOrDefault", null),
							out needsEscapeCheck),
						Expression.Constant(""));
				}
				else
				{
					// tmp == null ? "" : tmp.ToString(...);
					rhs = Expression.Condition(
						Expression.Equal(
							tempVar,
							Expression.Constant(null, member.Type)),
						Expression.Constant(""),
						ValueToString(member.Type, tempVar, out needsEscapeCheck));
				}


				// tmpString = {rhs};
				block.Add(Expression.Assign(_TmpStringEx, rhs));
			}
			else
			{
				// tmpString = tmp.ToString(...);
				block.Add(
					Expression.Assign(
						_TmpStringEx,
						ValueToString(member.Type, value, out needsEscapeCheck)));
			}

			if (needsEscapeCheck)
			{
				Expression condition = 
					Expression.NotEqual(
						Expression.Call(
							_TmpStringEx,
							_IndexOfAny,
							_CharsToEscape),
						Expression.Constant(-1, typeof(int)));

				if (IsNullable(member.Type))
				{
					condition = Expression.AndAlso(
						Expression.NotEqual(
							tempVar,
							Expression.Constant(null, member.Type)),
						condition);
				}

				// if (tmpString.IndexOfAny(charsToEscape) != -1)
				//   tmpString = String.Concat("\"", tmpString.Replace("\"", "\"\""), "\"");
				block.Add(
					Expression.IfThen(
						condition,
						Expression.Assign(
							_TmpStringEx,
							Expression.Call(
								_StringConcat3,
								Expression.Constant("\""),
								Expression.Call(
									_TmpStringEx, 
									_StringReplace, 
									Expression.Constant("\""),
									Expression.Constant("\"\"")),
								Expression.Constant("\"")))));
			}

			// writer.Write(tmpString);
			block.Add(
				Expression.Call(
					_TextWriterEx,
					_WriteString,
					_TmpStringEx));
		}

		Expression ValueToString(Type type, Expression value, out bool needsEscapeCheck)
		{
			needsEscapeCheck = false;

			var typeCode = Type.GetTypeCode(type);
			if (typeCode == TypeCode.Object)
			{
				if (type == typeof(DateTimeOffset))
				{
					typeCode = TypeCode.DateTime;
				}
			}

			switch (typeCode)
			{
				case TypeCode.Boolean:
				{
					// boolean ? "TRUE" : "FALSE"
					return Expression.Condition(
						value,
						Expression.Constant("TRUE"),
						Expression.Constant("FALSE"));
				}
				case TypeCode.Char:
				{
					// Array.IndexOf(charsToEscape, character) == -1 ? 
					//   character.ToString() :
					//   character == '"' ? 
					//     "\"\"\"\"" :
					//     String.Concat("\"", character.ToString(), "\"") 
					return Expression.Condition(
						Expression.Equal(
							Expression.Call(_IndexOfChar, _CharsToEscape, value),
							Expression.Constant(-1, typeof(int))),
						Expression.Call(value, _ObjectToString),
						Expression.Condition(
							Expression.Equal(value, Expression.Constant('"')),
							Expression.Constant("\"\"\"\""),
							Expression.Call(
								_StringConcat3,
								Expression.Constant("\""),
								Expression.Call(value, _ObjectToString),
								Expression.Constant("\""))));
				}
				case TypeCode.DateTime:
				{
					var method = type.GetMethod("ToString", new[] { typeof(string), typeof(IFormatProvider) });
					// {date}.ToString(someFormat, someCulture)
					return Expression.Call(
						value,
						method,
						Expression.Constant(_DateTimeFormat),
						_FormatProvider);
				}
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
				{
					var method = type.GetMethod("ToString", new[] { typeof(string), typeof(IFormatProvider) });
					// {number}.ToString("g", someCulture)
					return Expression.Call(
						value,
						method,
						Expression.Constant("g"),
						_FormatProvider);
				}
				case TypeCode.Object:
				default:
				{
					needsEscapeCheck = true;
					return Expression.Call(value, _ObjectToString);
				}
				case TypeCode.String:
				{
					needsEscapeCheck = true;
					return value;
				}
			}
		}

		private int _TemporaryIndex = 0;
		// possible future enhancement: reuse variables of the same type 'after' they're done being used
		ParameterExpression GetTemporary(IMemberInfo member)
		{
			var name = "t" + (_TemporaryIndex++) + String.Concat(member.Name.Take(10));
			var variable = Expression.Variable(member.Type, name);
			_Variables.Add(variable);
			return variable;
		}
	}
}
