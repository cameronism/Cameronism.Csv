/* Cameronism.Csv
 * Copyright © 2016 Cameronism.com.  All Rights Reserved.
 * 
 * Apache License 2.0 - http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Cameronism.Csv
{
    public interface IFlattener
    {
        Type Type { get; }
        IList<IMemberInfo> Members { get; }
        IList<object> Flatten(object item);
    }
}
