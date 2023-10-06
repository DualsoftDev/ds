using Dual.Common.Core;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        public static Dictionary<Vertex, ViewVertex> DicNode;
        public static Dictionary<IStorage, ViewVertex> DicActionTag;
        private static ViewVertex CreateViewVertex(Vertex v, IEnumerable<Tuple<ViewNode, UcView>> viewNodes, List<DsTask> tasks)
        {
            return new ViewVertex()
            {
                Vertex = v,
                ViewNodes = viewNodes.ToList(),
                Status = Status4.Homing,
                DsTasks = tasks,
            };
        }

        public static void DrawInit(DsSystem system, Dictionary<ViewNode, UcView> dicView)
        {
            DicNode = new Dictionary<Vertex, ViewVertex>();
            DicActionTag = new Dictionary<IStorage, ViewVertex>();
            var dicUcView = dicView
                .SelectMany(entry => entry.Key.UsedViewNodes.Select(node => new { Node = node, UcView = entry.Value }))
                .ToDictionary(item => item.Node, item => item.UcView);

            var nodes = dicView.Keys
                .SelectMany(view => view.UsedViewNodes.Where(node => node.CoreVertex != null));

            var dicViewNodes = nodes.ToDictionary(
                node => node.CoreVertex.Value,
                node => nodes.Where(w => w.PureVertex.Value == node.CoreVertex.Value)
                             .Select(n => Tuple.Create(n, dicUcView[n]))
            );

            foreach (Vertex v in GetVerties(system))
            {
                var tasks = (v is Call c) ? c.CallTargetJob.DeviceDefs.Cast<DsTask>().ToList() : new List<DsTask>();
                DicNode[v] = CreateViewVertex(v, dicViewNodes[v], tasks);
            }
            foreach (var v in DicNode)
            {
                v.Value.DsTasks.Cast<TaskDev>().Iter(t =>
                {
                    if(t.InTag != null)
                        DicActionTag[t.InTag] = v.Value;
                    if(t.OutTag != null)
                        DicActionTag[t.OutTag] = v.Value;
                });
            }

            ViewChangeSubject();
        }

        private static IEnumerable<Vertex> GetVerties(DsSystem system)
        {
            var systems = system.GetRecursiveLoadedSystems().ToList();
            systems.Add(system);

            return systems.SelectMany(s => s.GetVertices().OfType<Vertex>());
        }

        public static Subject<TagDS> VertexChangeSubject = new();
        private static IDisposable _Disposable;
        private static void ViewChangeSubject()
        {
            _Disposable?.Dispose();
            _Disposable = VertexChangeSubject.Subscribe(rx =>
            {
                if (rx.IsEventVertex)
                {
                    EventVertex ev = rx as EventVertex;
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
                        vv.ViewNodes.Iter(dic =>
                        {
                            dic.Item1.Status4 = status;
                            dic.Item2.UpdateStatus(dic.Item1);
                        });
                    }
                    if (TagKindExt.IsErrTag(rx))
                    {
                        var vv = DicNode[ev.Target];
                        if (ev.TagKind == VertexTag.errorTx)
                            vv.ErrorTX = (bool)ev.Tag.BoxedValue;
                        else if (ev.TagKind == VertexTag.errorRx)
                            vv.ErrorRX = (bool)ev.Tag.BoxedValue;
                        else
                            throw new Exception($"not ErrTag {TagKindExt.GetTagToText(rx)}");

                        vv.ViewNodes.Iter(dic =>
                        {
                            dic.Item2.UpdateError(dic.Item1, vv.ErrorTX, vv.ErrorRX);
                        });
                    }
                }

                if (rx.IsEventAction)
                {
                    EventAction ea = rx as EventAction;
                    var viewNode = DicActionTag[ea.Tag];
                    viewNode.ViewNodes.Iter(dic =>
                    {
                        if(ea.Tag.TagKind == (int)ActionTag.ActionIn)
                            dic.Item2.UpdateInValue(dic.Item1, ea.Tag.BoxedValue);
                        if (ea.Tag.TagKind == (int)ActionTag.ActionOut)
                            dic.Item2.UpdateOutValue(dic.Item1, ea.Tag.BoxedValue);
                    });
                }
            });
        }
    }
}


