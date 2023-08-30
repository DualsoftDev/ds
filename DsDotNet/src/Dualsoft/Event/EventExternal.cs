using Engine.Core;
using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;
using static Engine.Core.TagModule;

namespace DSModeler
{
    public static class EventCPU
    {

        static IDisposable DisposableHWPaixInput;
        static IDisposable DisposableTagDS;


        public static void CPUSubscribe(Dictionary<Vertex, DsType.Status4> dicStatus)
        {
            if (DisposableHWPaixInput == null && Global.CpuRunMode.IsPackagePC())
            {
                DisposableHWPaixInput = Global.PaixDriver.Conn.Subject.OfType<TagValueChangedEvent>()
                .Subscribe(evt =>
                {
                    var t = evt.Tag as WMXTag;
                    if (t.IOType == TagIOType.Output) return;
                    var tag = PcControl.DicActionIn[t];
                    tag.BoxedValue = t.Value;

                    Global.Logger.Debug($"HW_IN {tag.Address} value: {tag.BoxedValue} [{tag.Name}]");
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
                                Global.StatusChangeSubject.OnNext(Tuple.Create(t.Target, dicStatus[t.Target]));

                            LogicLog.AddLogicLog(t);
                            Task.Delay(ControlProperty.GetDelayMsec()).Wait();
                        }
                        else if (evt.IsEventAction && Global.CpuRunMode.IsPackagePC())
                        {
                            var tag = (evt as EventAction).Tag as Tag<bool>;
                            if (PcControl.DicActionOut.ContainsKey(tag))
                            {
                                var tagHW = PcControl.DicActionOut[tag];
                                tagHW.WriteRequestValue = tag.Value;
                            }

                            Global.Logger.Debug($"HW_OUT {tag.Address} value: {tag.Value} [{tag.Name}]");
                        }
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

}

