using Engine.Common;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.DsType;
using static Model.Import.Office.Object;
using Color = Microsoft.Msagl.Drawing.Color;

namespace Dual.Model.Import
{
    public partial class UCView : UserControl
    {
        private readonly GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();



        public UCView()
        {
            InitializeComponent();

            viewer.Dock = DockStyle.Fill;
            viewer.PanButtonPressed = true;
            viewer.ToolBarIsVisible = false;

            this.Controls.Add(viewer);


        }

        private Dictionary<Tuple<Seg, Status4>, int> _dicCycle = new Dictionary<Tuple<Seg, Status4>, int>();
        private Dictionary<string, Node> _dicDrawing = new Dictionary<string, Node>();



        public void SetGraph(Flo flow)
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





            //viewer.Graph = new Graph() { LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings() };
            //var layoutSetting = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings();
            //layoutSetting.NodeSeparation = 50;
            //layoutSetting.ClusterMargin = 30;
            //layoutSetting.LogScaleEdgeForces = false;
            //layoutSetting.RepulsiveForceConstant = 0.01;
            //layoutSetting.EdgeRoutingSettings.EdgeRoutingMode = Microsoft.Msagl.Core.Routing.EdgeRoutingMode.SugiyamaSplines;
            //layoutSetting.Decay = 0.8;


            viewer.Graph.LayoutAlgorithmSettings = layoutSetting;

            SetBackColor(System.Drawing.Color.FromArgb(33, 33, 33));

            var subDraws = flow.DrawSubs.ToList();

            flow.NoEdgeSegs.ToList().ForEach(seg => DrawSeg(viewer.Graph.RootSubgraph, seg, subDraws));

            drawMEdgeGraph(flow.Edges.ToList(), subDraws, viewer.Graph.RootSubgraph);

            viewer.SetCalculatedLayout(viewer.CalculateLayout(viewer.Graph));
        }


        private void UpdateLabelText(Node nNode)
        {
            nNode.LabelText = nNode.LabelText.Split(';')[0];
            nNode.Label.FontColor = Color.White;
            nNode.Attr.Color = Color.White;

        }




        private void drawMEdgeGraph(List<MEdge> edges, List<Seg> drawSubs, Subgraph subgraph)
        {
            foreach (var mEdge in edges)
                DrawMEdge(subgraph, mEdge, drawSubs);

        }

        private void DrawMEdge(Subgraph subgraph, MEdge edge, List<Seg> drawSubs)
        {
            MEdge mEdge = edge;

            bool bDrawSubSrc = mEdge.Source.IsChildExist && (drawSubs == null || drawSubs.Contains(mEdge.Source));
            bool bDrawSubTgt = mEdge.Target.IsChildExist && (drawSubs == null || drawSubs.Contains(mEdge.Target));

            var mEdgeSrc = mEdge.Source;
            var mEdgeTgt = mEdge.Target;
            var subGSrc = new Subgraph(mEdgeSrc.UIKey);
            var subGTgt = new Subgraph(mEdgeTgt.UIKey);

            if (bDrawSubSrc) subgraph.AddSubgraph(subGSrc);
            if (bDrawSubTgt) subgraph.AddSubgraph(subGTgt);
            var gEdge = viewer.Graph.AddEdge(subGSrc.Id, "", subGTgt.Id);
            DrawEdgeStyle(gEdge, mEdge, true);
            DrawSub(subgraph, mEdgeSrc, subGSrc, gEdge.SourceNode, bDrawSubSrc);
            DrawSub(subgraph, mEdgeTgt, subGTgt, gEdge.TargetNode, bDrawSubTgt);

        }
        private void DrawSeg(Subgraph subgraph, Seg seg, List<Seg> drawSubs)
        {

            bool bDrawSubSrc = (seg.IsChildExist || seg.NoEdgeSegs.Any()) && (drawSubs == null || drawSubs.Contains(seg));

            var subGSrc = new Subgraph(seg.UIKey);

            if (bDrawSubSrc) subgraph.AddSubgraph(subGSrc);
            var gEdge = viewer.Graph.AddEdge(subGSrc.Id, "", subGSrc.Id);
            UpdateLabelText(gEdge.SourceNode);
            UpdateNodeView(gEdge.SourceNode, seg);
            gEdge.IsVisible = false;

            DrawSub(subgraph, seg, subGSrc, gEdge.SourceNode, bDrawSubSrc);

        }

