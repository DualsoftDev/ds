using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsConstants;
using static Engine.Core.DsText;
using static Engine.Core.DsType;
using static Engine.Import.Office.InterfaceClass;
using static Engine.Import.Office.PPTDummyModule;
using static Engine.Import.Office.ViewModule;
using Color = Microsoft.Msagl.Drawing.Color;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Style = Microsoft.Msagl.Drawing.Style;
using Vertex = Engine.Core.CoreModule.Vertex;


namespace Diagram.View.MSAGL;

public partial class UcView : UserControl
{
    private readonly GViewer viewer = new();

    public Flow Flow { get; set; }
    public ViewNode MasterNode { get; set; }

    public UcView()
    {
        InitializeComponent();

        viewer.Dock = DockStyle.Fill;
        viewer.PanButtonPressed = true;
        viewer.ToolBarIsVisible = false;

        Controls.Add(viewer);
    }

    int node_attr_linewidthH = 4;
    int node_attr_linewidthL = 1;
    int edge_attr_linewidthWeek = 1;
    int edge_attr_HeadSize = 7;
    int nnode_label_fontsize = 6;
    int nnode_label_fontsize_call = 4;

    private readonly Dictionary<string, Node> _dicDrawing = new();

    private bool IsDummyMember(List<pptDummy> lstDummy, Vertex vertex)
    {
        return lstDummy.Any(w => w.Members.Contains(vertex));
    }

    public void SetGraph(ViewNode viewNode, Flow flow)
    {
        Flow = flow;
        MasterNode = viewNode;
        //sub 그래프 불가
        //viewer.Graph.LayoutAlgorithmSettings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
        //viewer.Graph.LayoutAlgorithmSettings = new RankingLayoutSettings();
        //sub 그래프 가능
        viewer.Graph = new Graph();
        Microsoft.Msagl.Layout.Layered.SugiyamaLayoutSettings layoutSetting = new();
        //layoutSetting = new Microsoft.Msagl.Layout.Incremental.FastIncrementalLayoutSettings(); 

        if (viewNode.UsedViewNodes.Count() > 30)
        {
            layoutSetting.PackingMethod = Microsoft.Msagl.Core.Layout.PackingMethod.Compact;
            layoutSetting.NodeSeparation = 2;
            layoutSetting.ClusterMargin = 2;
            layoutSetting.LiftCrossEdges = false;
            layoutSetting.PackingAspectRatio = 2;
        }
        else
        {
            layoutSetting.PackingMethod = Microsoft.Msagl.Core.Layout.PackingMethod.Columns;
            layoutSetting.LayerSeparation = 20;
            layoutSetting.NodeSeparation = 40;
            layoutSetting.ClusterMargin = 10;
        }

        viewer.Graph.LayoutAlgorithmSettings = layoutSetting;

        SetBackColor(System.Drawing.Color.FromArgb(33, 33, 33));

        viewNode.GetSingles().Where(w => !(w.ViewType == ViewType.VBUTTON || w.ViewType == ViewType.VLAMP))
                             .ForEach(f => DrawSeg(viewer.Graph.RootSubgraph, f));
        viewNode.GetEdges().ForEach(f => DrawMEdge(viewer.Graph.RootSubgraph, f));

        viewer.SetCalculatedLayout(viewer.CalculateLayout(viewer.Graph));
    }

    public void ForceUpdateLabelText()
    {
        MasterNode.UsedViewNodes.Where(w => w.IsVertex).Iter(viewNode =>
        {
            Node node = findNode(viewNode);
            UpdateLabelText(node, viewNode);
        });
    }


    private void UpdateLabelText(Node nNode, ViewNode viewNode)
    {
        int goingCnt = viewNode.GoingCnt;
        var org = viewNode.DisplayName; 
        if (goingCnt > 0)
            nNode.LabelText = $"{org}\v\r\n({goingCnt})";
        else
            nNode.LabelText = org;

        nNode.Label.FontColor = Color.White;

        if (viewNode.ViewType == ViewType.VREAL)
        {
            nNode.Label.FontSize = nnode_label_fontsize;
        }
        else
        {
            nNode.Label.FontSize = nnode_label_fontsize_call;
        }
    }

