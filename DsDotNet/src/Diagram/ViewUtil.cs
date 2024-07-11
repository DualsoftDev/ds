using Diagram.View.MSAGL;
using DocumentFormat.OpenXml.Presentation;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.CodeGenCPU;
using Engine.Core;
using Engine.Info;
using log4net.Util;
using Microsoft.Msagl.GraphViewerGdi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using static Engine.CodeGenCPU.ApiTagManagerModule;
using static Engine.CodeGenCPU.TagManagerModule;
using static Engine.CodeGenCPU.TaskDevManagerModule;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.Interface;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Core.TagKindList;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;
using static Engine.Import.Office.ImportViewModule;
using static Engine.Import.Office.ViewModule;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Diagram.View.MSAGL
{
    public static class ViewUtil
    {
        public static List<UcView> UcViews { get; set; } = new();
        public static Subject<TagDS> VertexChangeSubject = new();
        public static Dictionary<Vertex, ViewVertex> DicNode = new();

        static IDisposable _Disposable;
        static Dictionary<IStorage, List<ViewVertex>> DicTaskDevTag = new();
        static Dictionary<IStorage, List<ViewVertex>> DicMemoryTag = new();
        static private DsSystem _sys = null;
        static public bool SaveLog = false;
        
        public static List<ViewNode> CreateViews(DsSystem sys)
        {
            _sys = sys;
            var flowViewNodes = ImportViewUtil.GetViewNodesLoadingsNThis(sys).ToList();

            ViewChangeSubject();

            DicNode.Clear();
            DicTaskDevTag.Clear();
            DicMemoryTag.Clear();

            var flowViews = flowViewNodes.ToArray();
            var nodes = flowViews
                .SelectMany(view => view.UsedViewNodes.Where(node => node.CoreVertex != null)).ToArray();

            var dicViewNodes = nodes
                .GroupBy(d => d.CoreVertex.Value)
                .Select(g => g.First())
                .ToDictionary(
                    node => node.CoreVertex.Value,
                    node => nodes.Where(w => w.PureVertex.Value == node.CoreVertex.Value || w.CoreVertex.Value == node.CoreVertex.Value)
                );

            flowViews.Iter(fv =>
            {
                foreach (Vertex v in fv.Flow.Value.GetVerticesOfFlow())
                {
                    var tasks = (v.GetPure() is Call c && c.IsJob)
                        ? c.TargetJob.DeviceDefs.Cast<TaskDev>().ToList()
                        : new List<TaskDev>();
                    var viewVertex = CreateViewVertex(fv, v, dicViewNodes[v], tasks);
                    DicNode[v] = viewVertex;

                    viewVertex.TaskDevs.Cast<TaskDev>().Iter(t =>
                    {
                        UpdateDicTaskDevTag(t.InTag, viewVertex);
                        UpdateDicTaskDevTag(t.OutTag, viewVertex);
                        UpdateDicTaskDevPlanTag(t, viewVertex);
                    });
                }
            });


            _sys.GetRealVertices().Iter(real =>
            {
                var og = (real.TagManager as VertexManager).OG;
                UpdateOriginVertexTag(og, DicNode[real]);

                real.GetSharedReal().Iter(alias => UpdateOriginVertexTag(og, DicNode[alias]));
            });
             

            return flowViewNodes;

            ViewVertex CreateViewVertex(ViewNode fv, Vertex v, IEnumerable<ViewNode> viewNodes, List<TaskDev> tasks)
            {
                var nodes = new ViewVertex
                {
                    Vertex = v,
                    FlowNode = fv,
                    Status = Status4.Homing,
                    TaskDevs = tasks
                };

                if (v.GetPure() is Call)
                {
                    if (v.Parent.IsDuParentFlow)
                        nodes.SetViewNodes(viewNodes);
                    else
                        nodes.SetViewNodes(viewNodes.Where(w => w.CoreVertex.Value == v));
                }
                else
                    nodes.SetViewNodes(viewNodes);

                return nodes;
            }

            void UpdateDicTaskDevTag(IStorage tag, ViewVertex viewVertex)
            {
                if (tag != null)
                {
                    if (!DicTaskDevTag.ContainsKey(tag))
                        DicTaskDevTag.Add(tag, new List<ViewVertex>());
                    DicTaskDevTag[tag].Add(viewVertex);
                }
            }
            void UpdateDicTaskDevPlanTag(TaskDev td, ViewVertex viewVertex)
            {
                var planEndTag = (td.TagManager as TaskDevManager).PE;

                if (!DicMemoryTag.ContainsKey(planEndTag))
                    DicMemoryTag.Add(planEndTag, new List<ViewVertex>());
                DicMemoryTag[planEndTag].Add(viewVertex);
            }
            void UpdateOriginVertexTag(IStorage tag, ViewVertex viewVertex)
            {
                if (!DicMemoryTag.ContainsKey(tag))
                    DicMemoryTag.Add(tag, new List<ViewVertex>());
                DicMemoryTag[tag].Add(viewVertex);
            }

            
            void ViewChangeSubject()
            {
                _Disposable?.Dispose();
                _Disposable = VertexChangeSubject.Subscribe(rx =>
                {
                    if (rx.IsEventVertex)
                    {
                        HandleVertexEvent(rx as EventVertex);
                    }
                    else if (rx.IsEventTaskDev)
                    {
                        HandleActionEvent(rx as EventTaskDev);
                    }
                    else if (rx.IsEventTaskDev)
                    {
                        HandleTaskDevEvent(rx as EventTaskDev);
                    }
                 
                    if (SaveLog)
                        DBLog.InsertValueLog(DateTime.Now, rx);
                });
            }
        }
        private static void HandleVertexEvent(EventVertex ev)
        {
            if (ev.IsStatusTag() && (bool)ev.Tag.BoxedValue && DicNode.ContainsKey(ev.Target)) 
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
                vv.Nodes.Iter(node =>
                {
                    node.Status4 = status;
                    if (status == Status4.Going) node.UpdateGoingCnt();

                    var ucView = UcViews.FirstOrDefault(w => w.MasterNode == DicNode[node.CoreVertex.Value].FlowNode);
                    if (ucView != null) ucView.UpdateStatus(node);
                });
            }

            if (ev.IsVertexErrTag() && ev.Target is Call call)
            {
                var vv = DicNode[ev.Target];
                vv.IsError = (bool)ev.Tag.BoxedValue;
                vv.ErrorText = ConvertCoreExtUtils.errText(call);

                var ucView = UcViews.FirstOrDefault(w => w.MasterNode == vv.FlowNode);
                if (ucView != null)
                {
                    vv.DisplayNodes.Iter(node => { ucView.UpdateError(node, vv.IsError, vv.ErrorText); });
                }
            }
            if (ev.IsVertexOriginTag())
            {
                if (!DicMemoryTag.ContainsKey(ev.Tag)) return;
                var viewNodes = DicMemoryTag[ev.Tag];
                //var ucView = UcViews.FirstOrDefault(w => viewNodes.Select(n => n.FlowNode).Contains(w.MasterNode));

                viewNodes.Iter(n =>
                {
                    n.DisplayNodes.Iter(node =>
                    {
                        var ucView = UcViews.First(w => w.MasterNode == n.FlowNode);

                        var on = Convert.ToBoolean(ev.Tag.BoxedValue);
                        n.LampOrigin = on;
                        ucView.UpdateOriginValue(node, on);
                    });
                });
            }
        }

        private static void HandleActionEvent(EventTaskDev ea)
        {
            if (!DicTaskDevTag.ContainsKey(ea.Tag)) return;

            var viewNodes = DicTaskDevTag[ea.Tag];
            var ucView = UcViews.FirstOrDefault(w => viewNodes.Select(n => n.FlowNode).Contains(w.MasterNode));
            viewNodes.Iter(n =>
            {
                n.DisplayNodes.Iter(node =>
                {
                    if (!IsThisSystem(node)) return;

                    var ucView = UcViews.First(w => w.MasterNode == n.FlowNode);

                    switch (ea.Tag.TagKind)
                    {
                        case (int)TaskDevTag.actionIn:
                            {
                                var on = Convert.ToUInt64(ea.Tag.BoxedValue) != 0;
                                n.LampInput = on;
                                ucView.UpdateInValue(node, on);
                                break;
                            }
                        case (int)TaskDevTag.actionOut:
                            {
                                var on = Convert.ToUInt64(ea.Tag.BoxedValue) != 0;
                                n.LampOutput = on;
                                ucView.UpdateOutValue(node, on);
                                break;
                            }
                    }
                });
            });
        }


        private static void HandleTaskDevEvent(EventTaskDev td)
        {
            if (!DicMemoryTag.ContainsKey(td.Tag)) return;
            var viewNodes = DicMemoryTag[td.Tag];

            viewNodes.Iter(n =>
            {
                n.DisplayNodes.Iter(node =>
                {
                    if (!IsThisSystem(node)) return;

                    var tags = n.TaskDevs.Cast<TaskDev>().Select(w => w.TagManager).Cast<TaskDevManager>().Select(s => s.PE);

                    switch (td.Tag.TagKind)
                    {
                        case (int)ApiItemTag.apiItemEnd:
                            {
                                var on = tags.All(s => Convert.ToBoolean(s.Value));
                                n.LampPlanEnd = on;
                                var ucView = UcViews.First(w => w.MasterNode == n.FlowNode);
                                ucView.UpdatePlanEndValue(node, on);
                                break;
                            }
                    }
                });
            });
        }
        private static bool IsThisSystem(ViewNode node)
        {
            if (!node.IsVertex
                || _sys != node.CoreVertex.Value.Parent.GetSystem())
                return false;
            else
                return true;
        }
    }
}
    
