using System;

namespace Dual.Common.Core
{
    public static class EmConversion
    {
        public static double Degree2Radian(this double degree) { return degree * Math.PI / 180.0; }
        public static double Radian2Degree(this double radian) { return radian * 180.0 / Math.PI; }
        public static double Degree2Radian(this float degree) { return degree * Math.PI / 180.0; }
        public static double Radian2Degree(this float radian) { return radian * 180.0 / Math.PI; }


        public static double Sq2(this double val) { return val * val; }
        public static float Sq2(this float val) { return val * val; }
        public static int Sq2(this int val) { return val * val; }

        public static double Sq3(this double val) { return val * val * val; }
        public static float Sq3(this float val) { return val * val * val; }
        public static int Sq3(this int val) { return val * val * val; }

        public static double Sq4(this double val) { return val * val * val * val; }
        public static float Sq4(this float val) { return val * val * val * val; }
        public static int Sq4(this int val) { return val * val * val * val; }
    }
}
