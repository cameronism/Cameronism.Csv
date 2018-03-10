using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Runtime.Serialization;
using System.IO;

namespace Cameronism.Csv.Tests
{
    [TestFixture]
    public class Attributes
    {
        [DataContract]
        public class Foo
        {
            [DataMember]
            public int A { get; set; }
            [DataMember]
            public string B { get; set; }
            // no DataMember
            public DateTime C { get; set; }
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
        public void ShouldOnlySerializeContractMembers()
        {
            Approve(new[] {
                new Foo
                {
                    A = 42,
                    B = "C should be omitted since it does not have a DataMemberAttribute",
                    C = DateTime.UtcNow, // Should be different every test
				}
            });
        }
    }
}
