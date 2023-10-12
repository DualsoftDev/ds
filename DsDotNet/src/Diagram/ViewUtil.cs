using Dual.Common.Core;
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

namespace Diagram.View.MSAGL
{

    public static class ViewUtil
    {
        public static Dictionary<Vertex, ViewVertex> DicNode = new();
        public static Dictionary<IStorage, ViewVertex> DicActionTag = new();
        private static ViewVertex CreateViewVertex(ViewNode fv, Vertex v, IEnumerable<ViewNode> viewNodes, List<DsTask> tasks)
        {
            return new ViewVertex()
            {
                Vertex = v,
                ViewNodes = viewNodes.ToList(),
                FlowNode = fv,
                Status = Status4.Homing,
                DsTasks = tasks,
            };
        }

        public static void ViewInit(DsSystem system, IEnumerable<ViewNode> flowViews)
        {
            DicNode.Clear();
            DicActionTag.Clear();

            var nodes = flowViews
                .SelectMany(view => view.UsedViewNodes.Where(node => node.CoreVertex != null));

            var dicViewNodes = nodes.ToDictionary(
                node => node.CoreVertex.Value,
                node => nodes.Where(w => w.PureVertex.Value == node.CoreVertex.Value)
            );

            flowViews.Iter(fv =>
            {
                foreach (Vertex v in fv.Flow.Value.GetVerticesOfFlow())
                {
                    var tasks = (v is Call c) ? c.CallTargetJob.DeviceDefs.Cast<DsTask>().ToList() : new List<DsTask>();
                    var viewVertex = CreateViewVertex(fv, v, dicViewNodes[v], tasks);
                    DicNode[v] = viewVertex;

                    viewVertex.DsTasks.Cast<TaskDev>().Iter(t =>
                     {
                         if (t.InTag != null)
                             DicActionTag[t.InTag] = viewVertex;
                         if (t.OutTag != null)
                             DicActionTag[t.OutTag] = viewVertex;
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

        public static List<UcView> UcViews { get; set; } = new List<UcView>();  
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

                    if (TagKindExt.IsStatusTag(rx))
                    {
                        Status4 status = Status4.Homing;
                        switch (ev.TagKind)
                        {
                            case VertexTag.ready: status = Status4.Ready; break;
                            case VertexTag.going: status = Status4.Going; break;
                            case VertexTag.finish: status = Status4.Finish; break;
                            case VertexTag.homing: status = Status4.Homing; break;
                            default: break;
                        }
                        var vv = DicNode[ev.Target];
                        var ucView = UcViews.Where(w => w.MasterNode == vv.FlowNode).FirstOrDefault();
                        if (ucView != null)
                        {
                            vv.ViewNodes.Iter(node =>
                            {
                                node.Status4 = status;
                                ucView.UpdateStatus(node);
                            });
                        }
                    }
                    if (TagKindExt.IsErrTag(rx) && ev.Target is CallDev call)
                    {
                        var vv = DicNode[ev.Target];
                        vv.IsError = (bool)ev.Tag.BoxedValue;

                        var errs =
                            call.CallTargetJob.DeviceDefs.Select(s => s.ApiItem.TagManager)
                                    .Cast<ApiItemManager>()
                                    .Select(s => s.ErrorText);

                        vv.ErrorText = String.Join("\r", errs);
                       
                        var ucView = UcViews.Where(w => w.MasterNode == vv.FlowNode).FirstOrDefault();
                        if (ucView != null)
                        {
                            vv.ViewNodes.Iter(node =>
                            {
                                ucView.UpdateError(node, vv.IsError, vv.ErrorText);
                            });
                        }
                    }
                }

                if (rx.IsEventAction)
                {
                    EventAction ea = rx as EventAction;
                    if (!DicActionTag.ContainsKey(ea.Tag)) return;

                    var viewNode = DicActionTag[ea.Tag];
                    var ucView = UcViews.Where(w => w.MasterNode == viewNode.FlowNode).FirstOrDefault();
                    if (ucView != null)
                    {
                        viewNode.ViewNodes.Iter(node =>
                        {
                            if (ea.Tag.TagKind == (int)ActionTag.ActionIn)
                                ucView.UpdateInValue(node, ea.Tag.BoxedValue);
                            if (ea.Tag.TagKind == (int)ActionTag.ActionOut)
                                ucView.UpdateOutValue(node, ea.Tag.BoxedValue);
                        });
                    }
                }
            });
        }
    }
}


