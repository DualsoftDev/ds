using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.FSharp.Core;

namespace Dsu.Common.Utilities.Core.FSharpInterOp
{
    public static class EmFSharpOption
    {
        public static bool IsSome<T>(this FSharpOption<T> optionValue) => FSharpOption<T>.get_IsSome(optionValue);
        public static bool IsNone<T>(this FSharpOption<T> optionValue) => FSharpOption<T>.get_IsNone(optionValue);
    }
}
