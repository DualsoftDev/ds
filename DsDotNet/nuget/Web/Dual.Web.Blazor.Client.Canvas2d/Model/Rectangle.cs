using Blazor.Extensions.Canvas.Canvas2D;

using Dual.Web.Blazor.Client.Components;

namespace Dual.Web.Blazor.Client.Canvas2d.Model;

public class Rectangle : CanvasObject, IRectangle
{
    public Rectangle(string name, object tag, double x, double y, double w, double h, object fillStyle)
        : base(x, y, name, tag)
    {
        (W, H, FillStyle) = (w, h, fillStyle);
    }

    public double W { get; set; }
    public double H { get; set; }
    public object FillStyle { get; set; }

    public async Task DrawAsync(Canvas2DContext context)
    {
        await context.BeginPathAsync();
        await context.RectAsync(X, Y, W, H);

        if (FillStyle is not null)
            await context.SetFillStyleAsync(FillStyle);

        await context.FillAsync();
        await context.StrokeAsync();
    }
    public bool IsHit(double x, double y)
    {
        return X <= x && x <= X + W && Y <= y && y <= Y + H;
    }

    public virtual void OnMouse(IMouseEventArgs args) { }

    override public string ToString() => $"Rectangle: {Name}, {X:0.##}, {Y:0.##}, {W:0.##}, {H:0.##}, {FillStyle??"NoFill"}";
}
