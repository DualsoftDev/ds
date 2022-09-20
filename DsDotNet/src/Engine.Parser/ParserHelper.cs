namespace Engine.Parser;


public class ParserHelper
{
    // button category 중복 check 용
    public HashSet<(DsSystem, string)> ButtonCategories = new();

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


    internal string[] GetCurrentPathComponents(string lastName) =>
        CurrentPathNameComponents.Append(lastName).ToArray();
    internal string[] GetCurrentPath3Components(string lastName) =>
        CurrentPathNameComponents.Take(2).Append(lastName).ToArray();

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



    public T FindObject<T>(string[] qualifiedName) where T : class => PickQualifiedPathObject<T>(qualifiedName);

    public T[] FindObjects<T>(DsSystem system, RootFlow flow, string qualifiedNames) where T : class
    {
        if (qualifiedNames == "_")
            return Array.Empty<T>();

        return
            qualifiedNames
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => FindObject<T>(name.Divide()))
                .ToArray()
                ;
    }


    T PickQualifiedPathObject<T>(string[] qualifiedName, Func<T> creator = null) where T : class
    {
        T target = (T)Model.Find(qualifiedName);
        if (target != null)
            return target;

        if (creator == null)
        {
            if (ParserOptions.AllowSkipExternalSegment)
                return null;
            throw new Exception($"ERROR: failed to create {qualifiedName}");
        }

        var t = creator();
        Model.FindFlow(qualifiedName).InstanceMap[qualifiedName.Last()] = t;

        return t;
    }


    public string[] ToFQDN(string[] ns)
    {
        //string concat(params string[] names) =>
        //    String.Join(".", names.Where(n => n != null))
        //    ;
        var sysName = _system.Name;
        var flowName = _rootFlow.Name;
        var name = ns.Last();
        var mid = name.StartsWith($"{flowName}.") ? null : flowName;
        var parentingName = _parenting?.Name;
        var par = name.StartsWith($"{parentingName}.") ? null : parentingName;
        switch (ns.Length)
        {
            case 1:
                if (_rootFlow.AliasNameMaps.ContainsKey(ns))
                    return ns;
                break;
            //case 2:
            //    Assert(!name.StartsWith(sysName));
            //    return concat(sysName, mid, par, name);
            //case 3:
            //    return name;
            //default:
            //    throw new Exception("ERROR");
        }
        return GetCurrentPathComponents(name);
    }
}


