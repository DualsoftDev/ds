using Engine.Common;
using Engine.Core;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Base.DsType;
using Color = Microsoft.Msagl.Drawing.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;

namespace Model.Simulator
{
    public partial class UCSim : UserControl
    {
        private readonly GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();

        public UCSim()
        {
            InitializeComponent();

            viewer.Dock = DockStyle.Fill;
            viewer.PanButtonPressed = true;
            viewer.ToolBarIsVisible = false;

            this.Controls.Add(viewer);
        }

        private Dictionary<string, Node> _dicDrawing = new Dictionary<string, Node>();

        public void SetGraph(Flow flow)
        {
            //sub 그래프 불가
            //viewer.Graph.LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
            //viewer.Graph.LayoutAlgorithmSettings = new RankingLayoutSettings();
            //sub 그래프 가능
            viewer.Graph = new Graph() { LayoutAlgorithmSettings = new SugiyamaLayoutSettings() };
            var layoutSetting = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings();
            layoutSetting.LayerSeparation = 50;
            layoutSetting.NodeSeparation = 50;
            layoutSetting.ClusterMargin = 30;

            viewer.Graph.LayoutAlgorithmSettings = layoutSetting;
            SetBackColor(System.Drawing.Color.FromArgb(240, 240, 240));
            //var subDraws = flow.Edges.ToList();
            //var a = flow.CollectArrow();

            //flow.NoEdgeSegs.ToList().ForEach(seg => DrawSeg(viewer.Graph.RootSubgraph, seg, subDraws));
            drawMEdgeGraph(flow.CollectArrow(), viewer.Graph.RootSubgraph);

            viewer.SetCalculatedLayout(viewer.CalculateLayout(viewer.Graph));
        }

        private void drawMEdgeGraph(IEnumerable<FlowExtension.Causal> edgeCausal, Subgraph subgraph)
        {
            foreach (var edge in edgeCausal)
                DrawMEdge(subgraph, edge);
        }

        private void DrawMEdge(Subgraph subgraph, FlowExtension.Causal edge)
        {
            SegmentBase segSrc = edge.Source as SegmentBase;
            SegmentBase segTgr = edge.Target as SegmentBase;

            var subGSrc = new Subgraph(edge.Source.GetName());
            var subGTgt = new Subgraph(edge.Target.GetName());
            bool hasChildSrc = segSrc != null && segSrc.Children.Any();
            bool hasChildTgt = segTgr != null && segTgr.Children.Any();

            if (hasChildSrc) subgraph.AddSubgraph(subGSrc);
            if (hasChildTgt) subgraph.AddSubgraph(subGTgt);

            var gEdge = viewer.Graph.AddEdge(subGSrc.Id, "", subGTgt.Id);
            DrawEdgeStyle(gEdge, edge, true);
            DrawSub(subgraph, segSrc, subGSrc, gEdge.SourceNode, hasChildSrc);
            DrawSub(subgraph, segTgr, subGTgt, gEdge.TargetNode, hasChildTgt);
        }


        private void UpdateLabelText(Node nNode)
        {
            nNode.LabelText = nNode.LabelText.Split(';')[0];
            nNode.Label.FontColor = Color.Black;
            nNode.Attr.Color = Color.Black;

        }
        private void DrawSub(Subgraph subgraph, SegmentBase seg, Subgraph subG, Node gNode, bool bDrawSub)
        {
            if (_dicDrawing.ContainsKey(gNode.Id)) return;
            else _dicDrawing.Add(gNode.Id, gNode);

            if (bDrawSub && seg.ChildVertices.Any())
            {
                drawMEdgeGraph(seg.CollectArrow(), subG);

                //seg.NoEdgeSegs.ToList().ForEach(subSeg => DrawSeg(subG, subSeg, null));
            }
            else
                subgraph.AddNode(gNode);
        }


