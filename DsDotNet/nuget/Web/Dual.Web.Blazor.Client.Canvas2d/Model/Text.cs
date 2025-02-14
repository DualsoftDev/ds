using Blazor.Extensions.Canvas.Canvas2D;

using Dual.Web.Blazor.Client.Components;

namespace Dual.Web.Blazor.Client.Canvas2d.Model;

public class Text : CanvasObject, IText
{
    public Text(string name, object tag, double x, double y, string text, string font, string fillStyle)
        : base(x, y, name, tag)
    {
        (TextContent, Font, FillStyle) = (text, font, fillStyle);
    }

    public string TextContent { get; set; }
    public string Font { get; set; }    // e.g "26px Segoe UI"
    public object FillStyle { get; set; }

    public virtual async Task DrawAsync(Canvas2DContext context)
    {
        await context.SetFontAsync(Font);
        if (FillStyle is not null)
            await context.SetFillStyleAsync(FillStyle);
        await context.FillTextAsync(TextContent, X, Y);
    }
    public bool IsHit(double x, double y) => false;
    public virtual void OnMouse(IMouseEventArgs args) {}
}
