using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;

namespace Cameronism.Csv.Tests
{
    [TestFixture]
    public class DataReaderTests
    {
        private static void ExecuteReader(string sql)
        {
            using (var conn = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Integrated Security=true"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        var sw = new StringWriter { NewLine = "\r\n" };
                        Serializer.Serialize(sw, reader);
                        ApprovalTests.Approvals.Approve(sw);
                    }
                }
            }
        }

        [Test]
        public void OneString()
        {
            ExecuteReader("select 'bar' as foo");
        }

        [Test]
        public void TwoString()
        {
            ExecuteReader("select 'ba\nr\"' as foo, 1 as one, null as n");
        }
    }
}
