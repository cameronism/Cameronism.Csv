using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cameronism.Csv.Tests
{
    [TestFixture]
    public class DataTableTests
    {
        private static void Run<T>(IEnumerable<T> items)
        {
            var parameters = typeof(T)
                .GetConstructors()
                .Select(c => c.GetParameters())
                .OrderByDescending(p => p.Length)
                .FirstOrDefault();

            var props = new MethodInfo[parameters.Length];
            var dt = new DataTable();

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                dt.Columns.Add(param.Name, Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType);
                props[i] = typeof(T).GetProperty(param.Name).GetGetMethod();
            }


            foreach (var item in items)
            {
                var row = dt.NewRow();
                for (var i = 0; i < props.Length; i++)
                {
                    var value = props[i].Invoke(item, null);
                    row[i] = value ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }

            var sw = new StringWriter { NewLine = "\r\n" };
            Serializer.Serialize(sw, dt);
            Approvals.VerifyCsv(sw);
        }

        [Test]
        public void OneString()
        {
            //ExecuteReader("select 'ba\nr\"' as foo, 1 as one, null as n");
            Run(Enumerable.Range(1, 10)
                .Select(i => new
                {
                    i,
                    foo = i.ToString(),
                    ix = i % 2 == 0 ? (int?)i : null,
                    dt = new DateTime(2018, 1, i, 0, 0, 0, DateTimeKind.Utc),
                }));
        }
    }
}
