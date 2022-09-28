using Engine.Common;

namespace Engine.Parser;


/// <summary>
/// System, Flow,
/// Parenting(껍데기만),
/// Interface 구조까지 생성
/// </summary>
class SkeletonListener : ListenerBase
{
    public SkeletonListener(dsParser parser, ParserHelper helper)
        : base(parser, helper)
    {
    }

    override public void EnterSystem(SystemContext ctx)
    {
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        ICpu cpu = null;    // todo
        _system = DsSystem.Create(name, cpu, _model);
        Trace.WriteLine($"System: {name}");
    }

    override public void EnterFlow(FlowContext ctx)
    {
        var flowName = ctx.identifier1().GetText().DeQuoteOnDemand();
        _rootFlow = Flow.Create(flowName, _system);
    }

    override public void EnterParenting(ParentingContext ctx)
    {
        Trace.WriteLine($"Parenting: {ctx.GetText()}");
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        _parenting = Segment.Create(name, _rootFlow);
    }
}
