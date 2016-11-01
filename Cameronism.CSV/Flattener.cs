using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cameronism.Csv
{
	internal class Flattener : IFlattener
	{
		internal class FakeMemberInfo : IMemberInfo
		{
			readonly Type _Type;
			readonly string _Name;
			public FakeMemberInfo(Type type, string name = "")
			{
				_Type = type;
                _Name = name;
			}

			public string Name { get { return _Name; } }

			public Type Type { get { return _Type; } }

			public MemberInfo MemberInfo { get { return null; } }

			public IList<IMemberInfo> MemberPath { get { return null; } }
		}

        readonly object[] _EmptyObjectArray = new object[0];
		readonly IList<IMemberInfo> _Members;
		public IList<IMemberInfo> Members { get { return _Members; } }

		readonly Type _Type;
		public Type Type { get { return _Type; } }

		public Flattener(Type type, IList<IMemberInfo> members): this(type, members, null, null)
		{
		}

		public Flattener(Type type, IList<IMemberInfo> members, Type columnType, Array columns)
		{
			_Type = type;
			_Flatten = BuildFlattener.Create(type, members, columnType, columns); // BuildFlattener has its own special handling for empty member lists

			if (members == null || members.Count == 0)
			{
				members = new List<IMemberInfo> { new FakeMemberInfo(type) };
			}

			if (columns != null)
			{
				var tmp = new List<IMemberInfo>(members);
				var keyGetter = typeof(KeyValuePair<,>).MakeGenericType(new[] { typeof(string), columnType })
					.GetProperty("Key")
					.GetGetMethod();

				foreach (var kvp in columns)
				{
					var name = (string)keyGetter.Invoke(kvp, _EmptyObjectArray);
					tmp.Add(new FakeMemberInfo(columnType, name));
				}
				members = tmp;
			}

			_Members = members;
		}

        readonly Func<object, IList<object>> _Flatten;

        public IList<object> Flatten(object item)
		{
			return _Flatten(item);
		}
	}
}
