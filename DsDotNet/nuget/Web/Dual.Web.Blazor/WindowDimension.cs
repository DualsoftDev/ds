namespace Dual.Web.Blazor.Client;

public class WindowDimension
{
    public int Width { get; set; }
    public int Height { get; set; }
}

/// <summary>
/// Left, Top, Width, Height
/// </summary>
public class Rect
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public double X { get => Left; set => Left = value; }
    public double Y { get => Top; set => Top = value; }
    public double W { get => Width; set => Width = value; }
    public double H { get => Height; set => Height = value; }

    public Rect() {}

    public Rect(double x, double y, double w, double h)
    {
        (X, Y, W, H) = (x, y, w, h);
    }
}