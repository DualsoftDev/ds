using Antlr4.Runtime.Misc

using Engine.Common

namespace Engine.Parser.FS


/// <summary>
/// System, Flow, Task, Cpu
/// Parenting(껍데기만),
/// Segment Listing(root flow toplevel 만),
/// CallPrototype, Aliasing 구조까지 생성
/// </summary>
class ListenerBase : dsParserBaseListener
{
    public ParserHelper ParserHelper
    protected Model _model => ParserHelper.Model
    protected DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
    protected Flow _flow { get => ParserHelper._flow; set => ParserHelper._flow = value; }
    protected Real _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
    protected Dictionary<string[], GraphVertexType> _elements => ParserHelper._elements
    protected SpitResult[] _modelSpits { get => ParserHelper._modelSpits; set => ParserHelper._modelSpits = value; }
    protected object[] _modelSpitObjects { get => ParserHelper._modelSpitObjects; set => ParserHelper._modelSpitObjects = value; }

    protected void AddElement(string[] path, GraphVertexType elementType)
    {
        if (_elements.ContainsKey(path))
            _elements[path] |= elementType
        else
            _elements.Add(path, elementType)
    }

    protected string[] AppendPathElement(string name) => ParserHelper.AppendPathElement(name)
    protected string[] AppendPathElement(string[] names) => ParserHelper.AppendPathElement(names)
    protected string[] CurrentPathElements => ParserHelper.CurrentPathElements
    protected void UpdateModelSpits()
    {
        _modelSpits = _model.Spit().ToArray()
        _modelSpitObjects = _modelSpits.Select(spit => spit.GetCore()).ToArray()
    }



    public ListenerBase(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper
        parser.Reset()
    }

    public override void EnterModel(ModelContext ctx)
    {
        UpdateModelSpits()
    }

    override public void EnterSystem(SystemContext ctx)
    {
        let name = ctx.systemName().GetText().DeQuoteOnDemand()
        _system = _model.Systems.First(s => s.Name == name)
    }
    override public void ExitSystem(SystemContext ctx) { this._system = null; }

    override public void EnterFlow(FlowContext ctx)
    {
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        _flow = _system.Flows.First(f => f.Name == flowName)
    }
    override public void ExitFlow(FlowContext ctx) { _flow = null; }



    override public void EnterParenting(ParentingContext ctx)
    {
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        _parenting = (Real)_flow.Graph.Vertices.FindWithName(name)
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }

    //protected IVertex FindVertex(string[] fqdn)
    //{
    //    if (_parenting == null)
    //    {
    //        _flow.Graph.
    //    }
    //}
}
