using Blazor.Extensions.Canvas.Canvas2D;

using Dual.Web.Blazor.Client.Components;

namespace Dual.Web.Blazor.Client.Canvas2d.Model;

public class Circle : CanvasObject, ICircle
{
    public Circle(string name, object tag, double x, double y, double r, object fillStyle)
        : base(x, y, name, tag)
    {
        (R, FillStyle) = (r, fillStyle);
    }

    public double R { get; set; }
    public object FillStyle { get; set; }

    public virtual async Task DrawAsync(Canvas2DContext context)
    {
        await context.BeginPathAsync();
        await context.ArcAsync(X, Y, R, 0, 2 * Math.PI, false);

        await Console.Out.WriteLineAsync($"Drawing circle({X}, {Y}, {R}, FillStyle={FillStyle}");
        if (FillStyle is not null)
            await context.SetFillStyleAsync(FillStyle);

        await context.FillAsync();
        await context.StrokeAsync();
    }

    public bool IsHit(double x, double y)
    {
        var dx = X - x;
        var dy = Y - y;
        return dx * dx + dy * dy <= R * R;
    }
    public virtual void OnMouse(IMouseEventArgs args) { }

    override public string ToString() => $"Circle: {Name}, ({X:0.##}, {Y:0.##}), {R:0.##}, {FillStyle ?? "NoFill"}";
}
