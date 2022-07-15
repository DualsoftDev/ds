using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuickGraph;

namespace Engine.Core
{
    public class QgEdge : QuickGraph.IEdge<IVertex>
    {
        public IVertex Source { get; }
        public IVertex Target { get; }
        public Edge OriginalEdge { get; }
        public QgEdge(IVertex source, IVertex target, Edge originalEdge)
        {
            OriginalEdge = originalEdge;
            Source = source;
            Target = target;
        }
    }
    public class VertexAndOutgoingEdges
    {
        public IVertex Vertex { get; internal set; }
        public Edge[] OutgoingEdges { get; }
        public VertexAndOutgoingEdges(IVertex vertex, Edge[] outgoingEdges)
        {
            Vertex = vertex;
            OutgoingEdges = outgoingEdges;
        }
    }

    public class GraphInfo
    {
        public Flow[] Flows { get; }
        public virtual Edge[] Edges { get; }
        public virtual IVertex[] Vertices { get; protected set; }
        public virtual QgEdge[] QgEdges { get; protected set; }
        public virtual AdjacencyGraph<IVertex, QgEdge> Graph { get; protected set; }
        public virtual AdjacencyGraph<IVertex, QgEdge> SolidGraph { get; protected set; }
        public virtual UndirectedGraph<IVertex, QgEdge> UndirectedGraph { get; protected set; }
        public virtual UndirectedGraph<IVertex, QgEdge> UndirectedSolidGraph { get; protected set; }
        public virtual IVertex[] Inits { get; protected set; }
        public virtual IVertex[] Lasts { get; protected set; }

        /// Reset edge 까지 고려하였을 때의 connected component
        public virtual IVertex[][] ConnectedComponets { get; protected set; }

        /// Reset edge 제외한 상태의 connected component
        public virtual IVertex[][] SolidConnectedComponets { get; protected set; }

        public virtual VertexAndOutgoingEdges[] TraverseOrders { get; protected set; }

        public GraphInfo(IEnumerable<Flow> flows)
        {
            Flows = flows.ToArray();
        }
    }
}
