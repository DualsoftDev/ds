using Dsu.Common.CS.LSIS.ExtensionMethods;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Dsu.Common.CS.LSIS.Graph
{
    /*
     * Graph<N,E> 의 내용 중, Extension method 에 해당 하는 내용.
     * Generic class 의 extension method 만드는 것이 까다로워서 
     * partial class 로 구분 함.
     */
    public partial class Graph<N, E>
    {
        public IEnumerable<N> Sources
        {
            get
            {
                foreach (N n in m_Nodes)
                {
                    if (GetIncomingDegree(n) == 0)
                        yield return n;
                }
            }
        }

        public IEnumerable<N> Sinks
        {
            get
            {
                foreach (N n in m_Nodes)
                {
                    if (GetOutgoingDegree(n) == 0)
                        yield return n;
                }
            }
        }

        public void RemoveIsolatedNodes()
        {
            var vQuery = Nodes.Where(n => { return GetDegree(n) == 0; }).ToList();
            foreach (N node in vQuery)
                Remove(node);
        }


        /// <summary>
        /// 주어진 graph 에서 node 만 삭제하고 node 의 전후 관계 edge 는 그대로 복원한다.
        /// node 전후 관계 edge 의 data 중에서는 앞의 것만 살리고 뒤의 것은 없어짐
        /// </summary>
        /// <param name="node"></param>
        public void OmitNode(N node)
        {
            var ies = GetIncomingEdges(node).ToArray(); // 컬렉션이 수정되었습니다.  열거 작업이 ..
            var ons = GetOutgoingNodes(node).ToArray();
            ies.Iter(inE => {
                ons.Iter(outN =>
                {
                    AddEdge(inE.Start, outN, inE.Data);
                });
            });
            Remove(node);
        }

        // Connected graph 에서 traversal path 를 추출한다.   path 를 구성하는 edge 를 모아서 return 한다.
        // side effect : graph 에서 return 되는 path 에 해당하는 edge 들이 삭제 된다.
        public List<Edge<N, E>> PullOutPath()
        {
            RemoveIsolatedNodes();

            List<Edge<N, E>> path = new List<Edge<N, E>>();
            if (Sources.IsNullOrEmpty())
                return path;


            IEnumerable<N> nodes = DepthFirstSearch(Sources.ElementAt(0));
            Debug.Assert(nodes.Count() >= 2);
            for (int i = 0; i < nodes.Count() - 1; i++)
            {
                N s = nodes.ElementAt(i);
                N e = nodes.ElementAt(i + 1);
                Nullable<Edge<N, E>> edge = GetEdge(s, e);
                if (edge.HasValue)
                    path.Add(edge.Value);
                else
                    break;
            }

            foreach (Edge<N, E> edge in path)
            {
                m_Edges.Remove(edge);
            }

            return path;
        }



        // http://stackoverflow.com/questions/10032940/iterative-connected-components-algorithm
        public Graph<N, E> GetSubGraph(IEnumerable<N> nodes)
        {
            Graph<N, E> g = new Graph<N, E>();
            g.Add(nodes);
            foreach (var n in g.Nodes)
            {
                foreach (N adjacent in GetIncomingNodes(n))
                {
                    Nullable<Edge<N, E>> e = GetEdge(adjacent, n);
                    Debug.Assert(e.HasValue);
                    g.AddEdge(adjacent, n, e.Value.Data);
                }
                foreach (N adjacent in GetOutgoingNodes(n))
                {
                    Nullable<Edge<N, E>> e = GetEdge(n, adjacent);
                    Debug.Assert(e.HasValue);
                    g.AddEdge(n, adjacent, e.Value.Data);
                }
            }
            return g;
        }

        public IEnumerable<N> DepthFirstSearch(N nodeStart) { return DepthFirstSearch(nodeStart, false); }
        public IEnumerable<N> DepthFirstSearch(N nodeStart, bool bIgnoreDirection/*=false*/)
        {
            var stack = new Stack<N>();
            var visitedNodes = new HashSet<N>();
            stack.Push(nodeStart);
            while (stack.Count > 0)
            {
                var curr = stack.Pop();
                if (!visitedNodes.Contains(curr))
                {
                    visitedNodes.Add(curr);
                    yield return curr;
                    IEnumerable<N> adjs = bIgnoreDirection ? GetAdjacentNodes(curr) : GetOutgoingNodes(curr);
                    foreach (var next in adjs)
                    {
                        if (!visitedNodes.Contains(next))
                            stack.Push(next);
                    }
                }
            }
        }

        public IEnumerable<Graph<N, E>> GetConnectedComponents()
        {
            var visitedNodes = new HashSet<N>();
            var components = new List<Graph<N, E>>();

            foreach (var node in this.Nodes)
            {
                if (!visitedNodes.Contains(node))
                {
                    var subGraph = GetSubGraph(this.DepthFirstSearch(node, true));
                    components.Add(subGraph);
                    visitedNodes.UnionWith(subGraph.Nodes);
                }
            }
            return components;
        }

        public void Dump()
        {
            Global.Logger?.Debug($"---Graph---- : Nodes={Nodes.Count()}, Edges={Edges.Count()}");
            foreach (Edge<N, E> e in Edges)
            {
                Global.Logger?.Debug($"{e.Start} -- {e.End} : {e.Data}");
            }
        }
    
    }
}