        private void DrawEdgeStyle(Edge gEdge, FlowExtension.Causal edge, bool model = false)
        {
            //gEdge.Attr.Color = Color.Black;
            //gEdge.Label.FontColor = Color.White;
            gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Generalization;
            gEdge.Attr.Color = Color.Black;

            if (edge.EdgeCausal == EdgeCausal.SEdge)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.Color = Color.DeepSkyBlue;
                gEdge.Attr.LineWidth = 2;
            }
            else if (edge.EdgeCausal == EdgeCausal.SPush)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.DeepSkyBlue;
            }
            else if (edge.EdgeCausal == EdgeCausal.REdge)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.Color = Color.Green;
                gEdge.Attr.LineWidth = 2;
            }
            else if (edge.EdgeCausal == EdgeCausal.RPush)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.Green;
            }
            else if (edge.EdgeCausal == EdgeCausal.Interlock)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }
            else if (edge.EdgeCausal == EdgeCausal.SReset)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Tee;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }

            UpdateLabelText(gEdge.SourceNode);
            UpdateLabelText(gEdge.TargetNode);

            if (model)
            {
                var src = edge.Source as SegmentBase;
                var tgt = edge.Target as SegmentBase;

                UpdateNodeView(gEdge.SourceNode, src == null ? NodeCausal.TR : NodeCausal.MY);
                UpdateNodeView(gEdge.TargetNode, tgt == null ? NodeCausal.TR : NodeCausal.MY);
            }
        }

        private void UpdateNodeView(Node nNode, NodeCausal nodeCausal)
        {
            nNode.Attr.Color = Color.DarkGoldenrod;

            if (nodeCausal == NodeCausal.MY)
                nNode.Attr.Shape = Shape.Box;
            if (nodeCausal == NodeCausal.EX)
                nNode.Attr.Shape = Shape.Diamond;
            if (nodeCausal == NodeCausal.TR)
                nNode.Attr.Shape = Shape.Ellipse;
            if (nodeCausal == NodeCausal.TX)
                nNode.Attr.Shape = Shape.Ellipse;
            if (nodeCausal == NodeCausal.RX)
                nNode.Attr.Shape = Shape.Ellipse;

        }

        public void RefreshGraph() { viewer.Do(() => viewer.Refresh()); }


        internal void Update(IVertex seg, Status4 status4)
        {
            Node node = viewer.Graph.FindNode(seg.GetName());
            if (node == null)
            {
                if (viewer.Graph.SubgraphMap.ContainsKey(seg.GetName()))
                    node = viewer.Graph.SubgraphMap[seg.GetName()];
                else
                    return;
            }
            node.Attr.Color = Color.Black;
            node.Label.FontColor = Color.Black;
            if (seg != null)
            {
                //if (seg.NodeCausal == NodeCausal.MY)
                //    UpdateLineColor(seg.Status, node);
                //else
                UpdateFillColor(status4, node);
            }
            else
            {

            }

            RefreshGraph();
        }

        private static void UpdateFontColor(Status4 newStatus, Node node)
        {
            if (newStatus == Status4.Ready) node.Label.FontColor = Color.DarkGreen;
            else if (newStatus == Status4.Going) node.Label.FontColor = Color.DarkKhaki;
            else if (newStatus == Status4.Finish) node.Label.FontColor = Color.DarkBlue;
            else if (newStatus == Status4.Homing) node.Label.FontColor = Color.Black;
        }

        private static void UpdateLineColor(Status4 newStatus, Node node)
        {
            if (newStatus == Status4.Ready) node.Attr.Color = Color.DarkOliveGreen;
            else if (newStatus == Status4.Going) node.Attr.Color = Color.DarkGoldenrod;
            else if (newStatus == Status4.Finish) node.Attr.Color = Color.DarkBlue;
            else if (newStatus == Status4.Homing) node.Attr.Color = Color.DimGray;
        }

        private static void UpdateFillColor(Status4 newStatus, Node node)
        {
            if (newStatus == Status4.Ready) node.Attr.FillColor = Color.DarkOliveGreen;
            else if (newStatus == Status4.Going) node.Attr.FillColor = Color.DarkGoldenrod;
            else if (newStatus == Status4.Finish) node.Attr.FillColor = Color.DarkBlue;
            else if (newStatus == Status4.Homing) node.Attr.FillColor = Color.DimGray;
        }

        internal void SetBackColor(System.Drawing.Color color)
        {
            // var backColor = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
            //listBoxControl_Que.BackColor = backColor;
            var gColor = Color.Red;
            gColor.R = color.R;
            gColor.G = color.G;
            gColor.B = color.B;
            viewer.Graph.Attr.BackgroundColor = gColor;
        }


    }
}
