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
    class LocalMemberInfoTests
    {

        static void Approve<T>(T item)
        {
            var writer = new StringWriter { NewLine = "\r\n" };
            var members = (
                from mi in Serializer.FindAllMembers(typeof(T))
                let pathItems = (mi.MemberPath ?? Enumerable.Empty<IMemberInfo>()).Concat(new[] { mi }).Select(m => m.Name)
                let path = String.Join(".", pathItems)
                orderby path
                select path).ToList();

            writer.WriteLine("= Value");
            writer.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));

            writer.WriteLine();
            writer.WriteLine("= Members");
            writer.WriteLine(JsonConvert.SerializeObject(members, Formatting.Indented));

            Approvals.VerifyText(writer);
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


        [DataContract]
        public class DayCount
        {
            [DataMember(Order = 1)]
            public DateTime Date { get; set; }

            [DataMember(Order = 2)]
            public uint Count { get; set; }

            [DataMember(Order = 5)]
            public uint Ordering { get; set; }

            [DataMember(Order = 7)]
            public uint Begun { get; set; }

            [DataMember(Order = 4)]
            public uint Sense { get; set; }

            [DataMember(Order = 6)]
            public uint Has { get; set; }

            [DataMember(Order = 3)]
            public uint Non { get; set; }

        }

        [Test]
        public void ContractWithDateTime()
        {
            Approve(new DayCount { Date = DateTime.MinValue, Count = 42 });
        }
    }
}
