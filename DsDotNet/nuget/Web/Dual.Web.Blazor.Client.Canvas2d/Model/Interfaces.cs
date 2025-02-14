using Blazor.Extensions.Canvas.Canvas2D;
using Dual.Web.Blazor.Client.Components;
using Dual.Web.Blazor.Canvas;

namespace Dual.Web.Blazor.Client.Canvas2d.Model;

public interface ICanvas
{
    List<IDrawable> Drawables { get; set; }
    double Width { get; set; }
    double Height { get; set; }
    object FillStyle { get; set; }
    string StrokeStyle { get; set; }   // e.g "#FFFFFF"
    void Resize(double width, double height);
    Task DrawObjectsAsync(Canvas2DContext context);
    Task ClearBackgroundAsync(Canvas2DContext context);
}

public interface IDrawable
{
    string Name { get; set; }
    object Tag { get; set; }
    Task DrawAsync(Canvas2DContext context);
    bool IsHit(double x, double y);
    void OnMouse(IMouseEventArgs args);
}

public interface IFillable : IDrawable
{
    /// <summary>
    /// e.g color 인 경우, 문자열로 "#FF0000"
    /// </summary>
    object FillStyle { get; set; }
}

public interface IText : IWithPosition, IFillable, IDrawable
{
    string TextContent { get; set; }
    string Font { get; set; }   // e.g "26px Segoe UI"
}


public interface ICircle : IWithPosition, IFillable, IDrawable
{
    double R { get; set; }
}

public interface IRectangle : IWithPosition, IWithDimension, IFillable, IDrawable
{
}
