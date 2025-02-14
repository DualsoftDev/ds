using System;
using System.Globalization;
using System.Threading;

namespace Dual.Common.Core
{
    public enum SupportedCultures
    {
        English,
        Korean,
    }

    [Obsolete("Use EmCultrure instead")]
    public static class CultrureConverter
    {
        public static string ConvertToString(this SupportedCultures culture)
        {
            switch (culture)
            {
                case SupportedCultures.English:
                    return "en-US";
                case SupportedCultures.Korean:
                    return "ko-KR";
            }

            return null;
        }

        public static CultureInfo ConvertToCultureInfo(this SupportedCultures culture)
        {
            return new CultureInfo(culture.ConvertToString());
        }

        public static void ApplyObsoleted(this SupportedCultures culture)
        {
            var ci = culture.ConvertToCultureInfo();
            ci.ApplyObsoleted();
        }

        public static void ApplyObsoleted(this CultureInfo ci)
        {
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

    }
}