    private void DrawMEdge(Subgraph subgraph, ModelingEdgeInfo<ViewNode> edge)
    {
        var src = edge.Sources[0];
        var tgt = edge.Targets[0];

        bool bDrawSubSrc = src.IsChildExist;
        bool bDrawSubTgt = tgt.IsChildExist;

        ViewNode mEdgeSrc = src;
        ViewNode mEdgeTgt = tgt;
        Subgraph subGSrc = new(mEdgeSrc.UIKey);
        Subgraph subGTgt = new(mEdgeTgt.UIKey);

        if (bDrawSubSrc)
        {
            subgraph.AddSubgraph(subGSrc);
        }

        if (bDrawSubTgt)
        {
            subgraph.AddSubgraph(subGTgt);
        }

        Edge gEdge = viewer.Graph.AddEdge(subGSrc.Id, "", subGTgt.Id);
        DrawEdgeStyle(gEdge, edge, true);
        DrawSub(subgraph, mEdgeSrc, subGSrc, gEdge.SourceNode, bDrawSubSrc);
        DrawSub(subgraph, mEdgeTgt, subGTgt, gEdge.TargetNode, bDrawSubTgt);
    }

    private Subgraph DrawSeg(Subgraph parentGraph, ViewNode viewNode)
    {
        Subgraph subGraph = new(viewNode.UIKey);

        if (viewNode.IsChildExist)
        {
            parentGraph.AddSubgraph(subGraph);
        }

        Edge gEdge = viewer.Graph.AddEdge(subGraph.Id, "", subGraph.Id);
        UpdateLabelText(gEdge.SourceNode, viewNode);
        UpdateNodeView(gEdge.SourceNode, viewNode);
        gEdge.IsVisible = false;

        DrawSub(parentGraph, viewNode, subGraph, gEdge.SourceNode, viewNode.IsChildExist);

        return subGraph;
    }

    private void DrawSub(Subgraph parentGraph, ViewNode viewNode, Subgraph subG, Node gNode, bool bDrawSub)
    {
        if (_dicDrawing.ContainsKey(gNode.Id))
        {
            return;
        }

        _dicDrawing.Add(gNode.Id, gNode);

        if (bDrawSub)
        {
            viewNode.GetEdges().ForEach(f => { DrawMEdge(subG, f); });

            viewNode.GetSingles().ForEach(subSeg => DrawSeg(subG, subSeg));
        }
        else
        {
            parentGraph.AddNode(gNode);
        }
    }


    private void DrawEdgeStyle(Edge gEdge, ModelingEdgeInfo<ViewNode> edge, bool model = false)
    {
        //gEdge.Attr.Color = Color.Black;
        //gEdge.Label.FontColor = Color.White;
        gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Generalization;

        gEdge.Attr.Color = Color.DarkGray;

        void updateRowSource(bool bRev) 
        {
            if (bRev)
            {
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.None;
            }
            else
            {
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.None;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
            }
        }
   


        ModelingEdgeType et = edge.EdgeSymbol.ToModelEdge();
        if (et == ModelingEdgeType.StartEdge || et == ModelingEdgeType.RevStartEdge)
        {
            gEdge.Attr.AddStyle(Style.Solid);
            gEdge.Attr.LineWidth = edge_attr_linewidthWeek;
            gEdge.Attr.ArrowheadLength = edge_attr_HeadSize;
            updateRowSource(et == ModelingEdgeType.RevStartEdge);
        }
       
        else if (et == ModelingEdgeType.ResetEdge || et == ModelingEdgeType.RevResetEdge)
        {
            gEdge.Attr.AddStyle(Style.Dashed);
            gEdge.Attr.LineWidth = edge_attr_linewidthWeek;
            updateRowSource(et == ModelingEdgeType.RevResetEdge);
        }

        else if (et == ModelingEdgeType.Interlock)
        {
            gEdge.Attr.AddStyle(Style.Dashed);
            gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
            gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
        }
        
        else if (et == ModelingEdgeType.StartReset|| et == ModelingEdgeType.RevStartReset)
        {
            gEdge.Attr.AddStyle(Style.Solid);
            if (et == ModelingEdgeType.RevStartReset)
            {
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Tee;
            }
            else
            {
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Tee;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
            }
        }
        else if (et == ModelingEdgeType.SelfReset || et == ModelingEdgeType.RevSelfReset)
        {
            gEdge.Attr.AddStyle(Style.Dashed);
            if (et == ModelingEdgeType.RevSelfReset)
            {
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Normal;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Tee;
            }
            else
            {
                gEdge.Attr.ArrowheadAtSource = ArrowStyle.Tee;
                gEdge.Attr.ArrowheadAtTarget = ArrowStyle.Normal;
            }
        }
        else    
        {
           throw new Exception($"Error {et.ToText()} not DrawEdgeStyle");
        }

        UpdateLabelText(gEdge.SourceNode, edge.Sources.First());
        UpdateLabelText(gEdge.TargetNode, edge.Targets.First());

        if (model)
        {
            ViewNode src = edge.Sources[0];
            ViewNode tgt = edge.Targets[0];

            UpdateNodeView(gEdge.SourceNode, src);
            UpdateNodeView(gEdge.TargetNode, tgt);
        }
    }

