/* Cameronism.Csv
 * Copyright © 2012 Cameronism.com.  All Rights Reserved.
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

namespace Cameronism.Csv.Tests
{
	[TestFixture]
	public class Verify
	{
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
	}
}
