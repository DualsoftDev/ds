using Engine.Common;
using Engine.Core;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphmapsWithMesh;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeModule;
using static Engine.Core.GraphModule;
using Color = Microsoft.Msagl.Drawing.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Vertex = Engine.Core.CoreModule.Vertex;
using DsEdge = Engine.Core.CoreModule.Edge;
using static Model.Import.Office.InterfaceClass;

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

        //private Dictionary<Tuple<MSeg, Status4>, int> _dicCycle = new Dictionary<Tuple<MSeg, Status4>, int>();
        private Dictionary<string, Node> _dicDrawing = new Dictionary<string, Node>();



        public void SetGraph(Flow flow)
        {
            //sub 그래프 불가
            //viewer.Graph.LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
            //viewer.Graph.LayoutAlgorithmSettings = new RankingLayoutSettings();
            //sub 그래프 가능
            viewer.Graph = new Graph() { LayoutAlgorithmSettings = new SugiyamaLayoutSettings() };
            var layoutSetting = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings();
            layoutSetting.LayerSeparation = 30;
            layoutSetting.NodeSeparation = 30;
            layoutSetting.ClusterMargin = 20;

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

            flow.Graph.Vertices
                .ForEach(seg => DrawSeg(viewer.Graph.RootSubgraph, new DsViewNode(seg)));

            var dsEdges = flow.Graph.Edges.Select(s => new DsViewEdge(s));

            drawMEdgeGraph(dsEdges, viewer.Graph.RootSubgraph);

            viewer.SetCalculatedLayout(viewer.CalculateLayout(viewer.Graph));
        }


        private void UpdateLabelText(Node nNode)
        {
            nNode.LabelText = nNode.LabelText.Split(';')[0];
            nNode.Label.FontColor = Color.White;
            nNode.Attr.Color = Color.White;

        }




        private void drawMEdgeGraph(IEnumerable<DsViewEdge> edges, Subgraph subgraph)
        {
            edges.ForEach(f =>
            {
                //  if (!f.IsSkipUI)
                DrawMEdge(subgraph, f);
            });
        }

        private void DrawMEdge(Subgraph subgraph, DsViewEdge edge)
        {
            DsViewEdge mEdge = edge;

            bool bDrawSubSrc = mEdge.Source.IsChildExist;
            bool bDrawSubTgt = mEdge.Target.IsChildExist;

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
        private void DrawSeg(Subgraph subgraph, DsViewNode seg)
        {

            //bool bDrawSub = (seg.IsChildExist || seg.Singles.Any());

            //var subG = new Subgraph(seg.UIKey);

            //if (bDrawSub) subgraph.AddSubgraph(subG);
            //var gEdge = viewer.Graph.AddEdge(subG.Id, "", subG.Id);
            //UpdateLabelText(gEdge.SourceNode);
            //UpdateNodeView(gEdge.SourceNode, seg);
            //gEdge.IsVisible = false;

            //DrawSub(subgraph, seg, subG, gEdge.SourceNode, bDrawSub);

        }

        private void DrawSub(Subgraph subgraph, DsViewNode seg, Subgraph subG, Node gNode, bool bDrawSub)
        {
            if (_dicDrawing.ContainsKey(gNode.Id)) return;
            else _dicDrawing.Add(gNode.Id, gNode);

            if (bDrawSub && (seg.MEdges.Any() || seg.Singles.Any()))
            {
                if (seg.MEdges.Any())
                    drawMEdgeGraph(seg.MEdges.ToList(), subG);

                seg.Singles.ToList().ForEach(subSeg => DrawSeg(subG, subSeg));
            }
            else
                subgraph.AddNode(gNode);
        }


        private void DrawEdgeStyle(Edge gEdge, DsViewEdge edge, bool model = false)
        {
            //gEdge.Attr.Color = Color.Black;
            //gEdge.Label.FontColor = Color.White;
            gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Generalization;

            gEdge.Attr.Color = Color.White;

            var et = edge.Causal;
            if (et == EdgeType.Default)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.Color = Color.DeepSkyBlue;
                gEdge.Attr.LineWidth = 2;
            }
            else if (et == (EdgeType.Default | EdgeType.Strong))
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.DeepSkyBlue;
            }
            else if (et == EdgeType.Reset)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.Color = Color.Green;
                gEdge.Attr.LineWidth = 2;
            }
            else if (et == (EdgeType.Reset | EdgeType.Strong))
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.Green;
            }

            else if (edge.Causal == UtilEdge.Interlock)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }
            else if (edge.Causal == UtilEdge.StartReset)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Tee;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }

            UpdateLabelText(gEdge.SourceNode);
            UpdateLabelText(gEdge.TargetNode);

            if (model)
            {

                var src = edge.Source;
                var tgt = edge.Target;

                UpdateNodeView(gEdge.SourceNode, src);
                UpdateNodeView(gEdge.TargetNode, tgt);

            }
        }

        private void UpdateNodeView(Node nNode, DsViewNode segment)
        {
            {
                //nNode.Attr.Color = Color.DarkGoldenrod;

                if (segment.Bound == Bound.ExBtn)
                    nNode.Attr.Shape = Shape.Plaintext;
                else
                {
                    if (segment.NodeType == NodeType.MY)
                        nNode.Attr.Shape = Shape.Box;
                    if (segment.NodeType == NodeType.DUMMY)
                    {
                        nNode.Attr.Shape = Shape.Box;
                        nNode.Attr.FillColor = Color.Black;
                    }
                    if (segment.NodeType == NodeType.TR
                        || segment.NodeType == NodeType.TX
                        || segment.NodeType == NodeType.RX)
                        nNode.Attr.Shape = Shape.Ellipse;
                    if (segment.NodeType == NodeType.IF)
                        nNode.Attr.Shape = Shape.InvHouse;
                    if (segment.NodeType == NodeType.COPY)
                        nNode.Attr.Shape = Shape.Octagon;
                }
            }
        }

        public void RefreshGraph() { viewer.Do(() => viewer.Refresh()); }


        public void Update(CoreModule.Vertex seg)
        {

            //Node node = viewer.Graph.FindNode(seg.UIKey);
            //if (node == null)
            //{
            //    if (viewer.Graph.SubgraphMap.ContainsKey(seg.UIKey))
            //        node = viewer.Graph.SubgraphMap[seg.UIKey];
            //    else
            //        return;
            //}
            ////node.Attr.Color = Color.White;
            ////node.Label.FontColor = Color.White;
            //if (seg != null)
            //{
            //    if (seg.NodeType == NodeType.MY)
            //        UpdateLineColor(seg.Status4, node);
            //    else
            //        UpdateFillColor(seg.Status4, node);
            //}
            //else
            //{

            //}

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
