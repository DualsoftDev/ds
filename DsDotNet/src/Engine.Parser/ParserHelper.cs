using Engine.Common;

using System.Collections.Generic;
using System.Globalization;

namespace Engine.Parser;

public class ParserHelper
{
    public Dictionary<(DsSystem, string), object> QpInstanceMap = new();

    /// <summary> Alias, CallPrototype 에 대한 path </summary>
    public Dictionary<(DsSystem, string), object> QpDefinitionMap = new();
    public Dictionary<DsSystem, Dictionary<string, string>> AliasNameMaps = new();
    public Dictionary<DsSystem, Dictionary<string, string[]>> BackwardAliasMaps = new();

    public Model Model { get; } = new Model();
    internal DsSystem _system;
    internal RootFlow _rootFlow;
    internal SegmentBase _parenting;

    public Dictionary<string, Cpu> FlowName2CpuMap;

    public ParserOptions ParserOptions { get; set; }
    public ParserHelper(ParserOptions options)
    {
        ParserOptions = options;
    }

    internal string[] CurrentPathNameComponents
    {
        get
        {
            IEnumerable<string> helper()
            {
                if (_system != null)
                    yield return _system.Name;
                if (_rootFlow != null)
                    yield return _rootFlow.Name;
                if (_parenting != null)
                    yield return _parenting.Name;
            }
            return helper().ToArray();
        }
    }
    internal string CurrentPath => CurrentPathNameComponents.Combine();



    public T FindObject<T>(DsSystem system, string qualifiedName) where T : class => PickQualifiedPathObject<T>(system, qualifiedName);

    public T[] FindObjects<T>(DsSystem system, string qualifiedNames) where T : class
    {
        if (qualifiedNames == "_")
            return Array.Empty<T>();

        return
            qualifiedNames
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => FindObject<T>(system, name))
                .ToArray()
                ;
    }


    T PickQualifiedPathObject<T>(DsSystem system, string qualifiedName, Func<T> creator = null) where T : class
    {
        var key = (system, qualifiedName);
        var dict = QpInstanceMap;
        if (dict.ContainsKey(key))
            return (T)dict[key];

        if (creator == null)
        {
            if (ParserOptions.AllowSkipExternalSegment)
                return null;
            throw new Exception($"ERROR: failed to create {qualifiedName}");
        }

        var t = creator();
        dict[key] = t;

        return t;
    }


    public string ToFQDN(string name)
    {
        string concat(params string[] names) =>
            String.Join(".", names.Where(n => n != null))
            ;
        var sysName = _system.Name;

        var nameComponents = name.Split(new[] { '.' }).ToArray();
        var middleName = _rootFlow.Name;
        var mid = name.StartsWith($"{middleName}.") ? null : middleName;
        var parentingName = _parenting?.Name;
        var par = name.StartsWith($"{parentingName}.") ? null : parentingName;
        switch (nameComponents.Length)
        {
            case 1:
                if (AliasNameMaps[_system].ContainsKey(name))
                    return name;
                return concat(sysName, middleName, parentingName, name);
            case 2:
                Assert(!name.StartsWith(sysName));
                return concat(sysName, mid, par, name);
            case 3:
                return name;
            default:
                throw new Exception("ERROR");
        }
    }
}


