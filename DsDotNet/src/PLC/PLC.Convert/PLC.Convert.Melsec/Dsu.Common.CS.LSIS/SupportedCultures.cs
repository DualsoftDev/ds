using System.Globalization;
using System.Threading;

namespace Dsu.Common.CS.LSIS
{
    public enum SupportedCultures
    {
        English,
        Korean,
    }

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

        public static void Apply(this SupportedCultures culture)
        {
            var ci = culture.ConvertToCultureInfo();
            ci.Apply();
        }

        public static void Apply(this CultureInfo ci)
        {
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

    }
}
