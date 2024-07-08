// from cytoscpaeVisitor.ts

using Antlr4.Runtime.Misc;

namespace Engine.Parser;

//enum NodeType = "system" | "task" | "call" | "proc" | "func" | "segment" | "expression" | "conjunction";
enum NodeType
{
    system,
    task, call, proc, func, segment, expression, conjunction,
    segmentAlias,
    externalSegmentCall,
    callAlias,
};

abstract class NodeBase
{
    public string[] parentIds;
    public NodeType type;
    public NodeBase(string[] parentIds, NodeType type)
    {
        this.parentIds = parentIds;
        this.type = type;
    }
    public abstract string GetLabel();
}

class Node : NodeBase
{
    public string[] ids;
    public string label;
    public Node(string[] ids, string label, string[] parentIds, NodeType type)
        : base(parentIds, type)
    {
        Assert(ids.Length <= 4);        // MAX: Sys > Flow > Parenting > Name
        this.ids = ids;
        this.label = label;
    }
    public override string GetLabel() => label;
}
class NodeConjunction : NodeBase
{
    public string[][] idss;
    public string[] labels;
    public NodeConjunction(string[][] idss, string[] labels, string[] parentIds, NodeType type)
        : base(parentIds, type)
    {
        this.idss = idss;
        this.labels = labels;
    }
    public override string GetLabel() => string.Join(", ", labels);
}


partial class ElementsListener : dsBaseListener
{
    #region Boiler-plates
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    Flow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    Segment  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

    public ElementsListener(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper;

        this.allParserRules = getAllParseRules(parser);
        parser.Reset();
    }


    override public void EnterSystem(SystemContext ctx)
    {
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        _system = _model.Systems.First(s => s.Name == name);
    }
    override public void ExitSystem(SystemContext ctx) { this._system = null; }

    override public void EnterFlow(FlowBlockContext ctx)
    {
        var flowName = ctx.identifier1().GetText().DeQuoteOnDemand();
        _rootFlow = _system.Flows.First(f => f.Name == flowName);

        var flowOf = ctx.flowProp().identifier1();
        this.flowOfName = flowOf == null ? flowName : flowOf.GetText();
    }
    override public void ExitFlow(FlowBlockContext ctx)
    {
        _rootFlow = null;
        flowOfName = null;
    }

    #endregion Boiler-plates





    /** causal operator 왼쪽 */
    private CausalTokensDNFContext left;
    private CausalOperatorContext op;


    private string flowOfName;      // [flow of A]F={..} -> A
    private List<ParserRuleContext> allParserRules;

    Dictionary<string, NodeBase> nodes = new Dictionary<string, NodeBase>();



    public override void EnterSafetyBlock([NotNull] SafetyBlockContext ctx)
    {
        var safetyDefs = Descendants<SafetyDefContext>(ctx);
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
            let key = CollectNameComponents(TryFindFirstChild(safetyDef, t => t is SafetyKeyContext))   // ["Main"] or ["My", "Flow", "Main"]
            let valueHeader = Descendants<SafetyValuesContext>(safetyDef).First()
            let values = Descendants<Identifier123Context>(valueHeader).Select(CollectNameComponents).ToArray()
            select (key, values)
            ;

        Segment getKey(string[] segPath)
        {
            switch(ctx.Parent)
            {
                // global prop safety
                case PropertyBlockContext:
                    return _model.FindFirst<Segment>(segPath);

                // in flow safety
                case FlowBlockContext:
                    return (Segment)_rootFlow.InstanceMap[segPath[0]];

                default:
                    throw new Exception("ERROR");
            }
        }

        foreach (var (key, values) in safetyKvs)
        {
            var keySegment = getKey(key);
            keySegment.SafetyAutoPreConditions = values.Select(safety => _model.FindFirst<Segment>(safety)).ToArray();
        }
    }


