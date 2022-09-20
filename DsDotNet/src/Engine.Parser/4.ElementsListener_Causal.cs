using Nodes = System.Collections.Generic.List<System.Object>;

namespace Engine.Parser;

partial class ElementsListener
{
    private Dictionary<CausalTokensDNFContext, Nodes> _existings = new Dictionary<CausalTokensDNFContext, Nodes>();
    private Nodes addNodes(CausalTokensDNFContext ctx)
    {
        if (this._existings.ContainsKey(ctx))
            return this._existings[ctx];

        var cnfs = enumerateChildren<CausalTokensCNFContext>(ctx);

        Nodes dnfNodes = new Nodes();
        foreach (var cnf in cnfs)
        {
            List<Node> cnfNodes = new List<Node>();
            var causalContexts = enumerateChildren<CausalTokenContext>(cnf);

            foreach (var cc in causalContexts)
            {
                var ns = collectNameComponents(cc);
                var text = cc.GetText();
                if (text.StartsWith("#"))
                {
                    var node = new Node(ids: ns, label: text, null, NodeType.func);
                    cnfNodes.Add(node);
                }
                else if (text.StartsWith("@"))
                {
                    var node = new Node(ids: ns, label: text, null, NodeType.proc);
                    cnfNodes.Add(node);
                }
                else
                {
                    // count number of '.' from text
                    var flowIds = new[] { _system.Name, this.flowOfName };
                    if (_parenting != null)
                        flowIds = flowIds.Append(_parenting.Name).ToArray();

                    var n = ns.Length;

                    var nodeType = NodeType.segment;
                    var ids = n switch
                    {
                        1 => flowIds.Concat(ns).ToArray(),
                        2 => new Func<string[]>(() =>
                        {
                            if (_system.RootFlows.Any(rf => rf.Name == ns[0]))  // Sys.Flow + OtherFlow.Seg => Sys.OtherFlow.Seg
                            {
                                nodeType = NodeType.externalSegmentCall;
                                return ns.Prepend(_system.Name).ToArray();
                            }

                            return flowIds.Concat(ns).ToArray();
                        }).Invoke(),
                        3 => ns,
                        _ => throw new Exception("ERROR"),
                    };

                    if (n == 1 && _rootFlow.AliasNameMaps.ContainsKey(ns))
                        nodeType = NodeType.segmentAlias;

                    var node = new Node(ids, label: text, parentIds: flowIds, nodeType);
                    cnfNodes.Add(node);
                }
                foreach (var n in cnfNodes)
                {
                    var key = n.ids.Combine();
                    if (!this.nodes.ContainsKey(key))
                        this.nodes[key] = n;
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
                var idss = array.Select(n => n.ids).ToArray();
                var id = string.Join(",", idss.Select(ids => ids.Combine()));
                cnfTokens.Add(id);

                var conj = new NodeConjunction(idss, label: "", parentIds: new[] { flowOfName }, NodeType.conjunction);      // todo check flowOfName
                this.nodes[id] = conj;
            }
            else
            {
                if (isArray)
                    foreach (var id in array.Select(n => n.ids.Combine()))
                        cnfTokens.Add(id);
                else
                {
                    var token = array == null ? (x as Node) : array[0];
                    cnfTokens.Add(token.ids.Combine());
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
        if (op == "=>")
            op = "><|";     // replace shortcut(=>) to original 

        IEnumerable<string> split()
        {
            if (op == "<||>")
            {
                yield return "<||";
                yield return "||>";
                yield break;
            }

            foreach (var o in new[] { "||>", "<||", ">>", "<<", })
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



    IVertex[] FindVertices(SegmentBase parenting, NodeBase nodebase)
    {
        IVertex helper(string[] spec)
        {
            var obj = _model.Find(spec);
            if (obj is IVertex vtx)
                return vtx;
            return null;

            // todo: fix me
            //if (QpInstanceMap.ContainsKey((_system, $"{context}.{spec}")))
            //    spec = $"{context}.{spec}";
            //if (!QpInstanceMap.ContainsKey((_system, spec)))
            //{
            //    if (ParserHelper.AliasNameMaps[_rootFlow].ContainsKey(nodebase.label))
            //        spec = ParserHelper.AliasNameMaps[_rootFlow][nodebase.label];
            //}

            //var vertex = QpInstanceMap[(_system, spec)] as IVertex;
            //return vertex;
        }

        switch (nodebase)
        {
            case Node node:
                return new[] { helper(node.ids) };

            case NodeConjunction nodeConjunction:
                return
                    nodeConjunction.idss
                    .Select(helper)
                    .ToArray();
        }
        throw new NotImplementedException("ERROR");
    }


    /**
        * causal operator 를 처리해서 this.links 에 결과 누적
        * @param ll operator 왼쪽의 DNF
        * @param opr (복합) operator
        * @param rr operator 우측의 DNF
        */
    private void processCausal(CausalTokensDNFContext ll, CausalOperatorContext opr, CausalTokensDNFContext rr)
    {
        Trace.WriteLine($"{ll.GetText()} {opr.GetText()} {rr.GetText()}");

        if (rr.GetText() == "MyOtherFlow.A")
            Console.WriteLine();

        var ls = this.addNodes(ll);
        var rs = this.addNodes(rr);

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

                    Flow flow = (Flow)_parenting ?? _rootFlow;   // target flow
                    Assert(flow.Cpu != null);

                    var lvs = FindVertices(_parenting, l);
                    var rvs = FindVertices(_parenting, r);

                    Assert(l != null && r != null);   // 'node not found');
                    if (lvs.Length == 0) throw new ParserException($"Parse error: {l.label} not found", ll);
                    if (rvs.Length == 0) throw new ParserException($"Parse error: {r.label} not found", rr);

                    Edge e = null;
                    switch (op)
                    {
                        case "|>": e = new WeakResetEdge(flow, lvs, op, rvs[0]); break;
                        case ">": e = new WeakSetEdge(flow, lvs, op, rvs[0]); break;
                        case "||>": e = new StrongResetEdge(flow, lvs, op, rvs[0]); break;
                        case ">>": e = new StrongSetEdge(flow, lvs, op, rvs[0]); break;

                        case "<|":
                            Assert(lvs.Length == 1);
                            e = new WeakResetEdge(flow, rvs, "|>", lvs[0]);
                            break;
                        case "<":
                            Assert(lvs.Length == 1);
                            e = new WeakSetEdge(flow, rvs, ">", lvs[0]);
                            break;
                        case "<||":
                            Assert(lvs.Length == 1);
                            e = new StrongResetEdge(flow, rvs, "||>", lvs[0]);
                            break;
                        case "<<":
                            Assert(lvs.Length == 1);
                            e = new StrongSetEdge(flow, rvs, ">>", lvs[0]);
                            break;

                        default:
                            Assert(false);    //, `invalid operator: ${ op}`);
                            break;
                    }
                    flow.AddEdge(e);

                }
            }
        }
    }

}