    private void UpdateNodeView(Node nNode, ViewNode viewNode)
    {
        {
            //nNode.Attr.Color = Color.DarkGoldenrod;
            nNode.Attr.LineWidth = node_attr_linewidthL;


            if (viewNode.ViewType == ViewType.VBUTTON)
            {
                if (viewNode.IsChildExist)
                {
                    nNode.Attr.FillColor = Color.DarkGray;
                    nNode.Attr.Shape = Shape.Box;
                }
                else
                {
                    nNode.Attr.Shape = Shape.Box;
                    if (viewNode.BtnType.Value == BtnType.DuAutoBTN)
                    {
                        nNode.Attr.FillColor = Color.DodgerBlue;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuManualBTN)
                    {
                        nNode.Attr.FillColor = Color.DarkSlateBlue;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuDriveBTN)
                    {
                        nNode.Attr.FillColor = Color.DarkOliveGreen;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuPauseBTN)
                    {
                        nNode.Attr.FillColor = Color.Firebrick;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuEmergencyBTN)
                    {
                        nNode.Attr.FillColor = Color.MediumVioletRed;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuTestBTN)
                    {
                        nNode.Attr.FillColor = Color.CadetBlue;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuReadyBTN)
                    {
                        nNode.Attr.FillColor = Color.Green;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuHomeBTN)
                    {
                        nNode.Attr.FillColor = Color.DarkGray;
                    }

                    if (viewNode.BtnType.Value == BtnType.DuClearBTN)
                    {
                        nNode.Attr.FillColor = Color.DarkGoldenrod;
                    }
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
                    if (viewNode.LampType.Value == LampType.DuAutoModeLamp)
                    {
                        nNode.Attr.FillColor = Color.DodgerBlue;
                    }

                    if (viewNode.LampType.Value == LampType.DuManualModeLamp)
                    {
                        nNode.Attr.FillColor = Color.DarkSlateBlue;
                    }

                    if (viewNode.LampType.Value == LampType.DuDriveStateLamp)
                    {
                        nNode.Attr.FillColor = Color.DarkGoldenrod;
                    }

                    if (viewNode.LampType.Value == LampType.DuErrorStateLamp)
                    {
                        nNode.Attr.FillColor = Color.Firebrick;
                    }



                    if (viewNode.LampType.Value == LampType.DuTestDriveStateLamp)
                    {
                        nNode.Attr.FillColor = Color.CadetBlue;
                    }

                    if (viewNode.LampType.Value == LampType.DuReadyStateLamp)
                    {
                        nNode.Attr.FillColor = Color.Green;
                    }

                    if (viewNode.LampType.Value == LampType.DuIdleModeLamp)
                    {
                        nNode.Attr.FillColor = Color.DarkGray;
                    }
                    if (viewNode.LampType.Value == LampType.DuOriginStateLamp)
                    {
                        nNode.Attr.FillColor = Color.DarkGreen;
                    }
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
                    if (viewNode.ConditionType.Value == ConditionType.DuReadyState)
                    {
                        nNode.Attr.FillColor = Color.DodgerBlue;
                    }

                    if (viewNode.ConditionType.Value == ConditionType.DuDriveState)
                    {
                        nNode.Attr.FillColor = Color.DarkSlateBlue;
                    }
                }
            }

            if (viewNode.ViewType == ViewType.VREAL)
            {
                nNode.Attr.Shape = Shape.Box;
            }

            if (viewNode.ViewType == ViewType.VDUMMY)
            {
                nNode.Attr.Shape = Shape.Box;
                nNode.Attr.FillColor = GetDrawColor(System.Drawing.Color.FromArgb(25, 25, 25));
            }

            if (viewNode.ViewType == ViewType.VCALL)
            {
                nNode.Attr.Shape = Shape.Box;
            }

            if (viewNode.ViewType == ViewType.VIF)
            {
                if (viewNode.IsChildExist)
                {
                    nNode.Attr.FillColor = Color.DimGray;
                    nNode.Attr.Shape = Shape.Box;
                }
                else
                {
                    nNode.Attr.Shape = Shape.InvHouse;
                    nNode.Attr.FillColor = Color.BlueViolet;
                    nNode.Attr.LabelMargin = 6;
                }
            }
            //if (viewNode.NodeType == ViewType.VCOPY_SYS)
            //    nNode.Attr.Shape = Shape.Octagon;
        }
    }

    public void RefreshGraph()
    {
        viewer.Do(viewer.Refresh);
    }

    private Node findNode(ViewNode viewNode)
    {
        Node node = viewer.Graph.FindNode(viewNode.UIKey);
        return node ?? (viewer.Graph.SubgraphMap.TryGetValue(viewNode.UIKey, out var value)
            ? value
            : (Node)null);
    }

