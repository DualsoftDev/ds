using System.Collections.Generic;
using Engine.Common;
using Engine.Core;
using DsEdge = Engine.Core.CoreModule.Edge;
using DsVertex = Engine.Core.CoreModule.Vertex;
using static Model.Import.Office.InterfaceClass;
using static Engine.Core.CoreModule;
using static Engine.Core.CoreModule.AliasTargetType;
using static Engine.Core.DsType;
using static Engine.Core.DsText;
using static Engine.Core.ModelingEdgeExt;
using System.Linq;
using Engine.Common.FS;
using Model.Import.Office;
using static Model.Import.Office.PPTDummyModule;
using System;

namespace Dual.Model.Import
{
    public class DsViewNode
    {
        public bool IsChildExist = false;
        public bool IsGroup= false;
        public string UIKey = "";
        public DsVertex DsVertex;
        public Bound Bound;
        public NodeType NodeType;
        public BtnType BtnType;
        public List<DsViewEdge> MEdges = new List<DsViewEdge>();
        public List<DsViewNode> Singles = new List<DsViewNode>();
        public DsViewNode(string name, bool bGroup, BtnType btnType)
        {
            Bound = Bound.ExBtn;
            NodeType = NodeType.BUTTON;
            UIKey = $"{name};{this.GetHashCode()}";
            IsGroup = bGroup;
            BtnType = btnType;
        }
        public DsViewNode(string name, bool bGroup)
        {
            NodeType = NodeType.IF;
            UIKey = $"{name};{this.GetHashCode()}";
            IsGroup = bGroup;
        }
        public DsViewNode(string dummyName)
        {
            NodeType = NodeType.DUMMY;
            UIKey = $";{dummyName}";
            IsChildExist = true;
        }

        public DsViewNode(DsVertex v)
        {
            UIKey = $"{v.Name};{v.QualifiedName}";
            DsVertex = v;
            var real = v as Real;
            if (real != null)
            {
                real.Flow.ModelingEdges
                    .Where(w => w.Source.Parent.GetCore() == real && w.Target.Parent.GetCore() == real)
                    .ForEach(e => MEdges.Add(new DsViewEdge(e)));

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
                    UIKey = $"{target.Item.Name};{v.QualifiedName}";
                    NodeType = NodeType.TR;
                }

                if (alias.Target.IsRealTarget)
                {
                    var target = (RealTarget)alias.Target;
                    UIKey = $"{target.Item.Name};{v.QualifiedName}";
                    NodeType = NodeType.MY;
                }
            }
        }
    }


    public class DsViewEdge
    {
        public DsViewNode Source;
        public DsViewNode Target;
        public DsEdge DsEdge;
        public ModelingEdgeType Causal = ModelingEdgeType.StartEdge;

        public DsViewEdge(ModelingEdgeInfo<Vertex> modelEdgeInfo)
        {
            var mei = modelEdgeInfo;
            Source = new DsViewNode(mei.Source);
            Target = new DsViewNode(mei.Target);
            Causal = mei.EdgeSymbol.ToModelEdge();
        }

  
        //public DsViewEdge(pptDummy pptDummy, ModelingEdgeInfo<string> edge, Dictionary<string, Tuple<pptDummy, DsViewNode>> dummyNodes)
        //{
        //    var src = pptDummy.GetVertex(edge.Source);
        //    var tgt = pptDummy.GetVertex(edge.Target);

        //    if (src == null)
        //    {
        //        var dummy = dummyNodes[edge.Source].Item1;
        //        Source = dummyNodes[edge.Source].Item2;
        //        Source.Singles.AddRange(dummy.Members.Select(f=>new DsViewNode(f)));
        //    }
        //    else
        //        Source = new DsViewNode(src);
        //    if (tgt == null)
        //    {
        //        var dummy = dummyNodes[edge.Target].Item1;
        //        Target = dummyNodes[edge.Target].Item2;
        //        Target.Singles.AddRange(dummy.Members.Select(f => new DsViewNode(f)));
        //    }
        //    else
        //        Target = new DsViewNode(tgt);

        //    Causal = edge.EdgeSymbol.ToModelEdge();
        //}

    
    }
}

