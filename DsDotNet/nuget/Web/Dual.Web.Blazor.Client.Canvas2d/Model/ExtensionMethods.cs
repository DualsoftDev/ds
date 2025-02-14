using System.Drawing;

namespace Dual.Web.Blazor.Client.Canvas2d.Model;

public static class ExtensionMethods
{
    public static void SetColor(this IFillable object2d, Color color) => object2d.FillStyle = color.ToHexColorString();

}
