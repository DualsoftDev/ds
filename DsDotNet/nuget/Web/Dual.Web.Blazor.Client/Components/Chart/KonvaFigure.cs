using Microsoft.JSInterop;

using System.Text.Json;

namespace Dual.Web.Blazor.Client.Components.Chart;


public class ShapeInfo
{
    public int Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double OrigX { get; set; }
    public double OrigY { get; set; }
    public ShapeInfo() {}
    public ShapeInfo(int id, double x, double y)
    {
        (Id, X, Y, OrigX, OrigY) = (id, x, y, x, y);
    }
}

public abstract class KonvaShape : ShapeInfo
{
    public string Type { get; protected set; } // 예: "ShapeRect", "ShapeCircle"
    protected static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public virtual string Serialize() => throw new Exception("Serialize() should be overwridden!");   // SystemTextJson.Serialize(this, JsonSerializerOptions);
}

public class KonvaImage : KonvaShape
{
    //public IJSObjectReference Image { get; set; }
    public string ImageUrl { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double OrigWidth { get; set; }
    public double OrigHeight { get; set; }

    public KonvaImage()
    {
        Type = "KonvaImage"; // JSON에서 객체를 구별할 때 사용
    }
    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);
}

public abstract class KonvaFigure : KonvaShape
{
    public string Fill { get; set; } = "red";
    public string Stroke { get; set; }
    public int[] Dash { get; set; } //= [5, 2];
    public int StrokeWidth { get; set; } = 4;
    public bool Draggable { get; set; }

    protected KonvaFigure()
    {
        Type = GetType().Name; // 현재 객체의 타입 이름을 Type 속성에 할당
    }
}

public class KonvaFigureRect : KonvaFigure
{
    public double Width { get; set; }
    public double Height { get; set; }
    public double OrigWidth { get; set; }
    public double OrigHeight { get; set; }
    public double CornerRadius { get; set; } = 4;

    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);
}

public class KonvaFigureCircle : KonvaFigure
{
    public double Radius { get; set; }
    public double OrigRadius { get; set; }
    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);
}

public class KonvaFigureStar : KonvaFigure
{
    public int NumPoints { get; set; } = 6;
    public double InnerRadius { get; set; }
    public double OuterRadius { get; set; }
    public double OrigInnerRadius { get; set; }
    public double OrigOuterRadius { get; set; }
    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);

}
public class KonvaFigureWedge : KonvaFigure
{
    public double Radius { get; set; }
    public double Angle { get; set; } = 60;
    public double OrigRadius { get; set; }
    public double OrigAngle { get; set; } = 60;
    public double Rotation { get; set; } = -120;
    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);

}
public class KonvaFigureRegularPolygon : KonvaFigure
{
    public int Sides { get; set; }
    public double Radius { get; set; }
    public double OrigRadius { get; set; }
    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);
}
public class KonvaFigureGroup : KonvaFigureRect
{
    //public KonvaFigure[] SubShapes;  // property 로 만들지 말 것.
    //public string SubShapesJson { get; set; }
    public KonvaFigureGroup()
    {
        CornerRadius = 10;
    }
    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);
}


public class KonvaFigureText : KonvaFigure
{
    public string Text { get; set; }
    public double FontSize { get; set; } = 24;
    public string FontFamily { get; set; } = "Calibri";
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 100;
    public int OrigWidth { get; set; } = 200;
    public int OrigHeight { get; set; } = 100;

    public int Padding { get; set; }
    public string Align { get; set; } = "center";
    public override string Serialize() => SystemTextJson.Serialize(this, JsonSerializerOptions);
}

