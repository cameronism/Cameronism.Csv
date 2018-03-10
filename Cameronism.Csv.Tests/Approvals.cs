using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cameronism.Csv.Tests
{
    internal class Approvals
    {
        public static void VerifyCsv(TextWriter tw)
        {
            var writer = ApprovalTests.Writers.WriterFactory.CreateTextWriter(tw.ToString(), "csv");
            ApprovalTests.Approvals.Verify(writer);
        }

        public static void VerifyText(TextWriter tw)
        {
            ApprovalTests.Approvals.Verify(tw.ToString());
        }
    }
}
