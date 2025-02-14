using System;
using System.Globalization;

namespace Dual.Common.Core;

public static class EmDateTime
{
    public static string ToStringCulture(this DateTime dateTime, string cultureName = "en-US")
    {
        var culture = new CultureInfo(cultureName);
        return dateTime.ToString(culture);
    }
}

