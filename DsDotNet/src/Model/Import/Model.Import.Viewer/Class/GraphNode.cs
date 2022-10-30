using System.Collections.Generic;
using DsEdge = Engine.Core.CoreModule.Edge;
using DsVertex = Engine.Core.CoreModule.Vertex;
using static Engine.Core.GraphModule;
using static Model.Import.Office.InterfaceClass;

namespace Dual.Model.Import
{
    public class DsViewNode
    {
        public bool IsChildExist => true;
        public string UIKey => "";
        public DsVertex DsVertex;
        public Bound Bound;
        public NodeType NodeType;
        public List<DsViewEdge> MEdges;
        public List<DsViewNode> Singles;
        
        public DsViewNode(DsVertex v) { DsVertex = v; }

    }
    public class DsViewEdge
    {
        public bool IsChildExist => true;
        public string UIKey => "";
        public DsViewNode Source;
        public DsViewNode Target;
        public DsEdge DsEdge;
        public EdgeType Causal;
        
        public DsViewEdge(DsEdge e) { DsEdge = e; }

    }

}

