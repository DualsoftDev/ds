using System;

namespace Dsu.Common.Utilities.Core.StaticMethods
{
    /// <summary>
    /// Real world functional programming.pdf, pp. 64
    /// You can use System.Tuple.Create()
    /// </summary>
    public static class TupleCreateSample
    {
        // var prague = Tuple.Create("Prague", 1188000);
        // var prague = new Tuple<string, int>("Prague", 1188000);
        public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new Tuple<T1, T2>(item1, item2);
        }
    }
}
