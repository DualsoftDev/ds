// from cytoscpaeVisitor.ts

using Antlr4.Runtime.Misc;

namespace Engine.Parser;

//enum NodeType = "system" | "task" | "call" | "proc" | "func" | "segment" | "expression" | "conjunction";
enum NodeType {
    system,
    task, call, proc, func, segment, expression, conjunction,
    segmentAlias,
    callAlias,
};

class Node {
    public string id;
    public string label;
    public string parentId;
    public NodeType type;
    public Node(string id, string label, string parentId, NodeType type)
    {
        this.id = id;
        this.label = label;
        this.parentId = parentId;
        this.type = type;
    }
}


partial class ElementsListener : dsBaseListener
{
    #region Boiler-plates
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    DsTask   _task      { get => ParserHelper._task;      set => ParserHelper._task = value; }
    RootFlow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    SegmentBase  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

    string CurrentPath => ParserHelper.CurrentPath;
    Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
    Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

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

    override public void EnterSysTask(SysTaskContext ctx)
    {
        var name = ctx.id().GetText();
        _task = _system.Tasks.First(t => t.Name == name);
        Trace.WriteLine($"Task: {name}");
    }
    override public void ExitSysTask(SysTaskContext ctx) { _task = null; }

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

    public override void EnterSafetyBlock([NotNull] SafetyBlockContext context)
    {
        var safetyDefs = enumerateChildren<SafetyDefContext>(context);
        //foreach (var safetyDef in safetyDefs)
        //{
        //    var key= findFirstChild(safetyDef, t => t is SafetyKeyContext).GetText();
        //    var valueHeader = findFirstChild(safetyDef, t => t is SafetyValuesContext);
        //    var values =
        //        enumerateChildren<SegmentPathNContext>(valueHeader)
        //        .Select(ctx => ctx.GetText())
        //        ;
        //    Console.WriteLine();
        //}

        var kvs = (
            from safetyDef in safetyDefs
            let key = findFirstChild(safetyDef, t => t is SafetyKeyContext).GetText()
            let valueHeader = findFirstChild(safetyDef, t => t is SafetyValuesContext)
            let values = enumerateChildren<SegmentPathNContext>(valueHeader).Select(ctx => ctx.GetText()).ToArray()
            select (key, values)
        ).ToDictionary(tpl => tpl.key, tpl => tpl.values)
            ;



        switch (context.Parent)
        {
            case PropertyBlockContext propBlock:   // global prop safety
                Console.WriteLine(propBlock.ToString());
                break;
            case FlowContext flow:
                Console.WriteLine(flow.ToString());         // in flow safety
                break;
            default:
                throw new Exception("ERROR");
        }

        base.EnterSafetyBlock(context);
    }

    override public void EnterListing(ListingContext ctx) { }

    #endregion Boiler-plates





    /** causal operator 왼쪽 */
    private CausalTokensDNFContext left;
    private CausalOperatorContext op;


    private string flowOfName;      // [flow of A]F={..} -> A
    private List<ParserRuleContext> allParserRules;

    Dictionary<string, Node> nodes = new Dictionary<string, Node>();





    override public void EnterCall(CallContext ctx)
    {
        var name = ctx.id().GetText();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";
        var call = _task.CallPrototypes.First(c => c.Name == name);

        var callph = ctx.callPhrase();
        var txs = ParserHelper.FindObjects<SegmentBase>(callph.segments(0).GetText());
        var rxs = ParserHelper.FindObjects<SegmentBase>(callph.segments(1).GetText());
        call.TXs.AddRange(txs);
        call.RXs.AddRange(rxs);
        //Trace.WriteLine($"Call: {name} = {txs.Select(tx => tx.Name)} ~ {rx?.Name}");
    }


    override public void EnterParenting(ParentingContext ctx) {
        var name = ctx.id().GetText();
        _parenting = (SegmentBase)QpInstanceMap[$"{CurrentPath}.{name}"];
    }
    override public void ExitParenting(ParentingContext ctx) { _parenting = null; }



    override public void EnterCausalPhrase(CausalPhraseContext ctx) {
        this.left = null;
        this.op = null;

        Trace.WriteLine($"CausalPhrase: {ctx.GetText()}");
        var left = ctx.GetChild(0);
        var op = ctx.GetChild(1);
        var rights = ctx.GetChild(2);

        Trace.WriteLine($"\tCausalPhrase all: {left.GetText()}, {op.GetText()}, {rights.GetText()}");


        var names =
            enumerateChildren<SegmentContext>(ctx)
            .Select(segCtx => segCtx.GetText())
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
            foreach (var name in names)
            {
                //var n = ParserHelper.ToFQDN(name);
                var n = name;
                Child child = null;
                bool isAlias = false;
                var fqdn = $"{CurrentPath}.{n}";
                if (QpInstanceMap.ContainsKey(fqdn))
                    continue;

                var nameComponents = n.Split(new[] { '.' }).ToArray();
                string targetName = n;
                switch(nameComponents.Length)
                {
                    case 1:
                        isAlias = ParserHelper.AliasNameMaps[_system].ContainsKey(n);
                        targetName = isAlias ? ParserHelper.AliasNameMaps[_system][n] : n;
                        break;
                    case 2:
                        targetName = $"{_system.Name}.{n}";
                        break;
                    case 3:
                        targetName = n;
                        break;
                    default:
                        throw new Exception("ERROR");
                }

                object target = null;
                if (QpDefinitionMap.ContainsKey(targetName))
                    target = QpDefinitionMap[targetName];   // definition 우선시
                else if (QpInstanceMap.ContainsKey(targetName))
                    target = QpInstanceMap[targetName];

                switch (target)
                {
                    case CallPrototype cp:
                        var subCall = new SubCall(name, _parenting, cp);
                        child = new Child(subCall, _parenting) { IsAlias = isAlias };
                        subCall.ContainerChild = child;
                        QpInstanceMap.Add(fqdn, child);
                        break;
                    case SegmentBase exSeg:
                        var exCall = new ExSegment(name, exSeg);
                        child = new Child(exCall, _parenting) { IsAlias = isAlias };
                        exCall.ContainerChild = child;
                        QpInstanceMap.Add(fqdn, child);
                        break;
                    default:
                        throw new Exception($"ERRROR: Unknown target for {targetName}");
                }
            }
        }
    }
    override public void EnterCausalTokensDNF(CausalTokensDNFContext ctx) {
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
            throw new Exception("Layouts block should exist only once");

        var positionDefs = enumerateChildren<PositionDefContext>(ctx).ToArray();
        foreach(var posiDef in positionDefs)
        {
            var callPath = posiDef.callPath().GetText();
            var cp = (CallPrototype)QpDefinitionMap[callPath];
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
            throw new Exception("Layouts block should exist only once");

        var addressDefs = enumerateChildren<AddressDefContext>(ctx).ToArray();
        foreach (var addrDef in addressDefs)
        {
            var segPath = addrDef.segmentPath().GetText();            
            var seg = (SegmentBase)QpInstanceMap[segPath];
            var sre = addrDef.address();
            var (s, r, e) = (sre.startTag()?.GetText(), sre.resetTag()?.GetText(), sre.endTag()?.GetText());
            seg.Addresses = Tuple.Create(s, r, e);
        }
    }









    // ParseTreeListener<> method
    override public void VisitTerminal(ITerminalNode node)     { return; }
    override public void VisitErrorNode(IErrorNode node)        { return; }
    override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
