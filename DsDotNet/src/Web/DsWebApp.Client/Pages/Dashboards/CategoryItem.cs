namespace DsWebApp.Client.Pages.Dashboards;

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
public class CategoryItem
{
    public EfficiencyPopType Category { get; set; }
    public string CategoryName => Category.ToString();
    public double Value { get; set; }
}
