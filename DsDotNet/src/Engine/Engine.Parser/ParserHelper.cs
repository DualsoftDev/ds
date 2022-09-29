namespace Engine.Parser;

using System.Runtime.InteropServices;

using static Engine.Core.TextUtil;

//using TagDic = System.Collections.Generic.Dictionary<string, Engine.Core.Tag>;
//class AliasKey : Named
//{
//    public AliasKey(string name) : base(name)
//    {
//    }
//}

public class ParserHelper
{
    // button category 중복 check 용
    public HashSet<(DsSystem, string)> ButtonCategories = new();

    public Model Model { get; } = new Model();
    internal DsSystem _system;
    internal Flow _rootFlow;
    internal Segment _parenting;
    internal HashSet<string[]> _paths = new HashSet<string[]>(NameUtil.CreateNameComponentsComparer());

    public ParserOptions ParserOptions { get; set; }
    public ParserHelper(ParserOptions options)
    {
        ParserOptions = options;
    }


    internal string[] AppendPathElement(string lastName) =>
        CurrentPathElements.Append(lastName).ToArray();

    internal string[] CurrentPathElements
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
    internal string CurrentPath => CurrentPathElements.Combine();
}


