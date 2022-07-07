using System;


namespace Dsu.Common.Utilities
{
    public partial class CGuid
    {
        // GetGuidFromInterface(typeof(IFoo));
        public static Guid GetGuidFromInterface(Type t)
        {
            return System.Runtime.InteropServices.Marshal.GenerateGuidForType(t);
        }

        public static Guid NewGuid() { return Guid.NewGuid();  }
    }

    public static class EmGuid
    {
        public const int Length = 36;
        public static string ToStringSimple(this Guid guid) => GuidStringSimple(guid.ToString());
        public static string GuidStringSimple(this string guid) => guid.Substring(0, 8);
        public static string CreateSimpleGuid() => ToStringSimple(Guid.NewGuid());
    }

}