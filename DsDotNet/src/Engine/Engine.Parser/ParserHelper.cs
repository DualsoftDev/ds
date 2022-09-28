namespace Engine.Parser;


public class ParserHelper
{
    // button category 중복 check 용
    public HashSet<(DsSystem, string)> ButtonCategories = new();

    public Model Model { get; } = new Model();
    internal DsSystem _system;
    internal Flow _rootFlow;
    internal Segment _parenting;

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
}


