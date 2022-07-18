using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using Engine.Core;

namespace Engine.Parser
{
    class ModelListener : dsBaseListener
    {
        public ParserHelper ParserHelper;
        Model    _model => ParserHelper.Model;
        DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
        DsTask   _task      { get => ParserHelper._task;      set => ParserHelper._task = value; }
        RootFlow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
        Segment  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
        /// <summary> Qualified Path Map </summary>
        Dictionary<string, object> QpMap => ParserHelper.QualifiedPathMap;

        string CurrentPath => ParserHelper.CurrentPath;

        public ModelListener(dsParser parser, ParserHelper helper)
        {
            ParserHelper = helper;
            parser.Reset();
        }


        override public void EnterSystem(dsParser.SystemContext ctx)
        {
            var n = ctx.id().GetText();
            _system = new DsSystem(n, _model);
            ParserHelper.AliasNameMaps.Add(_system, new Dictionary<string, string>());
            ParserHelper.BackwardAliasMaps.Add(_system, new Dictionary<string, string[]>());
            Trace.WriteLine($"System: {n}");
        }
        override public void ExitSystem(dsParser.SystemContext ctx) { _system = null; }

        override public void EnterTask(dsParser.TaskContext ctx)
        {
            var name = ctx.id().GetText();
            _task = new DsTask(name, _system);
            QpMap.Add(CurrentPath, _task);
        }
        override public void ExitTask(dsParser.TaskContext ctx) { _task = null; }

        override public void EnterFlow(dsParser.FlowContext ctx)
        {
            var flowName = ctx.id().GetText();
            var flowOf = ctx.flowProp().id();
            _rootFlow = new RootFlow(flowName, _system);
            QpMap.Add(CurrentPath, _rootFlow);
            Trace.WriteLine($"Flow: {flowName}");
        }
        override public void ExitFlow(dsParser.FlowContext ctx) { _rootFlow = null; }
        override public void EnterParenting(dsParser.ParentingContext ctx)
        {
            Trace.WriteLine($"Parenting: {ctx.GetText()}");
            var name = ctx.id().GetText();
            _parenting = new Segment(name, _rootFlow);
            QpMap.Add(CurrentPath, _parenting);
        }
        override public void ExitParenting(dsParser.ParentingContext ctx) { _parenting = null; }

        override public void EnterCausalPhrase(dsParser.CausalPhraseContext ctx)
        {
            var xx = ctx.GetText();
            var names =
                DsParser.enumerateChildren<dsParser.SegmentContext>(
                    ctx, false, r => r is dsParser.SegmentContext)
                .Select(segCtx => segCtx.GetText())
                ;

            if (_parenting == null)
            {
                foreach (var n in names)
                {
                    Debug.Assert(!ParserHelper.AliasNameMaps[_system].ContainsKey(n));
                    var fqdn = $"{CurrentPath}.{n}";
                    if (!QpMap.ContainsKey(fqdn))
                    {
                        var seg = new Segment(n, _rootFlow);
                        QpMap.Add(fqdn, seg);
                    }
                }
            }

            //var _segments =
            //    names
            //    .Where(n => !n.Contains('.'))  // '.' 이 포함되면 Call
            //    .Where(n => !_rootFlow.Segments.Any(s => s.Name == n))
            //    .Select(n =>
            //    {
            //        var flow = (PFlow)_parenting ?? _rootFlow;
            //        if (ParserHelper.AliasNameMaps[_system].ContainsKey(n))
            //            return new PAlias(n, flow, ParserHelper.AliasNameMaps[_system][n]) as IPCoin;
            //        else
            //            return new PSegment(n, _rootFlow);
            //    })  // _flow 에 segment 로 등록됨
            //    .ToArray()
            //    ;

            System.Console.WriteLine();
        }

        override public void EnterCall(dsParser.CallContext ctx)
        {
            var name = ctx.id().GetText();
            var label = $"{name}\n{ctx.callPhrase().GetText()}";
            var callph = ctx.callPhrase();
            //var tx = callph.segments(0);
            //var rx = callph.segments(1);
            var call = new CallPrototype(name, _task);
            QpMap.Add($"{CurrentPath}.{name}", call);
            //var parentId = $"{this.systemName}.{this.taskName}";
            //var id = $"{parentId}.{name}";
            //this.nodes[id] = new Node(id, label, parentId, NodeType.call);
            Trace.WriteLine($"CALL: {name}");
        }


        override public void EnterListing(dsParser.ListingContext ctx)
        {
            var name = ctx.id().GetText();
            var seg = new Segment(name, _rootFlow);
            QpMap.Add($"{CurrentPath}.{name}", seg);

            //var id = $"{this.systemName}.{this.taskName}.{name}";
            ////const node = { "data": { id, "label": name, "background_color": "gray", parent: this.taskName }        };
            //var parentId = $"{this.systemName}.{this.taskName}";
            //this.nodes[id] = new Node(id, label: name, parentId, NodeType.segment);
        }



        override public void EnterCausals(dsParser.CausalsContext ctx)
        {
            Trace.WriteLine($"Causals: {ctx.GetText()}");
        }
        //override public void ExitCausals(dsParser.CausalsContext ctx) {}


        override public void EnterAliasListing(dsParser.AliasListingContext ctx)
        {
            var def = ctx.aliasDef().GetText();
            var aliasMnemonics =
                DsParser.enumerateChildren<dsParser.AliasMnemonicContext>(ctx, false, r => r is dsParser.AliasMnemonicContext)
                .Select(mne => mne.GetText())
                .ToArray()
                ;
            Debug.Assert(aliasMnemonics.Length == aliasMnemonics.Distinct().Count());

            ParserHelper.BackwardAliasMaps[_system].Add(def, aliasMnemonics);
        }
        override public void ExitAlias(dsParser.AliasContext ctx)
        {
            var bwd = ParserHelper.BackwardAliasMaps[_system];
            Debug.Assert(ParserHelper.AliasNameMaps[_system].Count() == 0);
            Debug.Assert(bwd.Values.Count() == bwd.Values.Distinct().Count());
            var reversed =
                from tpl in bwd
                let k = tpl.Key
                from v in tpl.Value
                select (v, k)
                ;

            foreach ((var mnemonic, var target) in reversed)
                ParserHelper.AliasNameMaps[_system].Add(mnemonic, target);
        }


        override public void EnterCpu(dsParser.CpuContext ctx)
        {
            var name = ctx.id().GetText();
            var flowPathContexts =
                DsParser.enumerateChildren<dsParser.FlowPathContext>(ctx, false, r => r is dsParser.FlowPathContext)
                ;

            var flows =
                flowPathContexts.Select(fpc =>
                {
                    var systemName = fpc.GetChild(0).GetText();
                    var dot_ = fpc.GetChild(1).GetText();
                    var flowName = fpc.GetChild(2).GetText();

                    var system = _model.Systems.FirstOrDefault(sys => sys.Name == systemName);
                    var flow = system.RootFlows.FirstOrDefault(f => f.Name == flowName);
                    return flow;
                })
                .ToArray()
                ;
            var cpu_ = new Cpu(name, flows, _model);
        }



        override public void ExitProgram(dsParser.ProgramContext ctx) {}


        // ParseTreeListener<> method
        override public void VisitTerminal(ITerminalNode node)     { return; }
        override public void VisitErrorNode(IErrorNode node)        { return; }
        override public void EnterEveryRule(ParserRuleContext ctx) { return; }
        override public void ExitEveryRule(ParserRuleContext ctx) { return; }
    }
}
