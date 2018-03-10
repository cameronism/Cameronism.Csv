/* Cameronism.Csv
 * Copyright © 2018 Cameronism.com.  All Rights Reserved.
 * 
 * Apache License 2.0 - http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using NUnit.Framework;
using System.Runtime.Serialization;

namespace Cameronism.Csv.Tests
{
	[TestFixture]
	public class Verify
	{
        [DataContract]
        class SomeDataTransferObject
        {
            [DataMember]
            public int Foo { get; set; }

            [DataMember, CsvIgnore]
            public int Bar { get; set; }
        }


		static void Approve<T>(IEnumerable<T> enumerable)
		{
			var writer = new StringWriter { NewLine = "\r\n" };
			var expression = Serializer.CreateExpression(typeof(T), separator: ",");
		    var action = expression.Compile();

		    action(enumerable, writer);

			ApprovalTests.Approvals.Approve(writer);
		}

		[Test]
		public void AnonymousType()
		{
			Approve(
				Enumerable.Repeat(new { Hello = "World" }, 1)
			);
		}

		[Test]
		public void RejectNullEnumerables()
		{
			var writer = new StringWriter { NewLine = "\r\n" };
			var expression = Serializer.CreateExpression(typeof(string), separator: ",");
		    var action = expression.Compile();

			Assert.Throws<ArgumentNullException>(() => action(null, writer));
		}

		[Test]
		public void Integers()
		{
			Approve(
				Enumerable.Range(0, 10)
			);
		}

		[Test]
		public void Doubles()
		{
			Approve(
				from i in Enumerable.Range(0, 10)
				select .5 * i
			);
		}

		[Test]
		public void NullMembers()
		{
			Approve(
				Enumerable.Repeat(
				new { 
					Hello = "World",
					Foo = (string)null
				}, 1)
			);
		}

		[Test]
		public void NullNestedMembers()
		{
			Approve(
				from i in Enumerable.Range(0, 11)
				select i == 10 ? null : new
				{
					i,
					Maybe = i % 2 == 1 ?
						new { Hello = "World" } :
						null,
					Foo = "Bar"
				}
			);
		}

		[Test]
		public void NullNestedInterfaceMembers()
		{
			Approve(
				from i in Enumerable.Range(0, 11)
				select i == 10 ? null : new
				{
					i,
					Maybe = (IEnumerable)(i % 2 == 1 ?
						"interfaces can be null too" :
						null),
					Foo = "Bar"
				}
			);
		}

		[Test]
		public void QuotedValue()
		{
			Approve(
				Enumerable.Repeat(
				new { 
					_ = "\"World",
					A = "Wo\nrld",
					B = ",rld",
					C = "r\rld",
					D = ",r\rld",
					E = ",r\r\nld",
					F = ",r\r\nl\"d",
				}, 1)
			);
		}

		[Test]
		public void DateTimes()
		{
			var baseDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

			Approve(
				from i in Enumerable.Range(0, 24)
				select baseDate.AddHours(i * .95)
			);
		}

		[Test]
		public void NullableInts()
		{
			Approve(
				from i in Enumerable.Range(0, 24)
				select new
				{
					integer = i,
					nullable = i % 2 == 0 ? (int?)null : i
				});
		}

		[Test]
		public void DataMemberOrder()
		{
			var start = new DateTime(2010, 10, 10, 10, 10, 10, 10, DateTimeKind.Utc);
			Approve(
				from i in Enumerable.Range(0, 24)
				select new LocalMemberInfoTests.DayCount
				{
					Date = start.AddDays(i),
					Count = (uint)i,
				});
		}

		[Test]
		public void Ignore()
		{
            Approve(
                from i in Enumerable.Range(0, 24)
                select new SomeDataTransferObject
                {
                    Bar = i,
                    Foo = i,
                });
		}

		[Test]
		public void ByteArrays()
		{
            Approve(
                from i in Enumerable.Range(0, 24)
                select new
                {
                    Length = i,
                    Bytes = Enumerable.Range(0, i).Select(b => (byte)b).ToArray(),
                });
		}
	}
}
