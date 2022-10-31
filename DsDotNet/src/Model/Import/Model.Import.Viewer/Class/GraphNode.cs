using System.Collections.Generic;
using DsEdge = Engine.Core.CoreModule.Edge;
using DsVertex = Engine.Core.CoreModule.Vertex;
using static Engine.Core.GraphModule;
using static Model.Import.Office.InterfaceClass;
using static Engine.Core.CoreModule;
using Engine.Common;
using static Engine.Core.CoreModule.AliasTargetType;
using System.Globalization;
using static Engine.Core.DsType;

namespace Dual.Model.Import
{
    public class DsViewNode
    {
        public bool IsChildExist = false;
        public string UIKey = "";
        public DsVertex DsVertex;
        public Bound Bound;
        public NodeType NodeType;
        public List<DsViewEdge> MEdges = new List<DsViewEdge>();
        public List<DsViewNode> Singles = new List<DsViewNode>();
        public DsViewNode(string name)
        {
            NodeType = NodeType.BUTTON;
            UIKey = $"{name};{this.GetHashCode()}";
        }
        public DsViewNode(DsVertex v)
        {

            UIKey = $"{v.Name};{this.GetHashCode()}";
            DsVertex = v;
            var real = v as Real;
            if (real != null)
            {
                real.Graph.Edges.ForEach(e => MEdges.Add(new DsViewEdge(e)));
                real.Graph.Islands.ForEach(f => Singles.Add(new DsViewNode(f)));
                if (real.Graph.Vertices.Count > 0)
                    IsChildExist = true;

                NodeType = NodeType.MY;
            }

            var call = v as Call;
            if (call != null)
            {
                Singles = new List<DsViewNode>();
                NodeType = NodeType.TR;
            }

            var alias = v as Alias;
            if (alias != null)
            {
                Singles = new List<DsViewNode>();
                if (alias.Target.IsCallTarget)
                {
                    var target = (CallTarget)alias.Target;
                    UIKey = $"{target.Item.Name};{this.GetHashCode()}";
                    NodeType = NodeType.TR;
                }

                if (alias.Target.IsRealTarget)
                {
                    var target = (RealTarget)alias.Target;
                    UIKey = $"{target.Item.Name};{this.GetHashCode()}";
                    NodeType = NodeType.MY;
                }
            }
        }
    }
    public class DsViewEdge
    {
        public string UIKey => "";
        public DsViewNode Source;
        public DsViewNode Target;
        public DsEdge DsEdge;
        public EdgeType Causal = EdgeType.Default;
        
        public DsViewEdge(DsEdge e) { 
            DsEdge = e;
            Source = new DsViewNode(e.Source);
            Target = new DsViewNode(e.Target);

            if (e.EditorInfo.HasFlag(EdgeType.EditorInterlock))
                Causal = EdgeType.EditorInterlock;
            if (e.EditorInfo.HasFlag(EdgeType.EditorStartReset))
                Causal = EdgeType.EditorStartReset;
            else
                Causal = e.EdgeType;
        }

    }

}

