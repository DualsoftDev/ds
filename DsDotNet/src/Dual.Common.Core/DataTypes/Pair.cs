using System.Runtime.InteropServices;

namespace Dsu.Common.Utilities
{
    // For .Net Version 4.0, System.Tuple can be used instead...
    // http://stackoverflow.com/questions/166089/what-is-c-sharp-analog-of-c-stdpair
    [ComVisible(false)]
    public class Pair<T, U>
    {
        public Pair()
        {
        }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };
}
