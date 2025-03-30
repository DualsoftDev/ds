/*
 * http://www.Planet-Source-Code.com/vb/scripts/ShowCode.asp?txtCodeId=4587&lngWId=10
 * http://stackoverflow.com/questions/10032940/iterative-connected-components-algorithm
 */


using log4net;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dual.Common.Utils.Graph
{
    /// Implements a graph where the nodes are of type T, and the edges are described by an object of type E
    /// <typeparam name="N">The node type</typeparam>
    /// <typeparam name="E">The edge data type</typeparam>
    public partial class Graph<N, E> : ICollection<N>
        where N : IEquatable<N>
    {
        protected List<N> m_Nodes = new List<N>();
        protected List<Edge<N, E>> m_Edges = new List<Edge<N, E>>();
        protected readonly bool m_bDirected;
        protected readonly bool m_bAllowsReflexivity;
        public static ILog Logger { get; set; }

        public delegate bool DelegateFilterEdge(Edge<N, E> edge);
        public delegate bool DelegateFilterNode(N node);

        public DelegateFilterEdge FilterEdge;
        public DelegateFilterNode FilterNode;

        /// Returns whether the graph is directed
        public bool IsDirected { get { return m_bDirected; } }

        /// Returns whether the graph allows reflexivity (nodes connected to themselves)
        public bool AllowsReflexivity { get { return m_bAllowsReflexivity; } }

        /// Returns the set of nodes in the graph
        public IEnumerable<N> Nodes
        {
            get
            {
                foreach (N node in m_Nodes)
                {
                    if ( FilterNode == null || ! FilterNode(node) )
                        yield return node;
                }
            }
        }

        /// Returns the set of edges in the graph
        public IEnumerable<Edge<N, E>> Edges
        {
            get
            {
                foreach (Edge<N, E> edge in m_Edges)
                {
                    if (FilterEdge == null || !FilterEdge(edge))
                        yield return edge;
                }
            }
        }

        /*
         * System.Collections.Generic.ICollection<N>.Count implementation
         */
        /// Returns the number of nodes in the graph
        public int Count { get { return m_Nodes.Count; } }

        /// Returns whether the graph is readonly
        public bool IsReadOnly { get { return false; } }


        public int NodeCount { get { return Nodes.Count(); } }
        public int EdgeCount { get { return Edges.Count(); } }


        /// Constructs a new, empty graph.
        /// <param name="directed">Whether the graph should be directed</param>
        /// <param name="allowsReflexivity">Whether the graph should allow reflexivity</param>
        public Graph(bool directed, bool allowsReflexivity)
        {
            var directional = directed ? "Directed" : "Undirected";
            Logger?.Debug($"Creating {directional} Graph..");

            this.m_bDirected = directed;
            this.m_bAllowsReflexivity = allowsReflexivity;
        }

        /// Constructs a new, empty graph.
        /// <param name="directed">Whether the graph should be directed</param>
        public Graph(bool directed) : this(directed, true) { }

        /// Constructs a new, empty graph.
        public Graph() : this(true, true) { }

        public Graph(Graph<N, E> src)
            : this(src.m_bDirected, src.m_bAllowsReflexivity)
        {
            //Logger?.Debug($"\tCreating graph with {src.m_Nodes.Count} nodes, {src.m_Edges.Count} edges.");
            m_Nodes.AddRange(src.m_Nodes);
            m_Edges.AddRange(src.m_Edges);
            FilterNode = src.FilterNode;
            FilterEdge = src.FilterEdge;
        }

        /// <summary>
        /// this graph 에 other 로 주어진 graph 의 node 와 edge 를 병합한다.
        /// </summary>
        /// <param name="other"></param>
        public virtual void MergeGraph(Graph<N, E> other)
        {
            if (other == null)
            {
                Logger?.Warn($"Skip merging null graph.");
                return;
            }

            Logger?.Debug($"Merging graph with {other.m_Nodes.Count} nodes, {other.m_Edges.Count} edges.");
            m_Nodes.AddRange(other.m_Nodes);
            m_Edges.AddRange(other.m_Edges);
        }



        /// <summary>
        /// Adds a node to the graph
        /// </summary>
        public void AddEx(N node)
        {
            if (m_Nodes.Contains(node))
                throw new ArgumentException("The specified node is already in the graph");

            m_Nodes.Add(node);
            Logger?.Debug($"Added node: {node}");
        }

        /// <summary>
        /// Adds a node to the graph
        /// </summary>
        // System.Collections.Generic.ICollection<N>.Add(N) implementation
        public void Add(N node)
        {
            try
            {
                AddEx(node);
            }
            catch (Exception ex)
            {
                Logger?.Warn($"Exception on Add:{ex}");
            }
        }

        /// <summary>
        /// Adds nodes to the graph
        /// </summary>
        public void Add(IEnumerable<N> nodes)
        {
            foreach (N node in nodes)
            {
                Add(node);
            }
        }



        /// Adds an edge between two existing nodes in the graph. Throws ArgumentException
        /// if the edge already exists, either node does not exist in the graph, of if the edge is illegal.
        /// <param name="start">The start node for the edge</param>
        /// <param name="end">The end node for the edge</param>
        /// <param name="data">Additional data to attach to the edge</param>
        public Edge<N, E> AddEdgeEx(N start, N end, E data)
        {
            if (!m_bAllowsReflexivity && start.Equals(end))
                throw new ArgumentException("Reflexivity is not allowed");
            if (!m_Nodes.Contains(start))
                throw new ArgumentException("The start node is not in the graph");
            if (!m_Nodes.Contains(end))
                throw new ArgumentException("The end node is not in the graph");
            if (ContainsEdge(start, end))
                throw new ArgumentException("The edge is already in the graph");

            Logger?.Debug($"Adding edge: {start} -> {end} : {data}");
            var edge = new Edge<N, E>(start, end, data);
            m_Edges.Add(edge);
            return edge;
        }

        public Nullable<Edge<N, E>> AddEdge(N start, N end, E data)
        {
            try
            {
                return AddEdgeEx(start, end, data);
            }
            catch (Exception ex)
            {
                Logger?.Warn($"Exception on AddEdge:{ex}");
                return null;
            }
        }


        /// Determines whether the graph contains an edge starting at start, and ending at end
        /// <param name="start">The start node</param>
        /// <param name="end">The end node</param>
        /// <returns></returns>
        public bool ContainsEdge(N start, N end)        {            return GetEdge(start, end).HasValue;        }

        public Nullable<Edge<N, E>> GetEdge(N start, N end)
        {
            if (m_bDirected)
            {
                foreach (Edge<N, E> p in Edges)
                {
                    if (!p.Start.Equals(start))
                        continue;
                    if (!p.End.Equals(end))
                        continue;
                    return p;
                }
                return null;
            }
            else
            {
                foreach (Edge<N, E> p in Edges)
                {
                    if (!p.Start.Equals(start) && !p.Start.Equals(end))
                        continue;
                    if (!p.End.Equals(start) && !p.End.Equals(end))
                        continue;
                    //if (start.Equals(end))
                    //    return p;
                    if (!p.Start.Equals(p.End))
                        return p;
                }
                return null;
            }

        }

        /// Removes the edge from start to end, if it exists. If an edge is removed, returns true.
        /// <param name="start">The start node</param>
        /// <param name="end">The end node</param>
        /// <returns>True iff an edge was removed</returns>
        public bool RemoveEdge(N start, N end)
        {
            for (int edge = m_Edges.Count - 1; edge >= 0; edge--)
            {
                if (FilterEdge != null && FilterEdge(m_Edges[edge]))
                    continue;

                if (m_Edges[edge].Start.Equals(start) && m_Edges[edge].End.Equals(end))
                {
                    Logger?.Debug($"Removing Edge:{edge}");
                    m_Edges.RemoveAt(edge);
                    return true;
                }
                if (!m_bDirected && m_Edges[edge].End.Equals(start) && m_Edges[edge].Start.Equals(end))
                {
                    Logger?.Debug($"Removing Edge:{edge}");
                    m_Edges.RemoveAt(edge);
                    return true;
                }
            }
            return false;
        }

        /// Returns the set of edges from the given node (ie that have it as their start)
        /// <param name="start">The start node</param>
        /// <returns>The set of edges beginning at start</returns>
        public IEnumerable<Edge<N, E>> GetOutgoingEdges(N start)
        {
            foreach (Edge<N, E> e in Edges)
            {
                if (e.Start.Equals(start))
                {
                    yield return e;
                    continue;
                }
                if (!m_bDirected && e.End.Equals(start))
                {
                    yield return e;
                    continue;
                }
            }
        }

        /// Returns the set of edges ending at the given node
        /// <param name="end">The ending node</param>
        /// <returns>The set of edges ending at the given node</returns>
        public IEnumerable<Edge<N, E>> GetIncomingEdges(N end)
        {
            foreach (Edge<N, E> e in Edges)
            {
                if (e.End.Equals(end))
                {
                    yield return e;
                    continue;
                }
                if (!m_bDirected && e.Start.Equals(end))
                {
                    yield return e;
                    continue;
                }
            }
        }

        public IEnumerable<Edge<N, E>> GetEdges(N node)
        {
            foreach (Edge<N, E> e in Edges)
            {
                if (e.Start.Equals(node) || e.End.Equals(node))
                {
                    yield return e;
                    continue;
                }
                if (!m_bDirected && (e.Start.Equals(node) || e.End.Equals(node)))
                {
                    yield return e;
                    continue;
                }
            }
        }



        /// Returns the degree of the specified node; IE the number of edges connected to it.
        /// <param name="node">The node to find the degree of</param>
        /// <returns>The degree of the node</returns>
        public int GetDegree(N node) { return GetIncomingDegree(node) + GetOutgoingDegree(node); }
        public int GetIncomingDegree(N node) { return GetIncomingEdges(node).Count(); }
        public int GetOutgoingDegree(N node) { return m_bDirected ? GetOutgoingEdges(node).Count() : 0; }


        public IEnumerable<N> GetAdjacentNodes(N node) { return GetIncomingNodes(node).Concat(GetOutgoingNodes(node)); }
        /// Returns the set of nodes directly connected to the given node, where the edge starts
        /// at the given node.
        /// <param name="node">A node</param>
        /// <returns>The set of nodes</returns>
        public IEnumerable<N> GetOutgoingNodes(N node)
        {
            if (m_bDirected)
                foreach (Edge<N, E> e in GetOutgoingEdges(node))
                    yield return e.End;
            else
                foreach (Edge<N, E> e in GetOutgoingEdges(node))
                {
                    if (e.Start.Equals(e.End))
                    {
                        yield return node;
                        continue;
                    }
                    if (e.Start.Equals(node))
                        yield return e.End;
                    if (e.End.Equals(node))
                        yield return e.Start;
                }
        }

        /// Returns the set of nodes directly connected to the given node, where the edge ends
        /// at the given node.
        /// <param name="node">A node</param>
        /// <returns>The set of nodes</returns>
        public IEnumerable<N> GetIncomingNodes(N node)
        {
            if (m_bDirected)
                foreach (Edge<N, E> e in GetIncomingEdges(node))
                    yield return e.Start;
            else
                foreach (Edge<N, E> e in GetIncomingEdges(node))
                {
                    if (e.Start.Equals(e.End))
                    {
                        yield return node;
                        continue;
                    }
                    if (e.Start.Equals(node))
                        yield return e.End;
                    if (e.End.Equals(node))
                        yield return e.Start;
                }
        }


        /// Removes all edges and nodes
        public void Clear()
        {
            Logger?.Debug($"Clearing graph.");
            m_Nodes.Clear();
            m_Edges.Clear();
        }

        /// Determines if the graph contains the specified node
        /// <param name="node">The node to look for</param>
        /// <returns>True if the node is in the graph</returns>
        public bool Contains(N node) { return m_Nodes.Contains(node); }

        /// Copies the nodes to an array. Not supported.
        /// <param name="array">An array to copy to</param>
        /// <param name="arrayIndex">The array index to begin at</param>
        public void CopyTo(N[] array, int arrayIndex) { throw new NotSupportedException("CopyTo(T[],int) is not supported on Graph<T,E>"); }


        /// Removes the specified node from the graph, along with all edges connected to it.
        /// see OmitNode(N node) also.
        /// <param name="node">The node to remove</param>
        /// <returns>True iff the node is removed</returns>
        public bool Remove(N node)
        {
            if (!m_Nodes.Remove(node))
                return false;

            Logger?.Debug($"Removing node: {node}.");
            for (int edge = m_Edges.Count - 1; edge >= 0; edge--)
            {
                if (m_Edges[edge].Start.Equals(node) || m_Edges[edge].End.Equals(node))
                    m_Edges.RemoveAt(edge);
            }
            return true;
        }



        /// Returns an enumerator over the nodes.
        /// <returns>An enumerator of nodes</returns>
        public IEnumerator<N> GetEnumerator() { return m_Nodes.GetEnumerator(); }

        /// Returns an enumerator of nodes
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return m_Nodes.GetEnumerator(); }
    }





    /// Implements a graph with nodes of type N, and no attached edge data.
    /// If you need to attach data to edges, use Graph&lt;T,E&gt; instead.
    /// <typeparam name="N">The node type</typeparam>
    public class Graph<N> : Graph<N, object> where N : IEquatable<N>
    {
        /// Constructs a new, empty graph.
        /// <param name="directed">Whether the graph should be directed</param>
        /// <param name="allowsReflexivity">Whether the graph should allow reflexivity</param>
        public Graph(bool directed, bool allowsReflexivity) : base(directed, allowsReflexivity) { }

        /// Constructs a new, empty graph.
        /// <param name="directed">Whether the graph should be directed</param>
        public Graph(bool directed) : base(directed) { }

        /// Constructs a new, empty graph.
        public Graph() : base() { }

        /// Adds an edge between start and end. Throws ArgumentException if the edge is invalid.
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void AddEdge(N start, N end) { base.AddEdge(start, end, null); }
    }
}
