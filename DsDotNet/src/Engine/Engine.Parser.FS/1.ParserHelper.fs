namespace Engine.Parser.FS


public class AliasTarget {}

public class AliasTargetWithFqdn: AliasTarget {
    public AliasTargetWithFqdn(string[] targetFqdn)
    {
        TargetFqdn = targetFqdn
    }

    public string[] TargetFqdn { get; set; }
}
public class AliasTargetReal : AliasTargetWithFqdn
{
    public AliasTargetReal(string[] targetFqdn) : base(targetFqdn) {}
}

public class AliasTargetDirectCall : AliasTargetWithFqdn
{
    public AliasTargetDirectCall(string[] targetFqdn) : base(targetFqdn) { }
}



public class AliasTargetApi : AliasTarget
{
    public AliasTargetApi(ApiItem apiItem)
    {
        ApiItem = apiItem
    }

    public ApiItem ApiItem { get; set; }
}


public class AliasCreator
{
    public AliasCreator(string name, ParentWrapper parent, AliasTarget target)
    {
        Name = name
        Parent = parent
        Target = target
    }

    public string Name { get; set; }
    public ParentWrapper Parent { get; set; }
    public AliasTarget Target { get; set; }
}


public class ParserHelper
{
    // button category 중복 check 용
    public HashSet<(DsSystem, string)> ButtonCategories = new()

    public Model Model { get; } = new Model()
    internal DsSystem _system
    internal Flow _flow
    internal Real _parenting
    internal Dictionary<string[], GraphVertexType> _elements = new (NameUtil.CreateNameComponentsComparer())
    internal SpitResult[] _modelSpits
    internal object[] _modelSpitObjects

    // 3.2.ElementListener 에서 Alias create 사용
    public List<AliasCreator> AliasCreators = new()

    public ParserOptions ParserOptions { get; set; }
    public ParserHelper(ParserOptions options)
    {
        ParserOptions = options
    }


    internal string[] AppendPathElement(string lastName) =>
        CurrentPathElements.Append(lastName).ToArray()
    internal string[] AppendPathElement(string[] lastNames) =>
        CurrentPathElements.Concat(lastNames).ToArray()

    internal string[] CurrentPathElements
    {
        get
        {
            IEnumerable<string> helper()
            {
                if (_system != null)
                    yield return _system.Name
                if (_flow != null)
                    yield return _flow.Name
                if (_parenting != null)
                    yield return _parenting.Name
            }
            return helper().ToArray()
        }
    }
    internal string CurrentPath => CurrentPathElements.Combine()
}


