using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Server.Information;

public class Startup
{
    static void Main(string[] args)
    {
        Console.WriteLine("Kafka information server");
        var ih = new InformationServer(@"..\..\..\Server.Information.config");
        ih.Executor();
        Console.ReadKey();
    }
}