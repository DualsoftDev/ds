using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Dual.Common.Core
{
#if NET
    [SupportedOSPlatform("windows")]
#endif
    public static class EmCOM
    {
        /// <summary>
        /// If the COM object should be released before the garbage collector cleans up the object, the static method
        /// Marshal.ReleaseComObject invokes the Release method of the component so that the component can
        /// destroy itself and free up memory:
        /// pp. 641.  Professional C# 2012 and .NET 4.5.pdf
        ///
        /// GC 에 의해서 최종적으로는 garbage collection 되는 듯함.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public static int ReleaseComObject(object o)
        {
            return Marshal.ReleaseComObject(o);
        }
    }
}
