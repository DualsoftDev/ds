using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public static class EmBuffer
    {
        public static byte[] BlockCopy(this byte[] src, int offset, int count)
        {
            byte[] cc = new byte[count];
            System.Buffer.BlockCopy(src, offset, cc, 0, count);
            return cc;
        }

        public static byte[] BlockCopy(this byte[] src, ref int offset, int count)
        {
            byte[] cc = new byte[count];
            System.Buffer.BlockCopy(src, offset, cc, 0, count);
            offset += count;
            return cc;
        }

	    public static string ToHexString(this byte[] src, string delimiter = " ", string prefix = "", string postfix = "")
	    {
			// for simple case, use BitConverter.ToSTring(byte[] str)
			return
                src.Select(c => $"{prefix}{c:X2}{postfix}")
                .JoinString(delimiter)
                ;
	    }

		/// <summary>
		/// {' ', '-', ',' } 외의 다른 요소 없이 Hex 로만 구성된 문자열을 byte 로 변환.  e.g. "FF321423" ==> byte[] {0xFF, 0x32, 0x14, 0x23}
		/// </summary>
		/// <param name="hex"></param>
		/// <returns></returns>
		public static byte[] ToByteArray(this string hex)
		{
			var hex0 = new string(hex.Where(c => !c.IsOneOf(' ', '-', ',')).ToArray());
			int NumberChars = hex0.Length;
			byte[] bytes = new byte[NumberChars / 2];
			for (int i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(hex0.Substring(i, 2), 16);
			return bytes;
		}
	}
}
