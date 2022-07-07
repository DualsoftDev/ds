using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Core.ExtensionMethods
{
    public static class EmFunctional
    {
        public static T1 fst<T1, T2>(this (T1, T2) tpl) => tpl.Item1;
    }
}
