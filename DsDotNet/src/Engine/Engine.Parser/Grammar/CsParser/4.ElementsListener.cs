// from cytoscpaeVisitor.ts

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using Nodes = System.Collections.Generic.List<System.Object>;

namespace DsParser
{
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


    class ElementsListener : dsBaseListener
    {
        PModel _model;

        /** causal operator 왼쪽 */
        private dsParser.CausalTokensDNFContext left;
        private dsParser.CausalOperatorContext op;

        PSystem _system;
        PTask _task;
        PRootFlow _rootFlow;
        PSegment _parenting;

        //private string flowName;        // [flow of A]F={..} -> F
        private string flowOfName;      // [flow of A]F={..} -> A
        private List<ParserRuleContext> allParserRules;
        private bool multipleSystems;

        Dictionary<string, Node> nodes = new Dictionary<string, Node>();

        public ElementsListener(dsParser parser, PModel model)
        {
            _model = model;

            this.allParserRules = DsParser.getAllParseRules(parser);
            parser.Reset();

            this.multipleSystems = this.allParserRules.Where(t => t is dsParser.SystemContext).Count() > 1;
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

        override public void EnterListing(dsParser.ListingContext ctx)
        {
            //var name = ctx.id().GetText();
            //var id = $"{this.systemName}.{this.taskName}.{name}";
            ////const node = { "data": { id, "label": name, "background_color": "gray", parent: this.taskName }        };
            //var parentId = $"{this.systemName}.{this.taskName}";
            //this.nodes[id] = new Node(id, label: name, parentId, NodeType.segment);
        }

        override public void EnterCall(dsParser.CallContext ctx) {
            var name = ctx.id().GetText();
            var label = $"{name}\n{ctx.callPhrase().GetText()}";
            var call = _task.Calls.First(c => c.Name == name);

            var callph = ctx.callPhrase();
            var txs = _model.FindSegments(callph.segments(0).GetText());
            var rxs = _model.FindSegments(callph.segments(1).GetText());
            call.TXs = txs;
            call.RXs = rxs;
            //Trace.WriteLine($"Call: {name} = {txs.Select(tx => tx.Name)} ~ {rx?.Name}");
        }

        override public void EnterFlow(dsParser.FlowContext ctx)
        {
            var flowName = ctx.id().GetText();
            _rootFlow = _system.RootFlows.OfType<PRootFlow>().First(f => f.Name == flowName);

            var flowOf = ctx.flowProp().id();
            this.flowOfName = flowOf == null ? flowName : flowOf.GetText();

            var causal =
                DsParser.enumerateChildren<dsParser.CausalContext>(ctx, false, r => r is dsParser.CausalContext)
                ;
            var parenting =
                DsParser.enumerateChildren<dsParser.ParentingContext>(ctx, false, r => r is dsParser.ParentingContext)
                ;
            var listing =
                DsParser.enumerateChildren<dsParser.ListingContext>(ctx, false, r => r is dsParser.ListingContext)
                ;

            Trace.WriteLine($"Flow: {flowName}");
        }
        override public void ExitFlow(dsParser.FlowContext ctx)
        {
            _rootFlow = null;
            flowOfName = null;
        }

        override public void EnterCausals(dsParser.CausalsContext ctx)
        {
            Trace.WriteLine($"Causals: {ctx.GetText()}");
        }
        override public void ExitCausals(dsParser.CausalsContext ctx)
        {
        }

        override public void EnterParenting(dsParser.ParentingContext ctx) {
            var name = ctx.id().GetText();
            var seg = _rootFlow.Segments.First(s => s.Name == name);

            _parenting = seg ?? new PSegment(name, _rootFlow);

            // A = {
            //  B > C > D;
            //  D |> C;
            // }
            var causalContexts =
                DsParser.enumerateChildren<dsParser.CausalPhraseContext>(ctx, false, r => r is dsParser.CausalPhraseContext)
                ;
            foreach(var cauCtx in causalContexts)
            {

            }

            Trace.WriteLine($"Parenting: {ctx.GetText()}");
        }
        override public void ExitParenting(dsParser.ParentingContext ctx) { _parenting = null; }



        override public void EnterCausalPhrase(dsParser.CausalPhraseContext ctx) {
            this.left = null;
            this.op = null;

            Trace.WriteLine($"CausalPhrase: {ctx.GetText()}");
            var left = ctx.GetChild(0);
            var op = ctx.GetChild(1);
            var rights = ctx.GetChild(2);

            if (_parenting == null)
            {
                //_parenting.ChildFlow.Edges.Add(new Edge())
            }
            else
            {
                //var l = _model.FindCall($"{_system.Name}.{left.GetText()}");
                //var r = _model.FindCall($"{_system.Name}.{rights.GetText()}");
                //var edge = new Edge(new[] { l }, r);
                //_parenting.ChildFlow.Edges.Add(edge);
            }
            Trace.WriteLine($"\tCausalPhrase all: {left.GetText()}, {op.GetText()}, {rights.GetText()}");
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

        override public void EnterCpu(dsParser.CpuContext ctx) {
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
            var cpu_ = new PCpu(name, flows, _model);
        }


        //override public void ExitProgram(dsParser.ProgramContext ctx)
        //{
        //    //var tpls = _model.Systems.SelectMany(s => s.AliasNameMap).Select(tpl => (tpl.Key, tpl.Value));
        //    //foreach ( (var alias, var target) in tpls )
        //    //{
        //    //    var xxx2 = _model.FindSegment(target);
        //    //    var xxx = _model.FindSegment(alias);
        //    //    //FindVertex(target);
        //    //    Console.WriteLine();
        //    //}

        //    var tpls =
        //        from sys in _model.Systems
        //        from tpl in sys.AliasNameMap
        //        where sys.Aliases.ContainsKey(tpl.Key)
        //        let alias = sys.Aliases[tpl.Key]
        //        let target = _model.FindSegment(tpl.Value)
        //        select (alias, target)
        //        ;
        //    foreach ((var alias, var target) in tpls)
        //    {
        //        switch(alias)
        //        {
        //            case PSegmentAlias seg:
        //                seg.AliasTarget = target;
        //                break;
        //            default:
        //                throw new Exception("ERROR");
        //        }
        //        Console.WriteLine();

        //    }

        //        Console.WriteLine();
        //    //_model.Systems.
        //}


        // ParseTreeListener<> method
        override public void VisitTerminal(ITerminalNode node)     { return; }
        override public void VisitErrorNode(IErrorNode node)        { return; }
        override public void EnterEveryRule(ParserRuleContext ctx) { return; }
        override public void ExitEveryRule(ParserRuleContext ctx) { return; }


        private Dictionary<dsParser.CausalTokensDNFContext, Nodes> _existings = new Dictionary<dsParser.CausalTokensDNFContext, Nodes>();
        private Nodes addNodes(dsParser.CausalTokensDNFContext ctx)
        {
            if (this._existings.ContainsKey(ctx))
                return this._existings[ctx];

            var cnfs =
                DsParser.enumerateChildren<dsParser.CausalTokensCNFContext>(ctx, false, t => t is dsParser.CausalTokensCNFContext)
                ;

            Nodes dnfNodes = new Nodes();
            foreach (var cnf in cnfs)
            {
                List<Node> cnfNodes = new List<Node>();
                var causalContexts =
                    DsParser.enumerateChildren<dsParser.CausalTokenContext>(cnf, false, t => t is dsParser.CausalTokenContext);

                foreach (var t in causalContexts)
                {
                    var text = t.GetText();
                    if (text.StartsWith("#"))
                    {
                        var node = new Node(id: text, label: text, null, NodeType.func);
                        cnfNodes.Add(node);
                    }
                    else if (text.StartsWith("@"))
                    {
                        var node = new Node(id: text, label: text, null, NodeType.proc);
                        cnfNodes.Add(node);
                    }
                    else
                    {
                        // count number of '.' from text
                        var dotCount = text.Split(new[] { '.' }).Length - 1;
                        string id = text;
                        var taskId = $"{_system.Name}.{this.flowOfName}";
                        if (_parenting != null)
                            taskId = $"{taskId}.{_parenting.Name}";

                        var parentId = taskId;
                        switch (dotCount)
                        {
                            case 0:
                                id = $"{ taskId}.{ text}";
                                break;
                            case 1:
                                id = $"{_system.Name}.{ text}";
                                parentId = $"{ _system.Name}.{text.Split(new[] { '.' })[0]}";
                                break;
                        }

                        var nodeType = NodeType.segment;
                        if (dotCount == 0 && _system.AliasNameMap.ContainsKey(text))
                            nodeType = NodeType.segmentAlias;

                        var node = new Node(id, label: text, parentId: taskId, nodeType);
                        cnfNodes.Add(node);
                    }
                    foreach (var n in cnfNodes)
                    {
                        if (!this.nodes.ContainsKey(n.id))
                            this.nodes[n.id] = n;
                    }
                }
                dnfNodes.Add(cnfNodes);
            }

            this._existings[ctx] = dnfNodes;

            return dnfNodes;
        }

        /**
            *
            * @param nodes : DNF nodes
            * @param append true (==nodes 가 sink) 인 경우, conjuction 생성.  false: 개별 node 나열 생성
            * @returns
            */
        private List<string> getCnfTokens(Nodes nodes, bool append = false)
        {
            var cnfTokens = new List<string>();
            foreach (var x in nodes)
            {
                var array = x as List<Node>;
                var isArray = array != null && array.Count() > 1;    // x is IList<Node> && ((x as List<Node>).Count() > 1);

                if (append && isArray)
                {
                    var id = string.Join(",", array.Select(n => n.id));
                    cnfTokens.Add(id);

                    var conj = new Node(id, label: "", parentId: flowOfName, NodeType.conjunction);
                    this.nodes[id] = conj;
                }
                else
                {
                    if (isArray)
                        foreach (var id in array.Select(n => n.id))
                            cnfTokens.Add(id);
                    else
                    {
                        var token = array == null ? (x as Node) : array[0];
                        cnfTokens.Add(token.id);
                    }
                }
            }

            return cnfTokens;
        }

        /**
            * 복합 Operator 를 분해해서 개별 operator array 로 반환
            * @param operator 복합 operator.  e.g "<||>"
            * @returns e.g [ "<|", "|>" ]
            */
        private List<string> splitOperator(string operator_)
        {
            var op = operator_;
            IEnumerable<string> split()
            {
                foreach (var o in new[] { "|>>", "<<|", ">>", "<<", })
                {
                    if (op.Contains(o))
                    {
                        yield return o;
                        op = op.Replace(o, "");
                    }
                }

                foreach (var o in new[] { "|>", "<|", })
                {
                    if (op.Contains(o))
                    {
                        yield return o;
                        op = op.Replace(o, "");
                    }
                }
                foreach (var o in new[] { ">", "<", })
                {
                    if (op.Contains(o))
                    {
                        yield return o;
                        op = op.Replace(o, "");
                    }
                }
                if (op.Length > 0)
                    Console.WriteLine($"Error on causal operator: {operator_}");
            }

            return split().ToList();
        }


        IPCoin FindVertex(string v, PFlow flow)
        {
            var seg = _model.FindSegment(v);
            var callPrototype = _model.FindCall(v);

            if (seg!=null && callPrototype!=null)
                throw new Exception($"Parse error: {v} is ambiguous.  Both segment and call exists.");
            if (seg == null && callPrototype == null)
                throw new Exception($"Parse error: {v} not found.");

            if (seg != null)
                return seg;

            var map = flow.CallInstanceMap;
            if (! map.ContainsKey(callPrototype))
            {
                var call = new PCall(callPrototype.Name, flow, callPrototype);
                map.Add(callPrototype, call);
            }

            return map[callPrototype];
        }
        IPCoin[] FindVertices(string specs, PFlow flow) => specs.Split(new[] { ',' }).Select(spec => FindVertex(spec, flow)).ToArray();
        IPCoin[] FindVertices(Node node, PFlow flow)
        {
            if (node.type == NodeType.segmentAlias)
            {
                var aliasTarget = flow.GetSystem().Aliases[node.label];
                return new IPCoin[] { aliasTarget };
            }
            return FindVertices(node.id, flow);
        }

        /**
            * causal operator 를 처리해서 this.links 에 결과 누적
            * @param ll operator 왼쪽의 DNF
            * @param opr (복합) operator
            * @param rr operator 우측의 DNF
            */
        private void processCausal(dsParser.CausalTokensDNFContext ll, dsParser.CausalOperatorContext opr, dsParser.CausalTokensDNFContext rr)
        {
            Trace.WriteLine($"{ ll.GetText()} { opr.GetText()} { rr.GetText()}");
            var nodes = this.nodes;

            var ls = this.addNodes(ll);
            var rs = this.addNodes(rr);
            // for (const n of this.nodes.keys())
            //     console.log(n);


            var ops = this.splitOperator(opr.GetText());
            foreach (var op in ops)
            {
                var sinkToRight = op == ">" || op == "|>";
                var lss = this.getCnfTokens(ls, sinkToRight);
                var rss = this.getCnfTokens(rs, !sinkToRight);

                foreach (var strL in lss)
                {
                    foreach (var strR in rss)
                    {
                        var l = this.nodes[strL];
                        var r = this.nodes[strR];

                        PFlow pFlow = (PFlow)_parenting ?? _rootFlow;   // target flow

                        var lvs = FindVertices(l, pFlow);
                        var rvs = FindVertices(r, pFlow);

                        Debug.Assert(l != null && r != null);   // 'node not found');
                        if (lvs.Length == 0) throw new Exception($"Parse error: {l.id} not found");
                        if (rvs.Length == 0) throw new Exception($"Parse error: {r.id} not found");

                        switch (op)
                        {
                            case "|>":
                            case ">":
                            case "|>>":
                            case ">>":
                                pFlow.Edges.Add(new PEdge(pFlow, lvs, op, rvs[0]));
                                break;

                            case "<|":
                                Debug.Assert(lvs.Length == 1);
                                pFlow.Edges.Add(new PEdge(pFlow, rvs, "|>", lvs[0]));
                                break;
                            case "<":
                                Debug.Assert(lvs.Length == 1);
                                pFlow.Edges.Add(new PEdge(pFlow, rvs, ">", lvs[0]));
                                break;
                            case "<<|":
                                Debug.Assert(lvs.Length == 1);
                                pFlow.Edges.Add(new PEdge(pFlow, rvs, "|>>", lvs[0]));
                                break;
                            case "<<":
                                Debug.Assert(lvs.Length == 1);
                                pFlow.Edges.Add(new PEdge(pFlow, rvs, ">>", lvs[0]));
                                break;

                            default:
                                Debug.Assert(false);    //, `invalid operator: ${ op}`);
                                break;
                        }

                    }
                }
            }
        }
    }
}
