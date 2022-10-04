using Engine.Common;
using Engine.Core;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.Interface;
using static Model.Import.Office.Object;
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
            drawMEdgeGraph(flow.Graph.Edges, viewer.Graph.RootSubgraph);

            viewer.SetCalculatedLayout(viewer.CalculateLayout(viewer.Graph));
        }

        private void drawMEdgeGraph(IEnumerable<InFlowEdge> edges, Subgraph subgraph)
        {
            foreach (var edge in edges)
                DrawMEdge(subgraph, edge, null);
        }

        private void drawMEdgeGraph(IEnumerable<InSegmentEdge> edges, Subgraph subgraph)
        {
            foreach (var edge in edges)
                DrawMEdge(subgraph, null, edge);
        }

        private void DrawMEdge(Subgraph subgraph, InFlowEdge edge, InSegmentEdge segEdge)
        {
            Segment segSrc = edge.Source as Segment;
            Segment segTgr = edge.Target as Segment;

            var subGSrc = new Subgraph(segSrc.Name);
            var subGTgt = new Subgraph(segTgr.Name);
            bool hasChildSrc = segSrc != null && segSrc.Graph.Vertices.Any();
            bool hasChildTgt = segTgr != null && segTgr.Graph.Vertices.Any();

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
        private void DrawSub(Subgraph subgraph, Segment seg, Subgraph subG, Node gNode, bool bDrawSub)
        {
            if (_dicDrawing.ContainsKey(gNode.Id)) return;
            else _dicDrawing.Add(gNode.Id, gNode);

            if (bDrawSub && seg.Graph.Vertices.Any())
            {
                drawMEdgeGraph(seg.Graph.Edges, subG);

                //seg.NoEdgeSegs.ToList().ForEach(subSeg => DrawSeg(subG, subSeg, null));
            }
            else
                subgraph.AddNode(gNode);
        }


        private void DrawEdgeStyle(Edge gEdge, InFlowEdge edge, bool model = false)
        {
            //gEdge.Attr.Color = Color.Black;
            //gEdge.Label.FontColor = Color.White;
            gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Generalization;
            gEdge.Attr.Color = Color.Black;

            if (EdgeHelper.GetEdgeCausal(edge.EdgeType) == EdgeCausal.SEdge)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.Color = Color.DeepSkyBlue;
                gEdge.Attr.LineWidth = 2;
            }
            else if (EdgeHelper.GetEdgeCausal(edge.EdgeType) == EdgeCausal.SPush)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.DeepSkyBlue;
            }
            else if (EdgeHelper.GetEdgeCausal(edge.EdgeType) == EdgeCausal.REdge)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.Color = Color.Green;
                gEdge.Attr.LineWidth = 2;
            }
            else if (EdgeHelper.GetEdgeCausal(edge.EdgeType) == EdgeCausal.RPush)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.Green;
            }
            else if (EdgeHelper.GetEdgeCausal(edge.EdgeType) == EdgeCausal.Interlock)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }
            else if (EdgeHelper.GetEdgeCausal(edge.EdgeType) == EdgeCausal.SReset)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Tee;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }

            UpdateLabelText(gEdge.SourceNode);
            UpdateLabelText(gEdge.TargetNode);

            if (model)
            {
                var src = edge.Source as Segment;
                var tgt = edge.Target as Segment;

                UpdateNodeView(gEdge.SourceNode, src == null ? NodeType.TR : NodeType.MY);
                UpdateNodeView(gEdge.TargetNode, tgt == null ? NodeType.TR : NodeType.MY);
            }
        }

        private void UpdateNodeView(Node nNode, NodeType NodeType)
        {
            nNode.Attr.Color = Color.DarkGoldenrod;

            if (NodeType == NodeType.MY)
                nNode.Attr.Shape = Shape.Box;
            if (NodeType == NodeType.TR)
                nNode.Attr.Shape = Shape.Ellipse;
            if (NodeType == NodeType.TX)
                nNode.Attr.Shape = Shape.Ellipse;
            if (NodeType == NodeType.RX)
                nNode.Attr.Shape = Shape.Ellipse;

        }

        public void RefreshGraph() { viewer.Do(() => viewer.Refresh()); }


        internal void Update(IVertex iSeg, Status4 status4)
        {
            var seg = iSeg as SegmentBase;
            Node node = viewer.Graph.FindNode(seg.Name);
            if (node == null)
            {
                if (viewer.Graph.SubgraphMap.ContainsKey(seg.Name))
                    node = viewer.Graph.SubgraphMap[seg.Name];
                else
                    return;
            }
            node.Attr.Color = Color.Black;
            node.Label.FontColor = Color.Black;
            if (seg != null)
            {
                //if (seg.NodeType == NodeType.MY)
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
