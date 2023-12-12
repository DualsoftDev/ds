using DsWebApp.Client.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class Sales
{
    static IList<SaleInfo> dataSource;
    static Sales()
    {
        CreateDataSource();
    }

    public static Task<IQueryable<SaleInfo>> GetSalesAsync()
    {
        return Task.FromResult(dataSource.AsQueryable());
    }

    static void CreateDataSource()
    {
        dataSource = new List<SaleInfo> {
        new SaleInfo {
                OrderId = 10248,
                Region = "North America",
                Country = "United States",
                City = "New York",
                Amount = 1740,
                Date = DateTime.Parse("2017/01/06")
            },
            new SaleInfo {
                OrderId = 10249,
                Region = "North America",
                Country = "United States",
                City = "Los Angeles",
                Amount = 850,
                Date = DateTime.Parse("2017/01/13")
            },
            new SaleInfo {
                OrderId = 10250,
                Region = "North America",
                Country = "United States",
                City = "Denver",
                Amount = 2235,
                Date = DateTime.Parse("2017/01/07")
            },
            new SaleInfo {
                OrderId = 10251,
                Region = "North America",
                Country = "Canada",
                City = "Vancouver",
                Amount = 1965,
                Date = DateTime.Parse("2017/01/03")
            },
            new SaleInfo {
                OrderId = 10252,
                Region = "North America",
                Country = "Canada",
                City = "Edmonton",
                Amount = 880,
                Date = DateTime.Parse("2017/01/10")
            },
            new SaleInfo {
                OrderId = 10253,
                Region = "South America",
                Country = "Brazil",
                City = "Rio de Janeiro",
                Amount = 5260,
                Date = DateTime.Parse("2017/01/17")
            },
            new SaleInfo {
                OrderId = 10254,
                Region = "South America",
                Country = "Argentina",
                City = "Buenos Aires",
                Amount = 2790,
                Date = DateTime.Parse("2017/01/21")
            },
            new SaleInfo {
                OrderId = 10255,
                Region = "South America",
                Country = "Paraguay",
                City = "Asuncion",
                Amount = 3140,
                Date = DateTime.Parse("2017/01/01")
            },
            new SaleInfo {
                OrderId = 10256,
                Region = "Europe",
                Country = "United Kingdom",
                City = "London",
                Amount = 6175,
                Date = DateTime.Parse("2017/01/24")
            },
            new SaleInfo {
                OrderId = 10257,
                Region = "Europe",
                Country = "Germany",
                City = "Berlin",
                Amount = 4575,
                Date = DateTime.Parse("2017/01/11")
            },
            new SaleInfo {
                OrderId = 10258,
                Region = "Rusia",
                Country = "Moscow",
                City = "Moscow",
                Amount = 455,
                Date = DateTime.Parse("2017/01/12")
            },
            // ...
    };
    }
}