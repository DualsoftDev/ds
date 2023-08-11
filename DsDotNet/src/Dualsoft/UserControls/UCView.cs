using Engine.Common;
using Engine.Core;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Microsoft.Msagl.Layout.Layered;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsText;
using static Engine.Core.DsType;
using static Model.Import.Office.InterfaceClass;
using static Model.Import.Office.PPTDummyModule;
using static Model.Import.Office.ViewModule;
using Color = Microsoft.Msagl.Drawing.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Vertex = Engine.Core.CoreModule.Vertex;


namespace Dualsoft
{
    public partial class UcView : UserControl
    {
        private readonly GViewer viewer = new Microsoft.Msagl.GraphViewerGdi.GViewer();

        public Flow Flow { get; set; }
        public UcView()
        {
            InitializeComponent();

            viewer.Dock = DockStyle.Fill;
            viewer.PanButtonPressed = true;
            viewer.ToolBarIsVisible = false;

            this.Controls.Add(viewer);


        }

        //private Dictionary<Tuple<MSeg, Status4>, int> _dicCycle = new Dictionary<Tuple<MSeg, Status4>, int>();
        private Dictionary<string, Node> _dicDrawing = new Dictionary<string, Node>();

        bool IsDummyMember(List<pptDummy> lstDummy, Vertex vertex)
        {
            return lstDummy.Where(w => w.Members.Contains(vertex)).Count() > 0;
        }

        public void SetGraph(ViewNode viewNode, Flow flow)
        {
            Flow = flow;
            //sub 그래프 불가
            //viewer.Graph.LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
            //viewer.Graph.LayoutAlgorithmSettings = new RankingLayoutSettings();
            //sub 그래프 가능
            viewer.Graph = new Graph() { LayoutAlgorithmSettings = new SugiyamaLayoutSettings() };
            var layoutSetting = new Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings();
            layoutSetting.LayerSeparation = 20;
            layoutSetting.NodeSeparation = 20;
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

            viewNode.Singles.ForEach(f => DrawSeg(viewer.Graph.RootSubgraph, f));
            viewNode.Edges.ForEach(f => DrawMEdge(viewer.Graph.RootSubgraph, f));

            viewer.SetCalculatedLayout(viewer.CalculateLayout(viewer.Graph));
        }

        private void UpdateLabelText(Node nNode)
        {
            nNode.LabelText = nNode.LabelText.Split(';')[0];
            nNode.Label.FontColor = Color.White;
            nNode.Attr.Color = Color.White;

        }

        private void DrawMEdge(Subgraph subgraph, ModelingEdgeInfo<ViewNode> edge)
        {

            bool bDrawSubSrc = edge.Sources[0].IsChildExist;
            bool bDrawSubTgt = edge.Targets[0].IsChildExist;

            var mEdgeSrc = edge.Sources[0];
            var mEdgeTgt = edge.Targets[0];
            var subGSrc = new Subgraph(mEdgeSrc.UIKey);
            var subGTgt = new Subgraph(mEdgeTgt.UIKey);

            if (bDrawSubSrc) subgraph.AddSubgraph(subGSrc);
            if (bDrawSubTgt) subgraph.AddSubgraph(subGTgt);
            var gEdge = viewer.Graph.AddEdge(subGSrc.Id, "", subGTgt.Id);
            DrawEdgeStyle(gEdge, edge, true);
            DrawSub(subgraph, mEdgeSrc, subGSrc, gEdge.SourceNode, bDrawSubSrc);
            DrawSub(subgraph, mEdgeTgt, subGTgt, gEdge.TargetNode, bDrawSubTgt);

        }

        private Subgraph DrawSeg(Subgraph parentGraph, ViewNode viewNode)
        {

            var subGraph = new Subgraph(viewNode.UIKey);

            if (viewNode.IsChildExist) parentGraph.AddSubgraph(subGraph);
            var gEdge = viewer.Graph.AddEdge(subGraph.Id, "", subGraph.Id);
            UpdateLabelText(gEdge.SourceNode);
            UpdateNodeView(gEdge.SourceNode, viewNode);
            gEdge.IsVisible = false;

            DrawSub(parentGraph, viewNode, subGraph, gEdge.SourceNode, viewNode.IsChildExist);

            return subGraph;
        }

