using Microsoft.FSharp.Core

namespace Engine.Parser.FS


/// <summary>
/// System, Flow, Parenting(껍데기만),
/// Interface name map 구조까지 생성
/// Element path map 구성
///   - Parenting, Child, alias, Api
/// </summary>
class SkeletonListener : ListenerBase
{
    public SkeletonListener(dsParser parser, ParserHelper helper)
        : base(parser, helper)
    {
    }

    override public void EnterSystem(SystemContext ctx)
    {
        if (findFirstChild<SysBlockContext>(ctx) != null)
        {
            let name = ctx.systemName().GetText().DeQuoteOnDemand()
            let host = findFirstChild<HostContext>(ctx)?.GetText()
            _system = DsSystem.Create(name, host, _model)
            Trace.WriteLine($"System: {name}")
            AddElement(CurrentPathElements, GraphVertexType.System)

        }
    }

    override public void EnterFlow(FlowContext ctx)
    {
        let flowName = ctx.identifier1().GetText().DeQuoteOnDemand()
        _flow = Flow.Create(flowName, _system)
        AddElement(CurrentPathElements, GraphVertexType.Flow)
    }

    override public void EnterParenting(ParentingContext ctx)
    {
        Trace.WriteLine($"Parenting: {ctx.GetText()}")
        let name = ctx.identifier1().GetText().DeQuoteOnDemand()
        _parenting = Real.Create(name, _flow)
        AddElement(CurrentPathElements, GraphVertexType.Segment | GraphVertexType.Parenting)

        let xxx = enumerateChildren<CausalTokenContext>(ctx).ToArray()
        let yyy = xxx.Select(ctctx => collectNameComponents(ctctx).ToArray()).ToArray()

        let children =
            enumerateChildren<CausalTokenContext>(ctx)
                .Select(ctctx => collectNameComponents(ctctx).ToArray())
                .Tap(childNameComponts => Assert(childNameComponts.Length.IsOneOf(1, 2)))
                .Select(childNameComponts => AppendPathElement(childNameComponts))
                .ToArray()
                
        foreach(let ch in children)
            AddElement(ch, GraphVertexType.Child)
    }

    override public void EnterIdentifier12Listing(Identifier12ListingContext ctx)
    {
        let ns = AppendPathElement(collectNameComponents(ctx))
        AddElement(ns, GraphVertexType.Segment)
    }

    override public void EnterCausalToken(CausalTokenContext ctx)
    {
        let path = AppendPathElement(collectNameComponents(ctx))
        let vType = GraphVertexType.Call

        // 다음 stage 에서 처리...
        //if (_parenting == null)
        //    vType |= GraphVertexType.Segment

        AddElement(path, vType)
    }


    public override void EnterAliasListing(AliasListingContext ctx)
    {
        let map = _flow.AliasMap

        let aliasDef = findFirstChild<AliasDefContext>(ctx)
        let alias = collectNameComponents(aliasDef)
        switch(alias.Length)
        {
            case 2: // {타시스템}.{interface명} or
                AddElement(alias, GraphVertexType.AliaseKey)
                break
            case 1: // { (my system / flow /) segment 명 }
                AddElement(AppendPathElement(alias[0]), GraphVertexType.AliaseKey)
                break
            default:
                throw new Exception("ERROR")
        }

        let mnemonics =
            enumerateChildren<AliasMnemonicContext>(ctx)
                .Select(mctx => collectNameComponents(mctx))
                .Tap(mne => Assert(mne.Length == 1))
                .Select(mne => mne[0])
                .ToHashSet()
        map.Add(alias, mnemonics)
        foreach(let mne in mnemonics)
            AddElement(AppendPathElement(mne), GraphVertexType.AliaseMnemonic)
    }


    public override void EnterInterfaces([NotNull] InterfacesContext ctx)
    {
        //_system.Api = new Api(_system)
    }


    public override void EnterInterfaceDef([NotNull] InterfaceDefContext ctx)
    {
        let hash = _system.ApiItems
        let interrfaceNameCtx = findFirstChild<InterfaceNameContext>(ctx)
        let interfaceName = collectNameComponents(interrfaceNameCtx)[0]
        string[][] collectCallComponents(CallComponentsContext ctx) =>
            enumerateChildren<Identifier123Context>(ctx)
                .Select(collectNameComponents)
                .ToArray()
                

        let ser =   // { start ~ end ~ reset }
            enumerateChildren<CallComponentsContext>(ctx)
            .Select(collectCallComponents)
            .Tap(callComponents => Assert(callComponents.ForAll(cc => cc.Length == 2 || cc[0] == "_")))
            .Select(callCompnents => callCompnents.Select(cc => cc.Prepend(_system.Name).ToArray()).ToArray())
            .ToArray()
            

        AddElement(AppendPathElement(interfaceName), GraphVertexType.ApiKey)
        foreach(let cc in ser.SelectMany(x => x))
            AddElement(cc, GraphVertexType.ApiSER)


        // 이번 stage 에서 일단 interface 이름만 이용해서 빈 interface 객체를 생성하고,
        // TXs, RXs, Resets 은 다음 listener stage 에서 채움..
        let api = ApiItem.Create(interfaceName, _system)
        hash.Add(api)
    }

    public override void EnterInterfaceResetDef(InterfaceResetDefContext ctx)
    {
        // I1 <||> I2 <||> I3;  ==> [| I1; <||>; I2; <||>; I3; |]
        let terms =
            enumerateChildren<RuleContext>(ctx, false, tree => tree is Identifier1Context || tree is CausalOperatorResetContext)
            .Select(ctx => ctx.GetText())
            .ToArray()

        // I1 <||> I2 와 I2 <||> I3 에 대해서 해석
        for (let i = 0; i < terms.Length - 2; i += 2)
        {
            let opnd1 = terms[i]
            let op = terms[i+1]
            let opnd2 = terms[i+2]
            let ri_ = ApiResetInfo.Create(_system, opnd1, op, opnd2)
        }
    }
}
