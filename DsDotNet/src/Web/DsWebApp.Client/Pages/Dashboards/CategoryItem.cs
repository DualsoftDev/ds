namespace DsWebApp.Client.Pages.Dashboards;

public class CategoryItem
{
    public string Category { get; set; }
    public double Value { get; set; }
}

public class CategoryItemT<T>
{
    public T Category { get; set; }
    public string CategoryName => Category.ToString();
    public double Value { get; set; }
}

/// Efficiency Piece Of Pizza Type
public enum EfficiencyPopType
{
    가동,
    비가동,
    에러
}


public static class EfficiencyPop
{
    public static string ToColor(this EfficiencyPopType type)
    {
        return FillColors[(int)type];
    }
    public static string[] FillColors = [
        "green",
        "darkgoldenrod",
        "red"
    ];
}


public class EfficiencyCategoryItem : CategoryItemT<EfficiencyPopType>
{
}
