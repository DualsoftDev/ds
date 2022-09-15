using Engine.Common;
using System.Globalization;

namespace Engine.Parser;

public class ParserHelper
{
    public Dictionary<string, object> QualifiedInstancePathMap = new();

    /// <summary> Alias, CallPrototype 에 대한 path </summary>
    public Dictionary<string, object> QualifiedDefinitionPathMap = new();
    public Dictionary<DsSystem, Dictionary<string, string>> AliasNameMaps = new();
    public Dictionary<DsSystem, Dictionary<string, string[]>> BackwardAliasMaps = new();

    public Model Model { get; } = new Model();
    internal DsSystem _system;
    internal DsTask _task;
    internal RootFlow _rootFlow;
    internal SegmentBase _parenting;

    internal Dictionary<string, Cpu> FlowName2CpuMap;

    internal string CurrentPath
    {
        get
        {
            if (_task != null)
                return $"{_system.Name}.{_task.Name}";
            if (_parenting != null)
                return $"{_system.Name}.{_rootFlow.Name}.{_parenting.Name}";
            if (_rootFlow != null)
                return $"{_system.Name}.{_rootFlow.Name}";
            if (_system != null)
                return _system.Name;

            throw new Exception("ERROR");
        }
    }

    public T FindObject<T>(string qualifiedName) where T : class => PickQualifiedPathObject<T>(qualifiedName);

    public T[] FindObjects<T>(string qualifiedNames) where T : class
    {
        if (qualifiedNames == "_")
            return Array.Empty<T>();

        return
            qualifiedNames
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => FindObject<T>(name))
                .ToArray()
                ;
    }


    T PickQualifiedPathObject<T>(string qualifiedName, Func<T> creator = null) where T : class
    {
        var dict = QualifiedInstancePathMap;
        if (dict.ContainsKey(qualifiedName))
            return (T)dict[qualifiedName];

        if (creator == null)
            throw new Exception("ERROR");

        var t = creator();
        dict[qualifiedName] = t;

        return t;
    }


    public string ToFQDN(string name)
    {
        string concat(params string[] names) =>
            String.Join(".", names.Where(n => n != null))
            ;
        var sysName = _system.Name;
        var tasks = _system.Tasks.Select(t => t.Name);
        if (tasks.Any(t => name.StartsWith($"{t}.")))
            return concat(sysName, name);

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


public static class ParserExtension
{
    /// <summary>
    /// DS 문법에서 사용하는 identifier (Segment, flow, call 등의 이름)가 적법한지 검사.
    /// 적법하지 않으면 double quote 로 감싸주어야 한다.
    /// </summary>
    public static bool IsValidIdentifier(this string identifier)
    {
        if (identifier.IsNullOrEmpty())
            throw new ArgumentNullException(nameof(identifier));
        if (identifier == "_")
            return true;

        bool isHangul(char ch) => char.GetUnicodeCategory(ch) == UnicodeCategory.OtherLetter;
        bool isValidStart(char ch) => ch == '_' || char.IsLetter(ch) || isHangul(ch);
        bool isValid(char ch) => isValidStart(ch) || char.IsDigit(ch);

        var chars = identifier.ToCharArray();
        var first = chars[0];

        return isValidStart(first) && chars.Skip(1).ForAll(isValid);
    }
    public static bool IsQuotationRequired(this string identifier) => !IsValidIdentifier(identifier);
}