    override public void EnterCallDef(CallDefContext ctx)
    {
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";

        var callPrototypes = _rootFlow.CallPrototypes;
        var call = callPrototypes.First(c => c.Name == name);

        var callph = ctx.callPhrase();
        Segment[] findSegments(ParserRuleContext txrxCtx)
        {
            if (txrxCtx == null || txrxCtx.GetText() == "_")
                return Array.Empty<Segment>();

            var nss = Descendants<Identifier123Context>(txrxCtx).Select(CollectNameComponents).ToArray();
            return nss.Select(ns => _model.FindFirst<Segment>(ns)).ToArray();
        }

        var txs = findSegments(callph.callComponents(0));
        var rxs = findSegments(callph.callComponents(1));
        var resets = findSegments(callph.callComponents(2));

        if (ParserHelper.ParserOptions.AllowSkipExternalSegment)
        {
            txs = txs.Where(t => t != null).ToArray();
            rxs = rxs.Where(t => t != null).ToArray();
        }

        string concat(IEnumerable<string> xs) => string.Join(", ", xs);

        var txDup = txs.Cast<Segment>().Select(x => x.QualifiedName).FindDuplicates();
        if (txDup.Any())
            throw new Exception($"Duplicated TXs [{concat(txDup)}] near {ctx.GetText()}");
        var rxDup = rxs.Cast<Segment>().Select(x => x.QualifiedName).FindDuplicates();
        if (rxDup.Any())
            throw new Exception($"Duplicated RXs [{concat(rxDup)}] near {ctx.GetText()}");

        call.TXs.AddRange(txs);
        call.RXs.AddRange(rxs);
        call.Resets.AddRange(resets);
        //Debug.WriteLine($"Call: {name} = {txs.Select(tx => tx.Name)} ~ {rx?.Name}");
    }


    override public void EnterParenting(ParentingContext ctx)
    {
        var name = ctx.identifier1().GetText().DeQuoteOnDemand();
        _parenting = (Segment)_rootFlow.InstanceMap[name];
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }



    override public void EnterCausalPhrase(CausalPhraseContext ctx)
    {
        this.left = null;
        this.op = null;
    }
    override public void EnterCausalTokensDNF(CausalTokensDNFContext ctx)
    {
        if (this.left != null)
        {
            Assert(this.op != null);  //, 'operator expected');

            Debug.WriteLine($"CausalTokensDNF per operator: {left.GetText()} + {op.GetText()} + {ctx.GetText()}");

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

        var layouts = Descendants<LayoutsContext>(ctx).ToArray();
        if (layouts.Length > 1)
            throw new ParserException("Layouts block should exist only once", ctx);

        var positionDefs = Descendants<PositionDefContext>(ctx).ToArray();
        foreach (var posiDef in positionDefs)
        {
            var callPath = CollectNameComponents(posiDef.callPath());
            var cp = _model.FindFirst<CallPrototype>(callPath);
            var xywh = posiDef.xywh();
            var (x, y, w, h) = (xywh.x().GetText(), xywh.y().GetText(), xywh.w()?.GetText(), xywh.h()?.GetText());
            cp.Xywh = new Xywh(int.Parse(x), int.Parse(y), w == null ? null : int.Parse(w), h == null ? null : int.Parse(h));
        }

        //[addresses] = {
        //    A.F.Am = (%I12.1, , %Q123.23);        // FQSegmentName = (Start, Reset, End) Tag address
        //    A.F.Ap = (%I12.2, , %Q123.24);
        //    B.F.Bm = (%I12.3, , %Q123.25);
        //    B.F.Bp = (%I12.4, , %Q123.26);
        //}
        var addresses = Descendants<AddressesContext>(ctx).ToArray();
        if (addresses.Length > 1)
            throw new ParserException("Layouts block should exist only once", ctx);

        var addressDefs = Descendants<AddressDefContext>(ctx).ToArray();
        foreach (var addrDef in addressDefs)
        {
            var segNs = CollectNameComponents(addrDef.segmentPath());
            var seg = _model.FindFirst<Segment>(segNs);
            var sre = addrDef.address();
            var (s, r, e) = (sre.startTag()?.GetText(), sre.resetTag()?.GetText(), sre.endTag()?.GetText());
            seg.Addresses = Tuple.Create(s, r, e);
        }
    }









    //// ParseTreeListener<> method
    //override public void VisitTerminal(ITerminalNode node) { return; }
    //override public void VisitErrorNode(IErrorNode node) { return; }
    //override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    //override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
