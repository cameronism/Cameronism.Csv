using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Cameronism.Csv.Tests
{

	[TestFixture]
	class BuildFlattenerTests
	{

		static void Approve<T>(T item)
		{
			var writer = new StringWriter { NewLine = "\r\n" };
			var flattener = Serializer.CreateFlattener(typeof(T));
			var flatItem = flattener.Flatten(item);

			writer.WriteLine("= Value");
			writer.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));

			writer.WriteLine();
			writer.WriteLine("= Flat");
			writer.WriteLine(JsonConvert.SerializeObject(flatItem, Formatting.Indented));

			ApprovalTests.Approvals.Approve(writer);
		}

		[Test]
		public void AnonymousType()
		{
			Approve(new { Hello = "World" });
		}

		[Test]
		public void NestedAnonymousType()
		{
			Approve(new 
			{ 
				Hello = "World",
				Foo = new 
				{
					Bar = 42,
					Bop = Guid.Empty,
				},
			});
		}

		[DataContract]
		class SemiDecorated
		{
			[DataMember]
			public string Included { get; set; }

			public string Excluded { get; set; }
		}

		[Test]
		public void DataContract()
		{
			Approve(new SemiDecorated { Included = "foo", Excluded = "bar" });
		}


		[Test]
		public void SimpleValue()
		{
			Approve(1);
		}
	}
}
