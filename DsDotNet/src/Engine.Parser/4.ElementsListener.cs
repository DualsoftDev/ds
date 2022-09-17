// from cytoscpaeVisitor.ts

using Antlr4.Runtime.Misc;

namespace Engine.Parser;

//enum NodeType = "system" | "task" | "call" | "proc" | "func" | "segment" | "expression" | "conjunction";
enum NodeType
{
    system,
    task, call, proc, func, segment, expression, conjunction,
    segmentAlias,
    callAlias,
};

abstract class NodeBase
{
    public string label;
    public string[] parentIds;
    public NodeType type;
    public NodeBase(string label, string[] parentIds, NodeType type)
    {
        this.label = label;
        this.parentIds = parentIds;
        this.type = type;
    }
}

class Node : NodeBase
{
    public string[] ids;
    public Node(string[] ids, string label, string[] parentIds, NodeType type)
        : base(label, parentIds, type)
    {
        this.ids = ids;
    }

}
class NodeConjunction : NodeBase
{
    public string[][] idss;
    public NodeConjunction(string[][] idss, string label, string[] parentIds, NodeType type)
        : base(label, parentIds, type)
    {
        this.idss = idss;
    }
}


partial class ElementsListener : dsBaseListener
{
    #region Boiler-plates
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    RootFlow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    SegmentBase  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

    string[] CurrentPathNameComponents => ParserHelper.CurrentPathNameComponents;
    string CurrentPath => ParserHelper.CurrentPath;
    Dictionary<(DsSystem, string), object> QpInstanceMap => ParserHelper.QpInstanceMap;
    Dictionary<(DsSystem, string), object> QpDefinitionMap => ParserHelper.QpDefinitionMap;

    public ElementsListener(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper;

        this.allParserRules = getAllParseRules(parser);
        parser.Reset();
    }


    override public void EnterSystem(SystemContext ctx)
    {
        var name = ctx.id().GetText();
        _system = _model.Systems.First(s => s.Name == name);
    }
    override public void ExitSystem(SystemContext ctx) { this._system = null; }

    override public void EnterFlow(FlowContext ctx)
    {
        var flowName = ctx.id().GetText();
        _rootFlow = _system.RootFlows.First(f => f.Name == flowName);

        var flowOf = ctx.flowProp().id();
        this.flowOfName = flowOf == null ? flowName : flowOf.GetText();
    }
    override public void ExitFlow(FlowContext ctx)
    {
        _rootFlow = null;
        flowOfName = null;
    }


    override public void EnterListing(ListingContext ctx) { }

    #endregion Boiler-plates





    /** causal operator 왼쪽 */
    private CausalTokensDNFContext left;
    private CausalOperatorContext op;


    private string flowOfName;      // [flow of A]F={..} -> A
    private List<ParserRuleContext> allParserRules;

    Dictionary<string, NodeBase> nodes = new Dictionary<string, NodeBase>();



    public override void EnterSafetyBlock([NotNull] SafetyBlockContext ctx)
    {
        var safetyDefs = enumerateChildren<SafetyDefContext>(ctx);
        /*
         * safety block 을 parsing 해서 key / value 의 dictionary 로 저장
         * 
        [safety] = {
            Main = {P.F.Sp; P.F.Sm}
            Main2 = {P.F.Sp; P.F.Sm}
        }
        => "Main" = {"P.F.Sp"; "P.F.Sm"}
           "Main2" = {"P.F.Sp"; "P.F.Sm"}
         */
        var safetyKvs =
            from safetyDef in safetyDefs
            let key = findFirstChild(safetyDef, t => t is SafetyKeyContext).GetText()
            let valueHeader = enumerateChildren<SafetyValuesContext>(safetyDef).First()
            let values = enumerateChildren<SegmentPathNContext>(valueHeader).Select(ctx => ctx.GetText()).ToArray()
            select (key, values)
            ;

        SegmentBase getKey(string segPath)
        {
            // global prop safety
            if (ctx.Parent is PropertyBlockContext && QpInstanceMap.ContainsKey((_system, segPath)))
                return (SegmentBase)QpInstanceMap[(_system, segPath)];

            // in flow safety
            var key = $"{CurrentPath}.{segPath}";
            if (ctx.Parent is FlowContext && QpInstanceMap.ContainsKey((_system, key)))
                return (SegmentBase)QpInstanceMap[(_system, key)];

            return null;
        }

        foreach (var (key, values) in safetyKvs)
        {
            var keySegment = getKey(key);
            keySegment.SafetyConditions = values.Select(safety => (SegmentBase)QpInstanceMap[(_system, safety)]).ToArray();
        }
    }


