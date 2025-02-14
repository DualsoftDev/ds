using System;


namespace Dual.Common.Core
{
    public partial class CGuid
    {
#if !NETSTANDARD
        // GetGuidFromInterface(typeof(IFoo));
        public static Guid GetGuidFromInterface(Type t)
        {
            return System.Runtime.InteropServices.Marshal.GenerateGuidForType(t);
        }
#endif

        public static Guid NewGuid() { return Guid.NewGuid();  }
    }
}