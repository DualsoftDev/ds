namespace DsWebApp.Client.Pages.Demo;

public class SampleSessionInfo
{
    public string Country { get; set; }
    public int Total { get; set; }

    public static SampleSessionInfo[] GetSamples() =>
        new SampleSessionInfo[] {
            new () { Country = "China",          Total = 16591 },
            new () { Country = "United States",  Total = 10286 },
            new () { Country = "India",          Total = 7978 },
            new () { Country = "South Korea",    Total = 6118 },
            new () { Country = "Germany",        Total = 5385 },
            new () { Country = "Turkey",         Total = 5064 },
            new () { Country = "Vietnam",        Total = 2804 },
            new () { Country = "United Kingdom", Total = 2451 },
            new () { Country = "Italy",          Total = 2130 },
            new () { Country = "Brazil",         Total = 2093 },
            new () { Country = "France",         Total = 1234 },
            new () { Country = "Japan",          Total = 5678 },
            new () { Country = "Canada",         Total = 987 },
            new () { Country = "Australia",      Total = 8765 },
            new () { Country = "Russia",         Total = 4321 },
            new () { Country = "Spain",          Total = 7890 },
            new () { Country = "Netherlands",    Total = 1234 },
            new () { Country = "Sweden",         Total = 5678 },
            new () { Country = "Norway",         Total = 987 },
            new () { Country = "Denmark",        Total = 8765 },
            new () { Country = "Mexico",         Total = 4321 },
            new () { Country = "Argentina",      Total = 7890 },
            new () { Country = "Chile",          Total = 1234 },
            new () { Country = "Peru",           Total = 5678 },
            new () { Country = "Colombia",       Total = 987 },
            new () { Country = "Venezuela",      Total = 8765 },
            new () { Country = "Ecuador",        Total = 4321 },
            new () { Country = "Bolivia",        Total = 7890 },
            new () { Country = "Paraguay",       Total = 1234 },
            new () { Country = "Uruguay",        Total = 5678 },
            new () { Country = "Guyana",         Total = 987 },
            new () { Country = "Suriname",       Total = 8765 },
            new () { Country = "Guyana",         Total = 4321 },
            new () { Country = "Suriname",       Total = 7890 },
            // 나머지 국가와 Total 값을 계속해서 추가합니다.
        };

}
