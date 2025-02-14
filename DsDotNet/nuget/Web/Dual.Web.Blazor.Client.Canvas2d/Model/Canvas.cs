using Blazor.Extensions.Canvas.Canvas2D;

namespace Dual.Web.Blazor.Client.Canvas2d.Model;

public class Canvas : ICanvas
{
    public List<IDrawable> Drawables { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public object FillStyle { get; set; } //= "rgba(255, 0, 0, 0.1)";     //"#003366";  // e.g color 인 경우, 문자열로 "#003366"
    /// <summary>
    /// 모든 Canvas object 에 대해서 default fill style
    /// </summary>
    public string StrokeStyle { get; set; } = "#FFFFFF";  // e.g "#FFFFFF"
    public virtual void Resize(double width, double height) =>
        (Width, Height) = (width, height);

    public virtual async Task DrawObjectsAsync(Canvas2DContext context)
    {
        foreach (IDrawable widget in Drawables)
            await widget.DrawAsync(context);
    }



    public async Task ClearBackgroundAsync(Canvas2DContext context)
    {
        await context.ClearRectAsync(0, 0, Width, Height);
        if (FillStyle != null)
        {
            await context.SetFillStyleAsync(FillStyle);
            await context.FillRectAsync(0, 0, Width, Height);
        }

        await context.SetStrokeStyleAsync(StrokeStyle);
    }
}
