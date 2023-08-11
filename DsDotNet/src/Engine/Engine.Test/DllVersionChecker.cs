using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Engine.Common;

public class DllVersionChecker
{

    /// <summary>
    /// ../../ExLib 폴더내 관리중인 외부 DLL 리스트 
    /// microsoft DLL은 nuget 최신 사용권고 (Ex: FSharp.Core.dll, System.Reactive.dll, ...)
    /// </summary>
    private static Dictionary<string, string> DllExlib
    {
        get
        {
            var exlibDLL = new Dictionary<string, string>();
            exlibDLL.Add("Confluent.Kafka", "1.9.3.0");
            exlibDLL.Add("log4net", "2.0.15.0");
            exlibDLL.Add("Newtonsoft.Json", "13.0.0.0");
            exlibDLL.Add("QuickGraph.Data", "3.6.61114.0");
            exlibDLL.Add("QuickGraph", "3.6.61114.0");
            exlibDLL.Add("QuickGraph.Graphviz", "3.6.61114.0");
            exlibDLL.Add("QuickGraph.Serialization", "1.0.0.0");

            return exlibDLL;
        }
    }

    /// <summary>
    /// exLib 폴더에서 직접참조한 dll과 사용된 Assembly.GetExecutingAssembly() 와 버전 체크
    /// exLib 폴더내부 dll과 버전이 다르면 예외 (해결방안 exLib로 부터 직접 참조)
    /// </summary>
    private static Dictionary<string, List<Tuple<AssemblyName, Assembly>>> GetAssemblyNames(Assembly rootAssembly)
    {
        var assemblyAll = new Dictionary<string, List<Tuple<AssemblyName, Assembly>>>();
        var visited = new HashSet<string>();
        var queue = new Queue<Assembly>();

        queue.Enqueue(rootAssembly);

        while (queue.Any())
        {
            var assembly = queue.Dequeue();
            visited.Add(assembly.FullName);

            var references = assembly.GetReferencedAssemblies();
            foreach (var reference in references)
            {
                if (!visited.Contains(reference.FullName))
                    queue.Enqueue(Assembly.Load(reference));

                if (!assemblyAll.ContainsKey(reference.FullName))
                    assemblyAll.Add(reference.FullName, new List<Tuple<AssemblyName, Assembly>>() { Tuple.Create(reference, assembly) });
                else
                    assemblyAll[reference.FullName].Add(Tuple.Create(reference, assembly));
            }

        }
        return assemblyAll.OrderBy(o => o.Key).ToDictionary(d => d.Key, d => d.Value);
    }
    public static bool IsValidExDLL(Assembly myAssembly)
    {
        foreach (var usingDll in GetAssemblyNames(myAssembly))
        {
            var dll = usingDll.Value.First().Item1;
            var parents = usingDll.Value.Select(s => s.Item2.GetName().Name).Distinct();
            string dllText = $"사용된 Ver {dll.Version.ToString().PadRight(10)} \t{dll.Name.PadRight(40)}  \t 참조자 : {string.Join(", \t", parents)}";
            Debug.WriteLine(dllText);

            if (DllExlib.ContainsKey(dll.Name))
                if (DllExlib[dll.Name] != dll.Version.ToString())
                    throw new Exception($"{dllText} \t(유효 버전 : {DllExlib[dll.Name]})");
        }
        return true;
    }
}
