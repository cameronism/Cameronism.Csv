using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Cameronism.Csv.Tests
{
	[TestFixture]
	public class DynamicColumns
	{
		static void Approve<TItem, TColumn>(IEnumerable<TItem> enumerable, IEnumerable<KeyValuePair<string, TColumn>> columns, bool approve = true)
		{
			var writer = new StringWriter { NewLine = "\r\n" };
			Serializer.Serialize(writer, enumerable, columns.ToArray());

			if (approve) ApprovalTests.Approvals.Approve(writer);
		}

		[Test]
		public void AnonymousType()
		{
			Assert.Throws(typeof(ArgumentException), () =>
			{
				Approve(
					Enumerable.Repeat(new { Hello = "World" }, 1),
					new Dictionary<string, int>(),
					approve: false);
			});
		}

		#region thing
		class Thing<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
		{
			public Thing(string stuff)
			{
				Stuff = stuff;
			}

			public void Add(TKey key, TValue value)
			{
				_Things[key] = value;
			}

			public TValue this[TKey key]
			{
				get
				{
					return _Things[key];
				}
			}

			public string Stuff { get; set; }

			private Dictionary<TKey, TValue> _Things = new Dictionary<TKey,TValue>();

			public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
			{
				return _Things.GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return ((System.Collections.IEnumerable)_Things).GetEnumerator();
			}
		}
		#endregion


		[Test]
		public void Thing1()
		{
			Approve(
				new[] {
						new Thing<int, int>("t1")
						{
							{ 1, 1 },
							{ 2, 2 },
						},
						new Thing<int, int>("t2")
						{
							{ 1, 10 },
							{ 2, 20 },
						},
					},
				new Dictionary<string, int>
				{
					{ "one", 1 },
					{ "two", 2 },
				});
		}
	}
}