        private void DrawSub(Subgraph subgraph, Seg seg, Subgraph subG, Node gNode, bool bDrawSub)
        {
            if (_dicDrawing.ContainsKey(gNode.Id)) return;
            else _dicDrawing.Add(gNode.Id, gNode);

            if (bDrawSub && (seg.MEdges.Any() || seg.NoEdgeSegs.Any()))
            {
                if (seg.MEdges.Any())
                    drawMEdgeGraph(seg.MEdges.ToList(), null, subG);

                seg.NoEdgeSegs.ToList().ForEach(subSeg => DrawSeg(subG, subSeg, null));
            }
            else
                subgraph.AddNode(gNode);
        }


        private void DrawEdgeStyle(Edge gEdge, MEdge edge, bool model = false)
        {
            //gEdge.Attr.Color = Color.Black;
            //gEdge.Label.FontColor = Color.White;
            gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Generalization;

            gEdge.Attr.Color = Color.White;

            if (edge.Causal == EdgeCausal.SEdge)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.Color = Color.DeepSkyBlue;
                gEdge.Attr.LineWidth = 2;
            }
            else if (edge.Causal == EdgeCausal.SPush)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.DeepSkyBlue;
            }
            else if (edge.Causal == EdgeCausal.REdge)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.Color = Color.Green;
                gEdge.Attr.LineWidth = 2;
            }
            else if (edge.Causal == EdgeCausal.RPush)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.Green;
            }
            else if (edge.Causal == EdgeCausal.Interlock)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }
            else if (edge.Causal == EdgeCausal.SReset)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Tee;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }

            UpdateLabelText(gEdge.SourceNode);
            UpdateLabelText(gEdge.TargetNode);

            if (model)
            {

                var src = edge.Source as Seg;
                var tgt = edge.Target as Seg;

                UpdateNodeView(gEdge.SourceNode, src);
                UpdateNodeView(gEdge.TargetNode, tgt);

            }
        }

        private void UpdateNodeView(Node nNode, Seg segment)
        {
            {
                //nNode.Attr.Color = Color.DarkGoldenrod;

                if (segment.Bound == Bound.ExBtn)
                    nNode.Attr.Shape = Shape.Plaintext;
                else
                {
                    if (segment.NodeCausal == NodeCausal.MY)
                        nNode.Attr.Shape = Shape.Box;
                    if (segment.NodeCausal == NodeCausal.EX)
                        nNode.Attr.Shape = Shape.Diamond;
                    if (segment.NodeCausal == NodeCausal.TR)
                        nNode.Attr.Shape = Shape.Ellipse;
                    if (segment.NodeCausal == NodeCausal.TX)
                        nNode.Attr.Shape = Shape.Ellipse;
                    if (segment.NodeCausal == NodeCausal.RX)
                        nNode.Attr.Shape = Shape.Ellipse;
                }
            }
        }

        public void RefreshGraph() { viewer.Do(() => viewer.Refresh()); }


        public void Update(Seg seg)
        {

            Node node = viewer.Graph.FindNode(seg.UIKey);
            if (node == null)
            {
                if (viewer.Graph.SubgraphMap.ContainsKey(seg.UIKey))
                    node = viewer.Graph.SubgraphMap[seg.UIKey];
                else
                    return;
            }
            //node.Attr.Color = Color.White;
            //node.Label.FontColor = Color.White;
            if (seg != null)
            {
                if (seg.NodeCausal == NodeCausal.MY)
                    UpdateLineColor(seg.Status4, node);
                else
                    UpdateFillColor(seg.Status4, node);
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
