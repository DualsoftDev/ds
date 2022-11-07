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
using Color = Microsoft.Msagl.Drawing.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Vertex = Engine.Core.CoreModule.Vertex;
using static Model.Import.Office.InterfaceClass;
using static Engine.Core.DsText;
using static Model.Import.Office.PPTDummyModule;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Diagnostics;
using DocumentFormat.OpenXml.Office2016.Presentation.Command;

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


        bool IsDummyMember(List<pptDummy> lstDummy, Vertex vertex) {
            return lstDummy.Where(w => w.Members.Contains(vertex)).Count() > 0;
        }

        public void SetGraph(Flow flow, DsSystem sys, List<pptDummy> lstDummy)
        {
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

            DrawButtons(flow, sys);
            if(sys.Flows.First() == flow && sys.ApiItems.Count>0) //처음 시스템 Flow에만 인터페이스 표기
                DrawApiItems(flow, sys);

            var edgeVetexs = flow.ModelingEdges.SelectMany(s => new List<Vertex>() { s.Source, s.Target });
            var dummyNodes = lstDummy
                .ToDictionary(d => d.DummyNodeKey, d => System.Tuple.Create(d, new DsViewNode(d.DummyNodeKey)));

            flow.Graph.Vertices
                .Where(seg => !edgeVetexs.Contains(seg))
                .ForEach(seg => DrawSeg(viewer.Graph.RootSubgraph, new DsViewNode(seg)));

            flow.ModelingEdges
                .Where(s => !IsDummyMember(lstDummy, s.Source) && !IsDummyMember(lstDummy, s.Target))
                .Where(s => s.Source.Parent.IsFlow && s.Target.Parent.IsFlow)
                .Select(s => new DsViewEdge(s))
                .ForEach(f => DrawMEdge(viewer.Graph.RootSubgraph, f));
             
            lstDummy
                .Where(w => w.GetParent().GetCore() is Flow)
                .ToDictionary(s => s, s => s.Edges)
                .SelectMany(dic => dic.Value.Select(edge => new DsViewEdge(dic.Key, edge, dummyNodes)))
                .ForEach(f => DrawMEdge(viewer.Graph.RootSubgraph, f));

            viewer.SetCalculatedLayout(viewer.CalculateLayout(viewer.Graph));
        }

        private void DrawButtons(Flow flow, DsSystem sys)
        {
            var btnGroups = new DsViewNode("Buttons", true, BtnType.AutoBTN);
            sys.AutoButtons     .Where(w => w.Value.Contains(flow)).ForEach(f => btnGroups.Singles.Add(new DsViewNode(f.Key, false, BtnType.AutoBTN)));
            sys.EmergencyButtons.Where(w => w.Value.Contains(flow)).ForEach(f => btnGroups.Singles.Add(new DsViewNode(f.Key, false, BtnType.EmergencyBTN)));
            sys.ResetButtons    .Where(w => w.Value.Contains(flow)).ForEach(f => btnGroups.Singles.Add(new DsViewNode(f.Key, false, BtnType.ResetBTN)));
            sys.StartButtons    .Where(w => w.Value.Contains(flow)).ForEach(f => btnGroups.Singles.Add(new DsViewNode(f.Key, false, BtnType.StartBTN)));
            if(btnGroups.Singles.Count > 0)
                DrawSeg(viewer.Graph.RootSubgraph, btnGroups);
        }

        private void DrawApiItems(Flow flow, DsSystem sys)
        {
            var apiGroups = new DsViewNode("Interfaces", true);
            sys.ApiItems.ForEach(f => apiGroups.Singles.Add(new DsViewNode(f.Name, false)));
            DrawSeg(viewer.Graph.RootSubgraph, apiGroups);
        }

        private void UpdateLabelText(Node nNode)
        {
            nNode.LabelText = nNode.LabelText.Split(';')[0];
            nNode.Label.FontColor = Color.White;
            nNode.Attr.Color = Color.White;

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

        private Subgraph DrawSeg(Subgraph parentGraph, DsViewNode seg)
        {
            bool bDrawSub = (seg.IsChildExist || seg.Singles.Any());

            var subGraph = new Subgraph(seg.UIKey);

            if (bDrawSub) parentGraph.AddSubgraph(subGraph);
            var gEdge = viewer.Graph.AddEdge(subGraph.Id, "", subGraph.Id);
            UpdateLabelText(gEdge.SourceNode);
            UpdateNodeView(gEdge.SourceNode, seg);
            gEdge.IsVisible = false;

            DrawSub(parentGraph, seg, subGraph, gEdge.SourceNode, bDrawSub);

            return subGraph;
        }

        private void DrawSub(Subgraph parentGraph, DsViewNode seg, Subgraph subG, Node gNode, bool bDrawSub)
        {
            if (_dicDrawing.ContainsKey(gNode.Id)) return;
            else _dicDrawing.Add(gNode.Id, gNode);

            if (bDrawSub)
            {
                seg.MEdges.ForEach(f =>
                {
                    DrawMEdge(subG, f);
                });

                seg.Singles.ToList().ForEach(subSeg => DrawSeg(subG, subSeg));


            }
            else
                parentGraph.AddNode(gNode);
        }


        private void DrawEdgeStyle(Edge gEdge, DsViewEdge edge, bool model = false)
        {
            //gEdge.Attr.Color = Color.Black;
            //gEdge.Label.FontColor = Color.White;
            gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Generalization;

            gEdge.Attr.Color = Color.White;

            var et = edge.Causal;
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

            else if (edge.Causal == ModelingEdgeType.Interlock)
            {
                gEdge.Attr.AddStyle(Style.Dashed);
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
                gEdge.Attr.Color = Color.PaleGoldenrod;
            }
            else if (edge.Causal == ModelingEdgeType.StartReset)
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

        private void UpdateNodeView(Node nNode, DsViewNode dsViewNode)
        {
            {
                //nNode.Attr.Color = Color.DarkGoldenrod;

                if (dsViewNode.NodeType == NodeType.BUTTON)
                {
                    if (dsViewNode.IsGroup)
                    {
                        nNode.Attr.FillColor = Color.DarkGray;
                        nNode.Attr.Shape = Shape.Box;
                    }
                    else
                    {
                        nNode.Attr.Shape = Shape.Ellipse;
                        if(dsViewNode.BtnType == BtnType.AutoBTN) nNode.Attr.FillColor = Color.DarkGoldenrod;
                        if(dsViewNode.BtnType == BtnType.ResetBTN) nNode.Attr.FillColor = Color.DarkOliveGreen;
                        if(dsViewNode.BtnType == BtnType.EmergencyBTN) nNode.Attr.FillColor = Color.MediumVioletRed;
                        if(dsViewNode.BtnType == BtnType.StartBTN) nNode.Attr.FillColor = Color.BlueViolet;
                    }

                }
                if (dsViewNode.NodeType == NodeType.MY)
                    nNode.Attr.Shape = Shape.Box;
                if (dsViewNode.NodeType == NodeType.DUMMY)
                {
                    nNode.Attr.Shape = Shape.Box;
                    nNode.Attr.FillColor = Color.Black;
                }
                if (dsViewNode.NodeType == NodeType.TR
                    || dsViewNode.NodeType == NodeType.TX
                    || dsViewNode.NodeType == NodeType.RX)
                    nNode.Attr.Shape = Shape.Ellipse;
                if (dsViewNode.NodeType == NodeType.IF)
                {
                    if (dsViewNode.IsGroup)
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
                if (dsViewNode.NodeType == NodeType.COPY)
                    nNode.Attr.Shape = Shape.Octagon;
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
