

namespace Engine.Parser;


/// <summary>
/// System, Flow, Parenting(껍데기만),
/// Interface name map 구조까지 생성
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

    public override void EnterAliasListing(AliasListingContext ctx)
    {
        var map = _rootFlow.AliasMap;

        var aliasDef = findFirstChild<AliasDefContext>(ctx);
        var alias = collectNameComponents(aliasDef);

        var mnemonics =
            enumerateChildren<AliasMnemonicContext>(ctx)
                .Select(mctx => collectNameComponents(mctx))
                .Pipe(mne => Assert(mne.Length == 1))
                .Select(mne => mne[0])
                .ToHashSet();
        map.Add(alias, mnemonics);
    }


    public override void EnterInterfaces([NotNull] InterfacesContext ctx)
    {
        _system.Api = new Api(_system);
    }


    public override void EnterInterfaceDef([NotNull] InterfaceDefContext ctx)
    {
        var hash = _system.Api.Items;
        var interrfaceNameCtx = findFirstChild<InterfaceNameContext>(ctx);
        var interfaceName = collectNameComponents(interrfaceNameCtx)[0];
        var serCtx = enumerateChildren<CallComponentsContext>(ctx);

        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 다음 listener stage 에서 채움..
        var api = ApiItem.Create(interfaceName, _system);
        hash.Add(api);
    }

    public override void EnterInterfaceResetDef([NotNull] InterfaceResetDefContext ctx)
    {
        var operands =
            enumerateChildren<Identifier1Context>(ctx)
            .Select(ctx => ctx.GetText())
            .ToArray();
        var operator_ = findFirstChild<CausalOperatorResetContext>(ctx).GetText();
        var ri_ = ApiResetInfo.Create(_system, operands[0], operator_, operands[1]);
    }
}
