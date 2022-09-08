using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Engine.Common;

public class DllVersionChecker
{
    private static Dictionary<string, string> DllExlib
    {
        get
        {
            var exlibDLL = new Dictionary<string, string>();
            exlibDLL.Add("Confluent.Kafka", "1.9.2.0");
            exlibDLL.Add("log4net", "2.0.15.0");
            exlibDLL.Add("Newtonsoft.Json", "13.0.1.25517");
            exlibDLL.Add("QuickGraph.Data", "3.6.61114.0");
            exlibDLL.Add("QuickGraph", "3.6.61114.0");
            exlibDLL.Add("QuickGraph.Graphviz", "3.6.61114.0");
            exlibDLL.Add("QuickGraph.Serialization", "1.0.0.0");

            return exlibDLL;
        }
    }

    /// <summary>
    /// Assembly ValidExDLLVersion 확인
    /// </summary>
    public static bool ValidExDLLVersion(Assembly myAssembly)
    {
        Dictionary<string, string> myDLLs = new Dictionary<string, string>();

        var dicDLL = myAssembly
            .GetReferencedAssemblies().ToDictionary(x => x.Name, x => x.Version.ToString());

        foreach (var usingDll in dicDLL)
        {
            if (DllExlib.ContainsKey(usingDll.Key) && DllExlib[usingDll.Key] != usingDll.Value)
            {
                return false;
            }
        }
        return true;
    }


}
