using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public static class EmEndian
    {
        public static UInt16 ToggleEndian(this UInt16 n) => BitConverter.ToUInt16(BitConverter.GetBytes(n).Reverse().ToArray(), 0);
        public static UInt32 ToggleEndian(this UInt32 n) => BitConverter.ToUInt32(BitConverter.GetBytes(n).Reverse().ToArray(), 0);
    }
}