    override public void EnterCall(CallContext ctx)
    {
        var name = ctx.id().GetText();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";

        var callPrototypes = _rootFlow.CallPrototypes;
        var call = callPrototypes.First(c => c.Name == name);

        var callph = ctx.callPhrase();
        SegmentBase[] findSegments(SegmentsContext txrxCtx)
        {
            var nss = enumerateChildren<SegmentContext>(txrxCtx).Select(collectNameComponents);
            return (
                from ns in nss
                let sys = _model.Systems.First(sys => sys.Name == ns[0])
                select ParserHelper.FindObject<SegmentBase>(sys, ns.Combine())
            ).ToArray();
        }

        var txs = findSegments(callph.segments(0));
        var rxs = findSegments(callph.segments(1));

        if (ParserHelper.ParserOptions.AllowSkipExternalSegment)
        {
            txs = txs.Where(t => t != null).ToArray();
            rxs = rxs.Where(t => t != null).ToArray();
        }
        call.TXs.AddRange(txs);
        call.RXs.AddRange(rxs);
        //Trace.WriteLine($"Call: {name} = {txs.Select(tx => tx.Name)} ~ {rx?.Name}");
    }


    override public void EnterParenting(ParentingContext ctx)
    {
        var name = ctx.id().GetText();
        _parenting = (SegmentBase)QpInstanceMap[(_system, $"{CurrentPath}.{name}")];
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }



    override public void EnterCausalPhrase(CausalPhraseContext ctx)
    {
        this.left = null;
        this.op = null;

        Trace.WriteLine($"CausalPhrase: {ctx.GetText()}");
        var left = ctx.GetChild(0);
        var op = ctx.GetChild(1);
        var rights = ctx.GetChild(2);

        Trace.WriteLine($"\tCausalPhrase all: {left.GetText()}, {op.GetText()}, {rights.GetText()}");


        var nameComponentss =
            enumerateChildren<SegmentContext>(ctx)
            .Select(collectNameComponents)
            .ToArray()
            ;
        if (_parenting == null)
        {
            /*
             * See ModelListener.EnterCausalPhrase()
             */
        }
        else
        {
            foreach (var ns in nameComponentss)
            {
                var n = ns.Combine();
                Child child = null;
                bool isAlias = false;
                var fqdn = $"{CurrentPath}.{n}";
                if (QpInstanceMap.ContainsKey((_system, fqdn)))
                    continue;

                string targetName = n;
                var key = (_system, n);
                switch (ns.Length)
                {
                    case 1:
                        isAlias = ParserHelper.AliasNameMaps[_system].ContainsKey(n);
                        if (isAlias)
                            key = (_system, ParserHelper.AliasNameMaps[_system][n]);
                        break;
                    case 2:
                        key = (_system, $"{_system.Name}.{n}");
                        break;
                    case 3:
                        var exsys = _model.Systems.First(sys => targetName.StartsWith($"{sys.Name}."));
                        key = (exsys, n);
                        break;
                    default:
                        throw new ParserException($"ERROR: {targetName} length error.", ctx);
                }

                object target = null;
                if (QpDefinitionMap.ContainsKey(key))
                    target = QpDefinitionMap[key];   // definition 우선시
                else if (QpInstanceMap.ContainsKey(key))
                    target = QpInstanceMap[key];
                else if (_rootFlow != null)
                {
                    if (_rootFlow.CallPrototypes.Exists(cp => cp.Name == targetName))
                        target = _rootFlow.CallPrototypes.First(cp => cp.Name == targetName);
                }

                switch (target)
                {
                    case CallPrototype cp:
                        var subCall = new SubCall(n, _parenting, cp);
                        child = new Child(subCall, _parenting) { IsAlias = isAlias };
                        subCall.ContainerChild = child;
                        QpInstanceMap.Add((_system, fqdn), child);
                        break;
                    case SegmentBase exSeg:
                        var exCall = new ExSegment(n, exSeg);
                        child = new Child(exCall, _parenting) { IsAlias = isAlias };
                        exCall.ContainerChild = child;
                        QpInstanceMap.Add((_system, fqdn), child);
                        break;
                    default:
                        throw new ParserException($"ERRROR: Unknown target for {targetName}", ctx);
                }
            }
        }
    }
    override public void EnterCausalTokensDNF(CausalTokensDNFContext ctx)
    {
        if (this.left != null)
        {
            Assert(this.op != null);  //, 'operator expected');

            Trace.WriteLine($"CausalTokensDNF per operator: {left.GetText()} + {op.GetText()} + {ctx.GetText()}");

            // process operator
            this.processCausal(this.left, this.op, ctx);
        }

        this.left = ctx;
    }
    override public void EnterCausalOperator(CausalOperatorContext ctx) { this.op = ctx; }

