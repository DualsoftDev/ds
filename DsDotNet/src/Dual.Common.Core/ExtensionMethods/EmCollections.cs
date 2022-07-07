using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dsu.Common.Utilities.ExtensionMethods;

namespace Dsu.Common.Utilities.Core.ExtensionMethods
{
	public static class EmCollections
	{
		public static IEnumerable<T> ToEnumerable<T>(this Array arr)
		{
			if (arr.IsNullOrEmpty())
				yield break;

			foreach (object o in arr)
				yield return (T) o;
		}
	}
}
