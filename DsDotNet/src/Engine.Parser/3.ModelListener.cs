using System;
using System.Diagnostics;
using System.Linq;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using Engine.Core;

namespace DsParser
{
    class ModelListener : dsBaseListener
    {
        public Model Model { get; }
        public ParserHelper ParserHelper;
        DsSystem _system;
        DsTask _task;
        RootFlow _rootFlow;
        Segment _parenting;

        string CurrentPath
        {
            get
            {
                if (_task != null)
                    return $"{_system.Name}.{_task.Name}";
                if (_parenting != null)
                    return $"{_system.Name}.{_rootFlow.Name}.{_parenting.Name}";
                if (_rootFlow != null)
                    return $"{_system.Name}.{_rootFlow.Name}";
                if (_system != null)
                    return _system.Name;

                throw new Exception("ERROR");
            }
        }
        public ModelListener(dsParser parser, ParserHelper helper)
        {
            Model = new Model();
            ParserHelper = helper;
            parser.Reset();
        }


        override public void EnterSystem(dsParser.SystemContext ctx)
        {
            var n = ctx.id().GetText();
            _system = new DsSystem(n, Model);
            Trace.WriteLine($"System: {n}");
        }
        //override public void ExitSystem(dsParser.SystemContext ctx) { this.systemName = null; }

        override public void EnterTask(dsParser.TaskContext ctx)
        {
            var name = ctx.id().GetText();
            _task = new DsTask(name, _system);
            ParserHelper.QualifiedPathMap.Add(CurrentPath, _task);
        }
        override public void ExitTask(dsParser.TaskContext ctx) { _task = null; }

        override public void EnterFlow(dsParser.FlowContext ctx)
        {
            var flowName = ctx.id().GetText();
            var flowOf = ctx.flowProp().id();
            _rootFlow = new RootFlow(flowName, _system);
            ParserHelper.QualifiedPathMap.Add(CurrentPath, _rootFlow);
            Trace.WriteLine($"Flow: {flowName}");
        }
        override public void ExitFlow(dsParser.FlowContext ctx) { _rootFlow = null; }
        override public void EnterParenting(dsParser.ParentingContext ctx)
        {
            Trace.WriteLine($"Parenting: {ctx.GetText()}");
            var name = ctx.id().GetText();
            _parenting = new Segment(name, _rootFlow);
            ParserHelper.QualifiedPathMap.Add(CurrentPath, _parenting);
        }
        override public void ExitParenting(dsParser.ParentingContext ctx) { _parenting = null; }

        override public void EnterCausalPhrase(dsParser.CausalPhraseContext ctx)
        {
            var names =
                DsParser.enumerateChildren<dsParser.SegmentContext>(
                    ctx, false, r => r is dsParser.SegmentContext)
                .Select(segCtx => segCtx.GetText())
                ;

            if (_parenting == null)
            {
                foreach (var n in names)
                {
                    Debug.Assert(!_system.AliasNameMap.ContainsKey(n));
                    var fqdn = $"{CurrentPath}.{n}";
                    if (! ParserHelper.QualifiedPathMap.ContainsKey(fqdn))
                    {
                        var seg = new Segment(n, _rootFlow);
                        ParserHelper.QualifiedPathMap.Add(fqdn, seg);
                    }
                }
            }
            else
            {
                foreach (var n in names)
                {
                    if (_system.AliasNameMap.ContainsKey(n))
                    {
                        var target = ParserHelper.QualifiedPathMap[_system.AliasNameMap[n]];
                        switch (target)
                        {
                            case CallPrototype cp:
                                var child = new Child(new Call(n, _parenting, cp), _parenting);
                                ParserHelper.QualifiedPathMap.Add($"{CurrentPath}.{n}", child);
                                break;
                            default:
                                throw new Exception("ERRROR");
                        }
                        Console.WriteLine();
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
            //        if (_system.AliasNameMap.ContainsKey(n))
            //            return new PAlias(n, flow, _system.AliasNameMap[n]) as IPCoin;
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
            ParserHelper.QualifiedPathMap.Add($"{CurrentPath}.{name}", call);
            //var parentId = $"{this.systemName}.{this.taskName}";
            //var id = $"{parentId}.{name}";
            //this.nodes[id] = new Node(id, label, parentId, NodeType.call);
            Trace.WriteLine($"CALL: {name}");
        }


        override public void EnterListing(dsParser.ListingContext ctx)
        {
            var name = ctx.id().GetText();
            var seg = new Segment(name, _rootFlow);
            ParserHelper.QualifiedPathMap.Add($"{CurrentPath}.{name}", seg);

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

            _system._strBackwardAliasMap.Add(def, aliasMnemonics);
        }
        override public void ExitAlias(dsParser.AliasContext ctx)
        {
            var bwd = _system._strBackwardAliasMap;
            Debug.Assert(_system.AliasNameMap.Count() == 0);
            Debug.Assert(bwd.Values.Count() == bwd.Values.Distinct().Count());
            var reversed =
                from tpl in bwd
                let k = tpl.Key
                from v in tpl.Value
                select (v, k)
                ;

            foreach ((var mnemonic, var target) in reversed)
                _system.AliasNameMap.Add(mnemonic, target);
        }

        override public void ExitProgram(dsParser.ProgramContext ctx)
        {
            ////var tpls = _model.Systems.SelectMany(s => s.AliasNameMap).Select(tpl => (tpl.Key, tpl.Value));
            ////foreach ( (var alias, var target) in tpls )
            ////{
            ////    var xxx2 = _model.FindSegment(target);
            ////    var xxx = _model.FindSegment(alias);
            ////    //FindVertex(target);
            ////    Console.WriteLine();
            ////}

            //var tpls =
            //    from sys in Model.Systems
            //    from tpl in sys.AliasNameMap
            //    where sys.Aliases.ContainsKey(tpl.Key)
            //    let alias = sys.Aliases[tpl.Key]
            //    let target = Model.FindCoin(tpl.Value)
            //    select (alias, target)
            //    ;
            //foreach ((var alias, var target) in tpls)
            //{
            //    Debug.Assert(target != null);
            //    switch (alias)
            //    {
            //        case PAlias seg:
            //            seg.AliasTarget = target;
            //            break;
            //        default:
            //            throw new Exception("ERROR");
            //    }
            //    Console.WriteLine();

            //}

            //Console.WriteLine();
            ////_model.Systems.
        }


        // ParseTreeListener<> method
        override public void VisitTerminal(ITerminalNode node)     { return; }
        override public void VisitErrorNode(IErrorNode node)        { return; }
        override public void EnterEveryRule(ParserRuleContext ctx) { return; }
        override public void ExitEveryRule(ParserRuleContext ctx) { return; }
    }
}
