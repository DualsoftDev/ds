namespace Dual.Web.Blazor.Canvas;

/// <summary>
/// { Layout{Asset, Line} } 의 공통 인터페이스
/// </summary>
public interface ILayoutItem { }
public interface IWithPosition
{
    double X { get; set; }
    double Y { get; set; }
}
public interface IWithDimension
{
    double W { get; set; }  // spec 상의 width
    double H { get; set; }  // spec 상의 height
}

public interface ICanvasDimension : IWithDimension
{
    string Name { get; set; }
    double w { get; set; }  // 현재 canvas 의 width
    double h { get; set; }  // 현재 canvas 의 height
}

public class WithPosition : IWithPosition
{
    public WithPosition() {}
    public WithPosition(double x, double y) => (X, Y) = (x, y);

    public double X { get; set; }
    public double Y { get; set; }
}
