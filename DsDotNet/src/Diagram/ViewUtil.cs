using Dual.Common.Core;
using Engine.CodeGenCPU;
using Engine.Core;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices.ComTypes;
using static Engine.CodeGenCPU.ApiTagManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeExt;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;
using static Engine.Import.Office.ViewModule;
using Tuple = System.Tuple;

namespace Diagram.View.MSAGL;

public static class ViewUtil
{
    public static Dictionary<Vertex, ViewVertex> DicNode = new();
    public static Dictionary<IStorage, List<ViewVertex>> DicActionTag = new();

    private static ViewVertex CreateViewVertex(ViewNode fv, Vertex v, IEnumerable<ViewNode> viewNodes,
        List<DsTask> tasks)
    {
        var nodes = new ViewVertex
        {
            Vertex = v,
            FlowNode = fv,
            Status = Status4.Homing,
            DsTasks = tasks
        };
        ;
        nodes.SetViewNodes(viewNodes.ToList());
        return nodes;
    }

    public static void ViewInit(DsSystem system, IEnumerable<ViewNode> flowViews)
    {
        DicNode.Clear();
        DicActionTag.Clear();

        var nodes = flowViews
            .SelectMany(view => view.UsedViewNodes.Where(node => node.CoreVertex != null));

        var dicViewNodes = nodes
            .GroupBy(d => d.CoreVertex.Value) // 중복된 항목을 그룹화
            .Select(g => g.First()) // 각 그룹에서 첫 번째 항목 선택
            .ToDictionary(
                node => node.CoreVertex.Value,
                node => nodes.Where(w => w.PureVertex.Value == node.CoreVertex.Value
                                         || w.CoreVertex.Value == node.CoreVertex.Value)
            );

        flowViews.Iter(fv =>
        {
            foreach (Vertex v in fv.Flow.Value.GetVerticesOfFlow())
            {
                var tasks = (v.GetPure() is Call c)
                    ? c.CallTargetJob.DeviceDefs.Cast<DsTask>().ToList()
                    : new List<DsTask>();
                var viewVertex = CreateViewVertex(fv, v, dicViewNodes[v], tasks);
                DicNode[v] = viewVertex;

                viewVertex.DsTasks.Cast<TaskDev>().Iter(t =>
                {
                    if (t.InTag != null)
                    {
                        if (!DicActionTag.ContainsKey(t.InTag))
                            DicActionTag.Add(t.InTag, new List<ViewVertex>());
                        DicActionTag[t.InTag].Add(viewVertex);
                    }

                    if (t.OutTag != null)
                    {
                        if (!DicActionTag.ContainsKey(t.OutTag))
                            DicActionTag.Add(t.OutTag, new List<ViewVertex>());
                        DicActionTag[t.OutTag].Add(viewVertex);
                    }
                });
            }
        });
    }

    private static IEnumerable<Vertex> GetVerties(DsSystem system)
    {
        var systems = system.GetRecursiveLoadedSystems().ToList();
        systems.Add(system);

        return systems.SelectMany(s => s.GetVertices().OfType<Vertex>());
    }

    public static List<UcView> UcViews { get; set; } = new();
    public static Subject<TagDS> VertexChangeSubject = new();
    private static IDisposable _Disposable;

    public static void ViewChangeSubject()
    {
        _Disposable?.Dispose();
        _Disposable = VertexChangeSubject.Subscribe(rx =>
        {
            if (rx.IsEventVertex)
            {
                EventVertex ev = rx as EventVertex;
                if (!DicNode.ContainsKey(ev.Target)) return;

                if (rx.IsStatusTag())
                {
                    Status4 status = ev.TagKind switch
                    {
                        VertexTag.ready => Status4.Ready,
                        VertexTag.going => Status4.Going,
                        VertexTag.finish => Status4.Finish,
                        VertexTag.homing => Status4.Homing,
                        _ => Status4.Homing
                    };

                    var vv = DicNode[ev.Target];
                    var ucView = UcViews.FirstOrDefault(w => w.MasterNode == vv.FlowNode);
                    vv.DisplayNodes.Iter(node =>
                    {
                        node.Status4 = status;
                        if (ucView != null) ucView.UpdateStatus(node);
                    });
                }

                if (rx.IsVertexErrTag() && ev.Target is CallDev call)
                {
                    var vv = DicNode[ev.Target];
                    vv.IsError = (bool)ev.Tag.BoxedValue;
                    vv.ErrorText = string.Join("\n", ConvertCoreExt.errTexts(call));

                    var ucView = UcViews.FirstOrDefault(w => w.MasterNode == vv.FlowNode);
                    if (ucView != null)
                    {
                        vv.DisplayNodes.Iter(node => { ucView.UpdateError(node, vv.IsError, vv.ErrorText); });
                    }
                }
            }

            if (rx.IsEventAction)
            {
                EventAction ea = rx as EventAction;
                if (!DicActionTag.ContainsKey(ea.Tag)) return;

                var viewNodes = DicActionTag[ea.Tag];
                var ucView = UcViews
                    .FirstOrDefault(w => viewNodes.Select(n => n.FlowNode).Contains(w.MasterNode));
                viewNodes.Iter(n =>
                {
                    n.DisplayNodes.Iter(node =>
                    {
                        var tags = n.DsTasks.Cast<TaskDev>();

                        switch (ea.Tag.TagKind)
                        {
                            case (int)ActionTag.ActionIn:
                            {
                                var off = tags
                                    .Select(s => Convert.ToUInt64(s.InTag.BoxedValue)).Any(w => w == 0);
                                n.LampInput = !off;
                                ucView?.UpdateInValue(node, !off);
                                break;
                            }
                            case (int)ActionTag.ActionOut:
                            {
                                var on = tags
                                    .Select(s => Convert.ToUInt64(s.OutTag.BoxedValue)).Any(w => w != 0);
                                n.LampOutput = on;
                                ucView?.UpdateOutValue(node, on);
                                break;
                            }
                        }
                    });
                });
            }
        });
    }
}