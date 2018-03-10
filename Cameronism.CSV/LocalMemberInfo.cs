/* Cameronism.Csv
 * Copyright © 2016 Cameronism.com.  All Rights Reserved.
 * 
 * Apache License 2.0 - http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace Cameronism.Csv
{
	internal class LocalMemberInfo : IMemberInfo
	{
		static readonly HashSet<Type> _KnownTypes = new HashSet<Type>
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

        static Func<IMemberInfo, bool> _CsvIgnore = mi => mi.MemberInfo.GetCustomAttributes(typeof(CsvIgnoreAttribute), true).Any();

		static bool AnyReturnTrue<T>(IEnumerable<Func<T, bool>> funcs, T value)
		{
			return funcs.Any(func => func(value));
		}

		static bool DefaultShouldExcludeType(Type type)
		{
			return type.IsArray && type != typeof(byte[]);
		}

		static bool DefaultShouldIncludeType(Type type)
		{
			string ns = type.Namespace ?? "";
			return type.IsEnum || ns == "System" || ns.StartsWith("System.");
		}

		static bool DataContractShouldExcludeMember(IMemberInfo mi)
		{
			return !mi.MemberInfo.GetCustomAttributes(typeof(DataMemberAttribute), true).Any();
		}

		public static IEnumerable<IMemberInfo> FindAll(
			Type type,
			IList<IMemberInfo> path = null,
			Func<Type, bool> excludeType = null,
			Func<Type, bool> includeType = null,
			Func<IMemberInfo, bool> excludeMember = null)
		{
			var shouldExcludeMember = new List<Func<IMemberInfo, bool>> { _CsvIgnore };
			var shouldExcludeType = new List<Func<Type, bool>> { DefaultShouldExcludeType };
			var shouldIncludeType = new List<Func<Type, bool>> { DefaultShouldIncludeType };

			if (excludeType != null)
			{
				shouldExcludeType.Add(excludeType);
			}
			if (includeType != null)
			{
				shouldIncludeType.Add(includeType);
			}

			// bail out if the type should be included directly
			if (AnyReturnTrue(shouldIncludeType, type))
			{
				yield break;
			}


			if (excludeMember != null)
			{
				shouldExcludeMember.Add(excludeMember);
			}

			bool hasDataContract = type.GetCustomAttributes(typeof(DataContractAttribute), true).Any();
			if (hasDataContract)
			{
				shouldExcludeMember.Add(DataContractShouldExcludeMember);
			}

			var members = 
					type.GetFields(BindingFlags.Public | BindingFlags.Instance)
						.Where(fi => 
							fi.FieldType != type && 
							!AnyReturnTrue(shouldExcludeType, fi.FieldType))
						.Select(fi => new LocalMemberInfo(fi, path))
				.Concat(
					type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
						.Where(pi => 
							pi.PropertyType != type && 
							pi.CanRead && 
							!pi.GetIndexParameters().Any() && 
							!AnyReturnTrue(shouldExcludeType, pi.PropertyType))
						.Select(pi => new LocalMemberInfo(pi, path))
				)
				.Where(mi => !AnyReturnTrue(shouldExcludeMember, mi))
				.ToList();

			if (hasDataContract)
			{
				members = (
					from member in members
					let attr = member.MemberInfo.GetCustomAttributes(typeof(DataMemberAttribute), true).Cast<DataMemberAttribute>().FirstOrDefault()
					let order = attr == null || attr.Order < 0 ? int.MaxValue : attr.Order
					orderby order
					select member).ToList();
			}

			foreach (var member in members)
			{
				if (AnyReturnTrue(shouldIncludeType, member.Type))
				{
					yield return member;
					continue;
				}

				List<IMemberInfo> localPath;
				if (path == null)
				{
					localPath = new List<IMemberInfo>();
				}
				else
				{
					localPath = new List<IMemberInfo>(path);
				}

				localPath.Add(member);
				foreach (var innerMember in FindAll(member.Type, localPath, excludeType, includeType, excludeMember))
				{
					yield return innerMember;
				}
			}
		}

		public LocalMemberInfo(PropertyInfo prop, IList<IMemberInfo> path)
		{
			_Name = prop.Name;
			_Type = prop.PropertyType;
			_MemberInfo = prop;
			_MemberPath = path;
		}

		public LocalMemberInfo(FieldInfo field, IList<IMemberInfo> path)
		{
			_Name = field.Name;
			_Type = field.FieldType;
			_MemberInfo = field;
			_MemberPath = path;
		}

		public LocalMemberInfo(MethodInfo mi)
		{
			_Name = mi.Name;
			_Type = mi.ReturnType;
		}

		readonly string _Name;
		public string Name { get { return _Name; } }

		readonly Type _Type;
		public Type Type { get { return _Type; } }

		readonly MemberInfo _MemberInfo;
		public MemberInfo MemberInfo { get { return _MemberInfo; } }

		readonly IList<IMemberInfo> _MemberPath;
		public IList<IMemberInfo> MemberPath { get { return _MemberPath; } }
	}
}
