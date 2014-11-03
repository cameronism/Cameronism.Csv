/* Cameronism.Csv
 * Copyright © 2014 Cameronism.com.  All Rights Reserved.
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
	public interface IMemberInfo
	{
		string Name { get; }
		Type Type { get; }
		MemberInfo MemberInfo { get; }
		IList<IMemberInfo> MemberPath { get; }
	}

}
