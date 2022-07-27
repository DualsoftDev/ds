// from cytoscpaeVisitor.ts

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using Engine.Common;
using Engine.Core;

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
    Segment  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }

    string CurrentPath => ParserHelper.CurrentPath;
    Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
    Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

    public ElementsListener(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper;

        this.allParserRules = DsParser.getAllParseRules(parser);
        parser.Reset();
    }


    override public void EnterSystem(dsParser.SystemContext ctx)
    {
        var name = ctx.id().GetText();
        _system = _model.Systems.First(s => s.Name == name);
    }
    override public void ExitSystem(dsParser.SystemContext ctx) { this._system = null; }

    override public void EnterTask(dsParser.TaskContext ctx)
    {
        var name = ctx.id().GetText();
        _task = _system.Tasks.First(t => t.Name == name);
        Trace.WriteLine($"Task: {name}");
    }
    override public void ExitTask(dsParser.TaskContext ctx) { _task = null; }

    override public void EnterFlow(dsParser.FlowContext ctx)
    {
        var flowName = ctx.id().GetText();
        _rootFlow = _system.RootFlows.First(f => f.Name == flowName);

        var flowOf = ctx.flowProp().id();
        this.flowOfName = flowOf == null ? flowName : flowOf.GetText();
    }
    override public void ExitFlow(dsParser.FlowContext ctx)
    {
        _rootFlow = null;
        flowOfName = null;
    }

    override public void EnterListing(dsParser.ListingContext ctx) { }

    #endregion Boiler-plates





    /** causal operator 왼쪽 */
    private dsParser.CausalTokensDNFContext left;
    private dsParser.CausalOperatorContext op;


    private string flowOfName;      // [flow of A]F={..} -> A
    private List<ParserRuleContext> allParserRules;

    Dictionary<string, Node> nodes = new Dictionary<string, Node>();





    override public void EnterCall(dsParser.CallContext ctx)
    {
        var name = ctx.id().GetText();
        var label = $"{name}\n{ctx.callPhrase().GetText()}";
        var call = _task.CallPrototypes.First(c => c.Name == name);

        var callph = ctx.callPhrase();
        var txs = ParserHelper.FindObjects<Segment>(callph.segments(0).GetText());
        var rxs = ParserHelper.FindObjects<Segment>(callph.segments(1).GetText());
        call.TXs.AddRange(txs);
        call.RXs.AddRange(rxs);
        //Trace.WriteLine($"Call: {name} = {txs.Select(tx => tx.Name)} ~ {rx?.Name}");
    }


    override public void EnterParenting(dsParser.ParentingContext ctx) {
        var name = ctx.id().GetText();
        _parenting = (Segment)QpInstanceMap[$"{CurrentPath}.{name}"];
    }
    override public void ExitParenting(dsParser.ParentingContext ctx) { _parenting = null; }



    override public void EnterCausalPhrase(dsParser.CausalPhraseContext ctx) {
        this.left = null;
        this.op = null;

        Trace.WriteLine($"CausalPhrase: {ctx.GetText()}");
        var left = ctx.GetChild(0);
        var op = ctx.GetChild(1);
        var rights = ctx.GetChild(2);

        Trace.WriteLine($"\tCausalPhrase all: {left.GetText()}, {op.GetText()}, {rights.GetText()}");


        var names =
            DsParser.enumerateChildren<dsParser.SegmentContext>(
                ctx, false, r => r is dsParser.SegmentContext)
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
                    case Segment exSeg:
                        var exCall = new ExSegmentCall(name, exSeg);
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
    override public void EnterCausalTokensDNF(dsParser.CausalTokensDNFContext ctx) {
        if (this.left != null)
        {
            Debug.Assert(this.op != null);  //, 'operator expected');

            Trace.WriteLine($"CausalTokensDNF per operator: {left.GetText()} + {op.GetText()} + {ctx.GetText()}");

            // process operator
            this.processCausal(this.left, this.op, ctx);
        }

        this.left = ctx;
    }
    override public void EnterCausalOperator(dsParser.CausalOperatorContext ctx) { this.op = ctx; }

    override public void ExitProgram(dsParser.ProgramContext ctx)
    {
        foreach(var cpu in _model.Cpus)
        {
            foreach(var seg in cpu.RootFlows.SelectMany(rf => rf.ChildVertices).OfType<Segment>())
            {
                seg.TagGoing = new Tag(cpu, seg, $"{seg.QualifiedName}_Going") { Type = TagType.Going };
                var ports = new Port[] { seg.PortS, seg.PortR, seg.PortE, };
                ports.Iter(p => p.OwnerCpu = cpu);
            }
        }
    }









    // ParseTreeListener<> method
    override public void VisitTerminal(ITerminalNode node)     { return; }
    override public void VisitErrorNode(IErrorNode node)        { return; }
    override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