    override public void ExitProgram(ProgramContext ctx)
    {
        //[layouts] = {
        //       L.T.Cp = (30, 50)            // xy
        //       L.T.Cm = (60, 50, 20, 20)    // xywh
        //}

        var layouts = enumerateChildren<LayoutsContext>(ctx).ToArray();
        if (layouts.Length > 1)
            throw new ParserException("Layouts block should exist only once", ctx);

        var positionDefs = enumerateChildren<PositionDefContext>(ctx).ToArray();
        foreach (var posiDef in positionDefs)
        {
            var callPath = posiDef.callPath().GetText();
            var cp = (CallPrototype)QpDefinitionMap[(_system, callPath)];
            var xywh = posiDef.xywh();
            var (x, y, w, h) = (xywh.x().GetText(), xywh.y().GetText(), xywh.w()?.GetText(), xywh.h()?.GetText());
            cp.Xywh = new Xywh(int.Parse(x), int.Parse(y), w == null ? null : int.Parse(w), h == null ? null : int.Parse(h));
        }

        //[addresses] = {
        //    A.F.Am = (%Q123.23, , %I12.1);        // FQSegmentName = (Start, Reset, End) Tag address
        //    A.F.Ap = (%Q123.24, , %I12.2);
        //    B.F.Bm = (%Q123.25, , %I12.3);
        //    B.F.Bp = (%Q123.26, , %I12.4);
        //}
        var addresses = enumerateChildren<AddressesContext>(ctx).ToArray();
        if (addresses.Length > 1)
            throw new ParserException("Layouts block should exist only once", ctx);

        var addressDefs = enumerateChildren<AddressDefContext>(ctx).ToArray();
        foreach (var addrDef in addressDefs)
        {
            var segNs = collectNameComponents(addrDef.segmentPath());
            var segPath = segNs.Combine();
            var sys = _model.Systems.First(sys => sys.Name == segNs[0]);
            var seg = (SegmentBase)QpInstanceMap[(sys, segPath)];
            var sre = addrDef.address();
            var (s, r, e) = (sre.startTag()?.GetText(), sre.resetTag()?.GetText(), sre.endTag()?.GetText());
            seg.Addresses = Tuple.Create(s, r, e);
        }
    }









    // ParseTreeListener<> method
    override public void VisitTerminal(ITerminalNode node) { return; }
    override public void VisitErrorNode(IErrorNode node) { return; }
    override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
