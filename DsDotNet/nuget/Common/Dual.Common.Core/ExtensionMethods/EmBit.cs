using System.Diagnostics.Contracts;

namespace Dual.Common.Core
{
    public static class EmBit
    {
        #region ULONG
        /// <summary>
        /// value 에서 [start, end] 구간의 bit 값을 해석하여, 값으로 반환.   e.g value 의 MSB=1 일때, 63-th bit 를 읽으면 1을 반환한다.
        /// todo: bigint 를 이용한 구현 방법 테스트
        /// </summary>
        /// <param name="value">읽을 대상</param>
        /// <param name="start">시작 bit index</param>
        /// <param name="end">끝 bit index.  포함됨.  0부터 4bit 를 읽는다면, start=0, end=3 이 주어져야 한다.</param>
        /// <returns></returns>
        public static ulong GetBitsValue(this ulong value, int start, int end)
        {
            Contract.Requires(end >= start);
            Contract.Requires(end - start < 64);

            var s = start;
            var e = end + 1;
            var length = e - s;
            ulong masked = (value << (64 - e)) >> (64 - length);
            return masked;
        }

        /// <summary>
        /// value 에서 [start, end] 구간의 bit 값을 추출하여 반환.   e.g value 의 MSB=1 일때, 63-th bit 만 읽으면 0x10000000 을 반환한다.
        /// </summary>
        /// <param name="value">읽을 대상</param>
        /// <param name="start">시작 bit index</param>
        /// <param name="end">끝 bit index.  포함됨.  0부터 4bit 를 읽는다면, start=0, end=3 이 주어져야 한다.</param>
        /// <returns></returns>
        public static ulong GetBits(this ulong value, int start, int end)
        {
            var masked = value.GetBitsValue(start, end);
            return masked << start;
        }

        public static bool GetBit(this ulong value, int index) => value.GetBitsValue(index, index) != 0;

        //public static ulong SetBits(this ulong value, int start, int end, ulong subValue)
        //{
        //    var s = start;
        //    var e = end + 1;
        //    var length = e - s;
        //    const ulong ff = 0xFFFFFFFFFFFFFFFF;
        //    ulong maskPositive = (ff << (64 - e)) >> (64 - length);     // 0x0000..FF..00;
        //    ulong maskNegative = ff ^ maskPositive;                     // 0xFFFF..00..FF;


        //}
        #endregion



        #region UINT
        public static uint GetBitsValue(this uint value, int start, int end)
        {
            return (uint) ((ulong) value).GetBitsValue(start, end);
        }
        public static uint GetBits(this uint value, int start, int end)
        {
            var masked = value.GetBitsValue(start, end);
            return masked << start;
        }

        public static bool GetBit(this uint value, int index) => value.GetBitsValue(index, index) != 0;
        #endregion




        #region USHORT
        public static ushort GetBitsValue(this ushort value, int start, int end)
        {
            return (ushort)((ulong)value).GetBitsValue(start, end);
        }
        public static ushort GetBits(this ushort value, int start, int end)
        {
            var masked = value.GetBitsValue(start, end);
            return (ushort)(masked << start);
        }

        public static bool GetBit(this ushort value, int index) => value.GetBitsValue(index, index) != 0;
        #endregion



        #region BYTE
        public static byte GetBitsValue(this byte value, int start, int end)
        {
            return (byte)((ulong)value).GetBitsValue(start, end);
        }
        public static byte GetBits(this byte value, int start, int end)
        {
            var masked = value.GetBitsValue(start, end);
            return (byte)(masked << start);
        }

        public static bool GetBit(this byte value, int index) => value.GetBitsValue(index, index) != 0;
        #endregion

    }
}
