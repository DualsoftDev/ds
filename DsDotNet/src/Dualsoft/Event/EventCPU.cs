using DevExpress.XtraEditors.Designer.Utils;
using Dual.Common.Core;
using Engine.Core;
using Microsoft.Msagl.GraphmapsWithMesh;
using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;
using static Engine.Core.TagModule;

namespace DSModeler;
[SupportedOSPlatform("windows")]
public static class EventCPU
{

    static IDisposable DisposableHWPaixInput;
    static IDisposable DisposableTagDS;

    public static void CPUSubscribe(Dictionary<CoreModule.Vertex, DsType.Status4> dicStatus)
    {
        if (DisposableHWPaixInput == null && Global.CpuRunMode.IsPackagePC())
        {
            DisposableHWPaixInput = Global.PaixDriver.Conn.Subject.OfType<TagValueChangedEvent>()
            .Subscribe(evt =>
            {
                var t = evt.Tag as WMXTag;
                if (t.IOType == TagIOType.Output)
                {
                    Global.Logger.Debug($"HW_OUT {t.Address} value: {t.Value}");
                }
                if (t.IOType == TagIOType.Input)
                {
                    var tags = PcControl.DicActionIn[t];
                    tags.Iter(tag =>
                    {
                        tag.BoxedValue = t.Value;
                        var dev = tag.Target.Value as TaskDev;
                        if (dev != null && ViewDraw.DicTask.ContainsKey(dev)) //job만정의 하고 call에 사용  안함
                        {
                            var vs = ViewDraw.DicTask[dev];
                            vs.Iter(v => ViewDraw.ActionChangeSubject
                                               .OnNext(System.Tuple.Create(v, t.Value)));
                        }
                    });
                    Global.Logger.Debug($"HW_IN {t.Address} value: {t.Value}");
                }
            });
        }

        if (DisposableTagDS == null)
        {
            DisposableTagDS =
                TagDSSubject
                .Subscribe(evt =>
                {
                    if (evt.IsEventVertex)
                    {
                        var t = evt as EventVertex;
                        var isStatus = false;
                        switch (t.TagKind)
                        {
                            case VertexTag.ready: dicStatus[t.Target] = Status4.Ready; isStatus = true; break;
                            case VertexTag.going: dicStatus[t.Target] = Status4.Going; isStatus = true; break;
                            case VertexTag.finish: dicStatus[t.Target] = Status4.Finish; isStatus = true; break;
                            case VertexTag.homing: dicStatus[t.Target] = Status4.Homing; isStatus = true; break;
                            default: break;
                        }

                        if (isStatus && (bool)t.Tag.BoxedValue)
                            ViewDraw.StatusChangeSubject.OnNext(System.Tuple.Create(t.Target, dicStatus[t.Target]));


                        if (Global.CpuRunMode.IsSimulation)
                            Task.Delay(ControlProperty.GetDelayMsec()).Wait();
                        else
                            Task.Yield();

                    }
                    else if (evt.IsEventAction && Global.CpuRunMode.IsPackagePC())
                    {
                        var tag = (evt as EventAction).Tag as Tag<bool>;
                        if (PcControl.DicActionOut.ContainsKey(tag))
                        {
                            var tagHW = PcControl.DicActionOut[tag];
                            tagHW.WriteRequestValue = tag.Value;
                        }

                    }

                    LogicLog.AddLogicLog(evt);
                });
        }

    }



    public static void CPUUnsubscribe()
    {
        DisposableHWPaixInput?.Dispose();
        DisposableHWPaixInput = null;
        DisposableTagDS?.Dispose();
        DisposableTagDS = null;
    }

}

