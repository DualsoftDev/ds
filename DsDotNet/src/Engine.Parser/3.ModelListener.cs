using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using Engine.Core;

namespace Engine.Parser;

class ModelListener : dsBaseListener
{
    public ParserHelper ParserHelper;
    Model    _model => ParserHelper.Model;
    DsSystem _system    { get => ParserHelper._system;    set => ParserHelper._system = value; }
    DsTask   _task      { get => ParserHelper._task;      set => ParserHelper._task = value; }
    RootFlow _rootFlow  { get => ParserHelper._rootFlow;  set => ParserHelper._rootFlow = value; }
    Segment  _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
    /// <summary> Qualified Path Map </summary>
    Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
    Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

    string CurrentPath => ParserHelper.CurrentPath;

    public ModelListener(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper;
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
    }
    override public void ExitFlow(dsParser.FlowContext ctx) { _rootFlow = null; }



    override public void EnterParenting(dsParser.ParentingContext ctx)
    {
        var name = ctx.id().GetText();
        _parenting = (Segment)QpInstanceMap[$"{CurrentPath}.{name}"];
    }
    override public void ExitParenting(dsParser.ParentingContext ctx) { _parenting = null; }









    override public void EnterCausalPhrase(dsParser.CausalPhraseContext ctx)
    {
        var names =
            DsParser.enumerateChildren<dsParser.SegmentContext>(
                ctx, false, r => r is dsParser.SegmentContext)
            .Select(segCtx => segCtx.GetText())
            ;

        void createFromDefinition(object target, string n, string fqdn)
        {
            switch (target)
            {
                case CallPrototype cp:
                    var call = new RootCall(n, _rootFlow, cp);
                    QpInstanceMap.Add(fqdn, call);
                    break;
                default:
                    throw new Exception("ERROR");
            }
        }

        if (_parenting == null)
        {
            foreach (var n in names)
            {
                var fqdn = $"{CurrentPath}.{n}";
                if (ParserHelper.AliasNameMaps[_system].ContainsKey(n))
                {
                    var targetName = ParserHelper.AliasNameMaps[_system][n];
                    var target = QpDefinitionMap[targetName];
                    createFromDefinition(target, n, fqdn);
                }
                else
                {
                    if (!QpInstanceMap.ContainsKey(fqdn))
                    {
                        if (n.Contains("."))
                        {
                            var fullPrototypeName = ParserHelper.ToFQDN(n);
                            if (QpDefinitionMap.ContainsKey(fullPrototypeName))
                            {
                                var def = QpDefinitionMap[fullPrototypeName];
                                createFromDefinition(def, n, fqdn);
                                continue;
                            }
                        }
                        var seg = new Segment(n, _rootFlow);
                        QpInstanceMap.Add(fqdn, seg);
                    }
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



    override public void EnterCausals(dsParser.CausalsContext ctx)
    {
        Trace.WriteLine($"Causals: {ctx.GetText()}");
    }
    //override public void ExitCausals(dsParser.CausalsContext ctx) {}



    /*
        [alias] = {
            P.F.Vp = { Vp1; Vp2; Vp3; }
        }
     */
    override public void EnterAliasListing(dsParser.AliasListingContext ctx)
    {
        var def = ctx.aliasDef().GetText(); // e.g "P.F.Vp"
        var aliasMnemonics =    // e.g { Vp1; Vp2; Vp3; }
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



    override public void ExitProgram(dsParser.ProgramContext ctx) {}


    // ParseTreeListener<> method
    override public void VisitTerminal(ITerminalNode node)     { return; }
    override public void VisitErrorNode(IErrorNode node)        { return; }
    override public void EnterEveryRule(ParserRuleContext ctx) { return; }
    override public void ExitEveryRule(ParserRuleContext ctx) { return; }
}
