using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Engine.Core;

namespace Engine.Parser
{
    class AliasListener : dsBaseListener
    {
        #region Boiler-plates
        public ParserHelper ParserHelper;
        Model _model => ParserHelper.Model;
        DsSystem _system { get => ParserHelper._system; set => ParserHelper._system = value; }
        DsTask _task { get => ParserHelper._task; set => ParserHelper._task = value; }
        RootFlow _rootFlow { get => ParserHelper._rootFlow; set => ParserHelper._rootFlow = value; }
        SegmentBase _parenting { get => ParserHelper._parenting; set => ParserHelper._parenting = value; }
        /// <summary> Qualified Path Map </summary>
        Dictionary<string, object> QpInstanceMap => ParserHelper.QualifiedInstancePathMap;
        Dictionary<string, object> QpDefinitionMap => ParserHelper.QualifiedDefinitionPathMap;

        string CurrentPath => ParserHelper.CurrentPath;

        public AliasListener(dsParser parser, ParserHelper helper)
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
            _parenting = (SegmentBase)QpInstanceMap[$"{CurrentPath}.{name}"];
        }
        override public void ExitParenting(dsParser.ParentingContext ctx) { _parenting = null; }
        #endregion Boiler-plates




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

            //var defInstance = _model.FindObject<object>(def);

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
    }
}
