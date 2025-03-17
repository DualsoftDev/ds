using Diagram.View.MSAGL;
using DocumentFormat.OpenXml.Drawing.Diagrams;
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
using static Engine.CodeGenCPU.ConvertCpuVertex;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.Interface;
using static Engine.Core.RuntimeGeneratorModule;
using static Engine.Core.TagKindList;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagEvent;
using static Engine.Import.Office.ImportViewModule;
using static Engine.Import.Office.ViewModule;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.VariantTypes;
using static Engine.Info.DBWriterModule;
using System.Runtime.Versioning;
using Dual.Common.Base.CS;
using static Engine.Core.CoreModule.GraphItemsModule;
using static Engine.Core.CoreModule.ApiItemsModule;
using Newtonsoft.Json;
using static Engine.Core.CoreModule.SystemModule;

namespace Diagram.View.MSAGL
{
    [SupportedOSPlatform("windows")]
    public static class ViewUtil
    {
        public static List<UcView> UcViews { get; set; } = new();
        public static Subject<TagEvent> VertexEventSubject = new();

        public static Dictionary<Vertex, ViewVertex> DicNode = new();

        static IDisposable _Disposable;
        static Dictionary<IStorage, List<ViewVertex>> DicTaskDevTag = new();
        static Dictionary<IStorage, List<ViewVertex>> DicMemoryTag = new();
        static private DsSystem _sys = null;

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
                        ? c.TaskDefs.Cast<TaskDev>().ToList()
                        : new List<TaskDev>();
                    var viewVertex = CreateViewVertex(fv, v, dicViewNodes[v], tasks);
                    DicNode[v] = viewVertex;

                    if (v.GetPure() is Call call && call.IsJob)
                    {
                        UpdateDicCallPlanTag(call, viewVertex);
                    }

                    viewVertex.TaskDevs.Cast<TaskDev>().Iter(t =>
                    {
                        UpdateDicTaskDevTag(t.InTag, viewVertex);
                        UpdateDicTaskDevTag(t.OutTag, viewVertex);
                    });

                }
            });


            var allSys = sys.GetRecursiveLoadedSystems().ToList();
            allSys.Add(sys);
            allSys.Iter(sys =>
            {
                sys.GetRealVertices().Iter(real =>
                {
                    var og = (real.TagManager as RealVertexTagManager).OG;
                    UpdateOriginVertexTag(og, DicNode[real]);

                    real.GetSharedReal().Iter(alias => UpdateOriginVertexTag(og, DicNode[alias]));
                });
            });

            void UpdateOriginVertexTag(IStorage tag, ViewVertex viewVertex)
            {
                if (!DicMemoryTag.ContainsKey(tag))
                    DicMemoryTag.Add(tag, new List<ViewVertex>());
                DicMemoryTag[tag].Add(viewVertex);
            }


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

            void UpdateDicCallPlanTag(Call c, ViewVertex viewVertex)
            {
                var dic = DicTaskDevTag;
                var cm = c.TagManager as CoinVertexTagManager;
                var ps = cm.PS;
                var pe = cm.PE;
                var ci = cm.CallIn;
                var co = cm.CallOut;
                //var po = cm.PO;
                if (!dic.ContainsKey(ps)) dic.Add(ps, new List<ViewVertex>() { viewVertex }); else dic[ps].Add(viewVertex);
                if (!dic.ContainsKey(pe)) dic.Add(pe, new List<ViewVertex>() { viewVertex }); else dic[pe].Add(viewVertex);
                if (!dic.ContainsKey(ci)) dic.Add(ci, new List<ViewVertex>() { viewVertex }); else dic[ci].Add(viewVertex);
                if (!dic.ContainsKey(co)) dic.Add(co, new List<ViewVertex>() { viewVertex }); else dic[co].Add(viewVertex);
            }




            void ViewChangeSubject()
            {
                _Disposable?.Dispose();
                _Disposable = VertexEventSubject.Subscribe(rx =>
                {
                    EventVertex eventVertex = null;
                    if (rx is EventVertex ev)
                    {
                        eventVertex = ev;
                        HandleVertexEvent(ev);
                    }
                    else if (rx.IsEventTaskDev)
                    {
                        HandleTaskDevEvent(rx as EventTaskDev);
                    }
                });
            }
        }

        private static void HandleVertexEvent(EventVertex ev)
        {

            if (DicNode.ContainsKey(ev.Target))
            {
                var vv = DicNode[ev.Target];
                vv.Nodes.Iter(node =>
                {
                    var ucView = UcViews.FirstOrDefault(w => w.MasterNode == DicNode[node.CoreVertex.Value].FlowNode);

                    switch (ev.TagKind)
                    {
                        case VertexTag.planEnd:
                            {
                                bool on = false;
                                if (ev.Target is Call c || ev.Target is Alias s)
                                {
                                    if (ev.Target.TryGetPureCall() != null)
                                    {
                                        var cv = ev.Target.GetPureCall();
                                        on = Convert.ToBoolean((cv.TagManager as CoinVertexTagManager).PE.Value);
                                    }
                                }
                                vv.LampPlanEnd = on;

                                ucView?.UpdatePlanEndValue(node, on);
                                break;
                            }
                        case VertexTag.callIn:
                            {
                                var on = Convert.ToBoolean(ev.Tag.BoxedValue);
                                vv.LampInput = on;
                                ucView?.UpdateInValue(node, on, true);
                                break;
                            }
                        case VertexTag.callOut:
                            {
                                var on = Convert.ToBoolean(ev.Tag.BoxedValue);
                                vv.LampOutput = on;
                                ucView?.UpdateOutValue(node, on, true);
                                break;
                            }
                    }
                });
            }

            if (ev.IsStatusTag() && (bool)ev.Tag.BoxedValue)
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
                    if (ucView != null)
                    {
                        ucView.UpdateStatus(node);
                        ucView.ForceUpdateLabelText();
                    }
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

            if (ev.TagKind == VertexTag.origin && DicMemoryTag.ContainsKey(ev.Tag))
            {
                var viewNodes = DicMemoryTag[ev.Tag];

                viewNodes.Iter(n =>
                {
                    n.DisplayNodes.Iter(node =>
                    {
                        var ucView = UcViews.FirstOrDefault(w => w.MasterNode == n.FlowNode);

                        var on = Convert.ToBoolean(ev.Tag.BoxedValue);
                        n.LampOrigin = on;
                        ucView?.UpdateOriginValue(node, on);
                    });
                });
            }
        }

        private static void HandleTaskDevEvent(EventTaskDev td)
        {
            if (!DicTaskDevTag.ContainsKey(td.Tag)) return;

            var viewNodes = DicTaskDevTag[td.Tag];

            viewNodes.Iter(n =>
            {
                n.DisplayNodes.Iter(node =>
                {
                    if (!IsThisSystem(node)) return;


                    switch (td.Tag.TagKind)
                    {
                        case (int)TaskDevTag.actionIn:
                            {
                                //io table 만들기
                                break;
                            }
                        case (int)TaskDevTag.actionOut:
                            {
                                //io table 만들기

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

