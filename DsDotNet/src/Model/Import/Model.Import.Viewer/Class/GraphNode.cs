using System.Collections.Generic;
using DsEdge = Engine.Core.CoreModule.Edge;
using DsVertex = Engine.Core.CoreModule.Vertex;
using static Engine.Core.GraphModule;
using static Model.Import.Office.InterfaceClass;
using Microsoft.Msagl.Core.DataStructures;
using static Engine.Core.CoreModule;
using System;
using Engine.Common;

namespace Dual.Model.Import
{
    public class DsViewNode
    {
        public bool IsChildExist =false;
        public string UIKey = "";
        public DsVertex DsVertex;
        public Bound Bound;
        public NodeType NodeType;
        public List<DsViewEdge> MEdges;
        public List<DsViewNode> Singles;
        
        public DsViewNode(DsVertex v) {

            UIKey = $"{v.Name};{this.GetHashCode()}";
            DsVertex = v;
            var real = v as Real;
            if (real != null)
            {
                real.Graph.Vertices.ForEach(f => Singles.Add(new DsViewNode(f)));
                if (real.Graph.Vertices.Count > 0) 
                    IsChildExist = true;
            }

            var call = v as Call;
            if (call != null)
                Singles = new List<DsViewNode>();
        }

    }
    public class DsViewEdge
    {
        public string UIKey => "";
        public DsViewNode Source;
        public DsViewNode Target;
        public DsEdge DsEdge;
        public EdgeType Causal;
        
        public DsViewEdge(DsEdge e) { 
            DsEdge = e;
            Source = new DsViewNode(e.Source);
            Target = new DsViewNode(e.Target);
            Causal = e.EdgeType;

        }

    }

}

