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
                        Approvals.VerifyCsv(sw);
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

        [Test]
        public void SomeBytes()
        {
            ExecuteReader("select 0xFF0872FDDA3FC1EFDA9706B2B3FBAC7BF6DAB1EEB8 as bytes");
        }
    }
}
