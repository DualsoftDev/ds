using Dual.Common.Base.CS;

using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace Dual.Common.Core
{
    public static class EmList
	{
		public static T Pop<T>(this List<T> list)
		{
			T last = list.Last();
			list.RemoveTail();
			return last;
		}
	}
}
