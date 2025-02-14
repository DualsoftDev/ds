using System.Drawing;

namespace Dual.Web.Blazor;

public static class ExtensionMethod
{
    public static string ToHexColorString(this Color color) => string.Format("#{0:X6}", color.ToArgb() & 0x00FFFFFF);
    public static string ToHexColorString(this Color color, byte alpha) => string.Format("#{0:X8}", ((color.ToArgb() & 0x00FFFFFF) << 8) | alpha);
    public static string ToHexColorString(this Color color, double alpha) => ToHexColorString(color, (byte)(alpha * 255));
}