    private IEnumerable<Edge> findEdgeTargetSame(Node node)
    {
        var edges = viewer.Graph.Edges.Where(w => w.TargetNode == node);
        return edges;
    }

    public void UpdateInValue(ViewNode viewNode, object item2, bool vRefresh = true)
    {
        Node node = findNode(viewNode);
        if (node == null) return;
        bool dataExist = Convert.ToDouble(item2) != 0;
        UpdateFillColor(dataExist, node, Color.DarkBlue);
        if (vRefresh) RefreshGraph();
    }
    public void UpdatePlanEndValue(ViewNode viewNode, bool item2, bool vRefresh = true)
    {
        Node node = findNode(viewNode);
        if (node == null) return;
        UpdateFillColor(item2, node, Color.CornflowerBlue);

        if (vRefresh) RefreshGraph();
    }

    public void UpdateOriginValue(ViewNode viewNode, object item2, bool vRefresh = true)
    {
        Node node = findNode(viewNode);
        if (node == null) return;
        bool on = Convert.ToBoolean(item2);
        UpdateFillColor(on, node, Color.Gold);
        if (vRefresh) RefreshGraph();
    }



    public void UpdateOutValue(ViewNode viewNode, object item2, bool vRefresh = true)
    {
        Node node = findNode(viewNode);
        if (node == null) return;
        bool dataExist = Convert.ToDouble(item2) != 0;
        UpdateLineWidth(dataExist, node);
        if (vRefresh) RefreshGraph();
    }

    public void UpdateViewNode(ViewNode viewNode, ViewVertex vv)
    {
        bool vRefresh = false;
        Node node = findNode(viewNode);

        UpdateStatus(viewNode, vRefresh);
        UpdateOriginValue(viewNode, vv.LampOrigin, vRefresh);

        UpdateInValue(viewNode, vv.LampInput, vRefresh);
        UpdateOutValue(viewNode, vv.LampOutput, vRefresh);
        UpdateError(viewNode, vv.IsError, vv.ErrorText, vRefresh);

        UpdatePlanEndValue(viewNode, vv.LampPlanEnd, vRefresh);

        RefreshGraph();
    }


    public void UpdateStatus(ViewNode viewNode, bool vRefresh = true)
    {
        Node node = findNode(viewNode);
        if (node != null)
        {
            UpdateLabelText(node, viewNode);
            UpdateBackColor(viewNode.Status4, node);
            if (vRefresh) RefreshGraph();
        }
    }

    public void UpdateError(ViewNode viewNode, bool isError, string errorText, bool vRefresh = true)
    {
        Node node = findNode(viewNode);
        if (node != null)
        {
            UpdateFontColor(isError, errorText, node, viewNode.ViewType);
            if (vRefresh) RefreshGraph();
        }
    }


    private void UpdateFontColor(bool err, string errText, Node node, ViewType viewType)
    {
        var org = node.Label.Text.Split('\n')[0];
        if (err)
        {
            node.Label.FontColor = Color.Red;
            if (viewType != ViewType.VREAL)
                node.Label.Text = $"{org}\n{errText}";
        }
        else
        {
            node.Label.FontColor = Color.White;
            if (viewType != ViewType.VREAL)
                node.Label.Text = org;
        }
    }

    private void UpdateBackColor(Status4 newStatus, Node node)
    {
        if (newStatus == Status4.Ready)
        {
            node.Attr.FillColor = Color.DarkGreen;
        }
        else if (newStatus == Status4.Going)
        {
            node.Attr.FillColor = Color.DarkGoldenrod;
        }
        else if (newStatus == Status4.Finish)
        {
            node.Attr.FillColor = Color.RoyalBlue;
        }
        else if (newStatus == Status4.Homing)
        {
            node.Attr.FillColor = Color.DimGray;
        }

    }


    private void UpdateFillColor(bool dataExist, Node node, Color color)
    {
        node.Attr.Color = dataExist 
            ? color 
            : Color.Black;
    }

    private void UpdateLineWidth(bool dataExist, Node node)
    {
        node.Attr.LineWidth = dataExist ? node_attr_linewidthH : node_attr_linewidthL;
    }

    private void UpdateEdgeColor(bool dataExist, Edge edge)
    {
        edge.Attr.Color = dataExist ? Color.PeachPuff : Color.DarkGray;
    }


    public void SetBackColor(System.Drawing.Color color)
    {
        viewer.Graph.Attr.BackgroundColor = GetDrawColor(color);
    }

    public Color GetDrawColor(System.Drawing.Color color)
    {
        Color gColor = Color.Red;
        gColor.R = color.R;
        gColor.G = color.G;
        gColor.B = color.B;

        return gColor;
    }
}