        private void DrawSub(Subgraph parentGraph, ViewNode viewNode, Subgraph subG, Node gNode, bool bDrawSub)
        {
            if (_dicDrawing.ContainsKey(gNode.Id)) return;
            else _dicDrawing.Add(gNode.Id, gNode);

            if (bDrawSub)
            {
                viewNode.Edges.ForEach(f =>
                {
                    DrawMEdge(subG, f);
                });

                viewNode.Singles.ForEach(subSeg => DrawSeg(subG, subSeg));


            }
            else
                parentGraph.AddNode(gNode);
        }


        private void DrawEdgeStyle(Edge gEdge, ModelingEdgeInfo<ViewNode> edge, bool model = false)
        {
            //gEdge.Attr.Color = Color.Black;
            //gEdge.Label.FontColor = Color.White;
            gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Generalization;

            gEdge.Attr.Color = Color.White;
            var et = edge.EdgeSymbol.ToModelEdge();
            if (et == ModelingEdgeType.StartEdge)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.Color = Color.DeepSkyBlue;
                gEdge.Attr.LineWidth = 2;
            }
            else if (et == ModelingEdgeType.StartPush)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.DeepSkyBlue;
            }
            else if (et == ModelingEdgeType.ResetEdge)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.Color = Color.Green;
                gEdge.Attr.LineWidth = 2;
            }
            else if (et == ModelingEdgeType.ResetPush)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.LineWidth = 4;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.Green;
            }

            else if (et == ModelingEdgeType.Interlock)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }
            else if (et == ModelingEdgeType.StartReset)
            {
                gEdge.Attr.AddStyle(Style.Solid);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Tee;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }

            UpdateLabelText(gEdge.SourceNode);
            UpdateLabelText(gEdge.TargetNode);

            if (model)
            {

                var src = edge.Sources[0];
                var tgt = edge.Targets[0];

                UpdateNodeView(gEdge.SourceNode, src);
                UpdateNodeView(gEdge.TargetNode, tgt);

            }
        }

        private void UpdateNodeView(Node nNode, ViewNode viewNode)
        {
            {
                //nNode.Attr.Color = Color.DarkGoldenrod;

                if (viewNode.ViewType == ViewType.VBUTTON)
                {
                    if (viewNode.IsChildExist)
                    {
                        nNode.Attr.FillColor = Color.DarkGray;
                        nNode.Attr.Shape = Shape.Box;
                    }
                    else
                    {
                        nNode.Attr.Shape = Shape.Ellipse;
                        if (viewNode.BtnType.Value == BtnType.DuAutoBTN) nNode.Attr.FillColor = Color.DodgerBlue;
                        if (viewNode.BtnType.Value == BtnType.DuManualBTN) nNode.Attr.FillColor = Color.DarkSlateBlue;
                        if (viewNode.BtnType.Value == BtnType.DuDriveBTN) nNode.Attr.FillColor = Color.DarkGoldenrod;
                        if (viewNode.BtnType.Value == BtnType.DuStopBTN) nNode.Attr.FillColor = Color.Firebrick;
                        if (viewNode.BtnType.Value == BtnType.DuEmergencyBTN) nNode.Attr.FillColor = Color.MediumVioletRed;
                        if (viewNode.BtnType.Value == BtnType.DuTestBTN) nNode.Attr.FillColor = Color.CadetBlue;
                        if (viewNode.BtnType.Value == BtnType.DuReadyBTN) nNode.Attr.FillColor = Color.Green;
                        if (viewNode.BtnType.Value == BtnType.DuHomeBTN) nNode.Attr.FillColor = Color.DarkGray;
                        if (viewNode.BtnType.Value == BtnType.DuClearBTN) nNode.Attr.FillColor = Color.DarkOliveGreen;
                    }
                }
                if (viewNode.ViewType == ViewType.VLAMP)
                {
                    if (viewNode.IsChildExist)
                    {
                        nNode.Attr.FillColor = Color.DarkGray;
                        nNode.Attr.Shape = Shape.Box;
                    }
                    else
                    {

                        nNode.Attr.Shape = Shape.Box;
                        if (viewNode.LampType.Value == LampType.DuAutoLamp) nNode.Attr.FillColor = Color.DodgerBlue;
                        if (viewNode.LampType.Value == LampType.DuManualLamp) nNode.Attr.FillColor = Color.DarkSlateBlue;
                        if (viewNode.LampType.Value == LampType.DuDriveLamp) nNode.Attr.FillColor = Color.DarkGoldenrod;
                        if (viewNode.LampType.Value == LampType.DuStopLamp) nNode.Attr.FillColor = Color.Firebrick;
                        if (viewNode.LampType.Value == LampType.DuEmergencyLamp) nNode.Attr.FillColor = Color.MediumVioletRed;
                        if (viewNode.LampType.Value == LampType.DuTestDriveLamp) nNode.Attr.FillColor = Color.CadetBlue;
                        if (viewNode.LampType.Value == LampType.DuReadyLamp) nNode.Attr.FillColor = Color.Green;
                        if (viewNode.LampType.Value == LampType.DuIdleLamp) nNode.Attr.FillColor = Color.DarkGray;
                    }
                }

                if (viewNode.ViewType == ViewType.VCONDITION)
                {
                    if (viewNode.IsChildExist)
                    {
                        nNode.Attr.FillColor = Color.DarkGray;
                        nNode.Attr.Shape = Shape.Box;
                    }
                    else
                    {
                        nNode.Attr.Shape = Shape.Box;
                        if (viewNode.ConditionType.Value == ConditionType.DuReadyState) nNode.Attr.FillColor = Color.DodgerBlue;
                        if (viewNode.ConditionType.Value == ConditionType.DuDriveState) nNode.Attr.FillColor = Color.DarkSlateBlue;
                    }
                }

                if (viewNode.ViewType == ViewType.VREAL)
                    nNode.Attr.Shape = Shape.Box;
                if (viewNode.ViewType == ViewType.VDUMMY)
                {
                    nNode.Attr.Shape = Shape.Box;
                    nNode.Attr.FillColor = Color.Black;
                }
                if (viewNode.ViewType == ViewType.VCALL)
                    nNode.Attr.Shape = Shape.Ellipse;
                if (viewNode.ViewType == ViewType.VIF)
                {
                    if (viewNode.IsChildExist)
                    {
                        nNode.Attr.FillColor = Color.DarkGray;
                        nNode.Attr.Shape = Shape.Box;
                    }
                    else
                    {
                        nNode.Attr.Shape = Shape.InvHouse;
                        nNode.Attr.FillColor = Color.BlueViolet;
                    }

                }
                //if (viewNode.NodeType == ViewType.VCOPY_SYS)
                //    nNode.Attr.Shape = Shape.Octagon;
            }
        }

        public void RefreshGraph()
        {
            viewer.Do(() =>
            {
                viewer.Refresh();
            });

        }


        public void UpdateStatus(ViewNode viewNode)
        {
            Node node = viewer.Graph.FindNode(viewNode.UIKey);
            if (node == null)
            {
                if (viewer.Graph.SubgraphMap.ContainsKey(viewNode.UIKey))
                    node = viewer.Graph.SubgraphMap[viewNode.UIKey];
                else
                    return;
            }
            //node.Attr.Color = Color.White;
            //node.Label.FontColor = Color.White;
            if (viewNode != null)
            {
                if (viewNode.ViewType == ViewType.VREAL)
                    UpdateLineColor(viewNode.Status4, node);
                else
                    UpdateFillColor(viewNode.Status4, node);
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

        public void SetBackColor(System.Drawing.Color color)
        {
            var gColor = Color.Red;
            gColor.R = color.R;
            gColor.G = color.G;
            gColor.B = color.B;
            viewer.Graph.Attr.BackgroundColor = gColor;
        }
    }
}
