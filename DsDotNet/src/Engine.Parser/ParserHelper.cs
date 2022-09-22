using static Engine.Core.CoreClass;
using static Engine.Core.CoreFlow;
using  Engine.Core;
using static Engine.Core.CoreStruct;
using static Engine.Cpu.Cpu;

namespace Engine.Parser;


public class ParserHelper
{
    // button category 중복 check 용
    public HashSet<(ParserSystem, string)> ButtonCategories = new();

    public ParserModel Model { get; } = new ParserModel();
    internal ParserSystem _system;
    internal ParserRootFlow _rootFlow;
    internal ParserSegment _parenting;

    public Dictionary<string, ParserCpu> FlowName2CpuMap;

    public ParserOptions ParserOptions { get; set; }
    public ParserHelper(ParserOptions options)
    {
        ParserOptions = options;
    }


    internal string[] GetCurrentPathComponents(string lastName) =>
        CurrentPathNameComponents.Append(lastName).ToArray();

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

    public T[] FindObjects<T>(ParserSystem system, ParserRootFlow flow, string qualifiedNames) where T : class
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
            if (ParserOptions.AllowSkipExternalParserSegment)
                return null;
            throw new Exception($"ERROR: failed to create {qualifiedName}");
        }

        var t = creator();
        Model.FindFlow(qualifiedName).InstanceMap[qualifiedName.Last()] = t;

        return t;
    }
}


