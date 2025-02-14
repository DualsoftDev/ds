namespace Dual.Common.Core
{
    public static class EmDataType
    {
        public static bool GetBit(this byte aByte, int nthBit)
        {
            return (aByte & (1 << nthBit)) != 0;
        }


        /// <summary>
        /// int n 을 byte 로 환산한 4 byte 를 반환한다.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public static byte[] GetBytes(this int n)
        {
            byte b0 = (byte)((n & 0xFF000000) >> 24);
            byte b1 = (byte)((n & 0x00FF0000) >> 16);
            byte b2 = (byte)((n & 0x0000FF00) >> 8);
            byte b3 = (byte)((n & 0x000000FF) >> 0);

            return new byte[] {b0, b1, b2, b3,};
        }
    }
}
