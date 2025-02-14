using Dual.Web.Blazor.Canvas;

namespace Dual.Web.Blazor.Client.Canvas2d.Model;

public abstract class CanvasObject : WithPosition
{
    protected CanvasObject(double x, double y, string name, object tag)
        : base(x, y)
    {
        Name = name;
        Tag = tag;
    }

    public string Name { get; set; }
    public object Tag { get; set; }
}
