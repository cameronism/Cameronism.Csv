using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cameronism.Csv
{
    [System.AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class CsvIgnoreAttribute : Attribute
    {
    }
}
