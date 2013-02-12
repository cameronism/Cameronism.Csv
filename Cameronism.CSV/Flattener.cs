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
			public FakeMemberInfo(Type type)
			{
				_Type = type;
			}

			public string Name { get { return ""; } }

			public Type Type { get { return _Type; } }

			public MemberInfo MemberInfo { get { return null; } }

			public IList<IMemberInfo> MemberPath { get { return null; } }
		}

		readonly IList<IMemberInfo> _Members;
		public IList<IMemberInfo> Members { get { return _Members; } }

		readonly Type _Type;
		public Type Type { get { return _Type; } }

		public Flattener(Type type, IList<IMemberInfo> members)
		{
			_Type = type;
			_Flatten = BuildFlattener.Create(type, members); // BuildFlattener has its own special handling for empty member lists

			if (members == null || members.Count == 0)
			{
				members = new List<IMemberInfo> { new FakeMemberInfo(type) };
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
