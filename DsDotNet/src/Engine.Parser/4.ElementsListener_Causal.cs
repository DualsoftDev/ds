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
                            id = $"{taskId}.{text}";
                            break;
                        case 1:
                            id = $"{taskId}.{text}";
                            //parentId = $"{taskId}.{text.Split(new[] { '.' })[0]}";
                            break;
                    }

                    var nodeType = NodeType.segment;
                    if (dotCount == 0 && ParserHelper.AliasNameMaps[_system].ContainsKey(text))
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
        if (op == "=>")
            op = "><|";     // replace shortcut(=>) to original 

        IEnumerable<string> split()
        {
            foreach (var o in new[] { "||>", "<||", ">>", "<<", })
            {
                if (op.Contains(o) && op != "<||>")
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



    IVertex[] FindVertices(string context, Node node)
    {
        string specs = node.id;
        return specs.Split(new[] { ',' }).Select(sp =>
        {
            var spec = sp;
            if (QpInstanceMap.ContainsKey($"{context}.{sp}"))
                spec = $"{context}.{sp}";
            if (!QpInstanceMap.ContainsKey(spec))
            {
                if (ParserHelper.AliasNameMaps[_system].ContainsKey(node.label))
                    spec = ParserHelper.AliasNameMaps[_system][node.label];
            }

            var vertex = QpInstanceMap[spec] as IVertex;
            return vertex;
        }).ToArray();
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

                    var context = _parenting == null ? "" : CurrentPath;

                    var lvs = FindVertices(context, l);
                    var rvs = FindVertices(context, r);

                    Assert(l != null && r != null);   // 'node not found');
                    if (lvs.Length == 0) throw new ParserException($"Parse error: {l.id} not found", ll);
                    if (rvs.Length == 0) throw new ParserException($"Parse error: {r.id} not found", rr);

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
