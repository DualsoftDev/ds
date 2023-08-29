using DevExpress.Accessibility;
using DevExpress.Utils.Behaviors.Common;
using Engine.Core;
using Microsoft.FSharp.Core;
using Server.HW.Common;
using Server.HW.WMX3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;
using static Engine.Core.TagModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public static class EventCPU
    {

        //static IDisposable DisposableCPUEventStatus;
        //static IDisposable DisposableCPUEventValue;
        static IDisposable DisposableHWPaixInput;
        static IDisposable DisposableTagDS;


        public static void CPUSubscribe(Dictionary<Vertex, DsType.Status4> dicStatus)
        {
            //if (DisposableCPUEventStatus == null)
            //{
            //    DisposableCPUEventStatus = CpusEvent.StatusSubject.Subscribe(rx =>
            //    {
            //        var v = rx.vertex as Vertex;
            //        string txtStatus = "[ERR]";
            //        switch (rx.status.ToString())
            //        {
            //            case "Ready": txtStatus = "[R]"; break;
            //            case "Going": txtStatus = "[G]"; break;
            //            case "Finish": txtStatus = "[F]"; break;
            //            case "Homing": txtStatus = "[H]"; break;
            //            default:
            //                break;
            //        }
            //        // if (DisposableCPUEventStatus != null) //외부에서 CPUUnsubscribe() 했을 경우가 아니면
            //        dicStatus[v] = rx.status;
            //        Global.StatusChangeSubject.OnNext(Tuple.Create(v, dicStatus[v]));
            //        var log = CreateLogicLog(v.QualifiedName, txtStatus, rx.status.ToString(), v.Parent.GetSystem());
            //        LogicLog.TryAdd(log);

            //    });
            //}
            //if (DisposableCPUEventValue == null)
            //{
            //    DisposableCPUEventValue = CpusEvent.ValueSubject.Subscribe(rx =>
            //    {
            //        var value = rx.Item3;
            //        if (value is bool)
            //        {
            //            var sys = rx.Item1;
            //            var storage = rx.Item2;
            //            var log = CreateLogicLog(storage.Name, value.ToString(), TagKindExt.GetVertexTagKindText(storage), sys);
            //            LogicLog.TryAdd(log);

            //        }

            //        if (Global.SimReset)
            //            Task.Yield(); //리셋은 시뮬레이션 속도 영향 없음
            //        else
            //            Task.Delay(ControlProperty.GetDelayMsec()).Wait();
            //    });
            //}
            //if (DisposableHWPaixOutput == null && Global.CpuRunMode.IsPackagePC())
            //{
            //    DisposableHWPaixOutput = CpusEvent.ValueHWOutSubject.Subscribe(rx =>
            //    {
            //        var value = rx.Item3;
            //        var tag = rx.Item2 as Tag<bool>;
            //        var tagHW = PcControl.DicActionOut[tag];
            //        tagHW.WriteRequestValue = value; 

            //        Global.Logger.Debug($"PaixO {tag.Address} value: {tag.Value} [{tag.Name}]");
            //    });
            //}
            if (DisposableHWPaixInput == null && Global.CpuRunMode.IsPackagePC())
            {
                DisposableHWPaixInput = Global.PaixDriver.Conn.Subject.OfType<TagValueChangedEvent>()
                .Subscribe(evt =>
                {
                    var t = evt.Tag as WMXTag;
                    if (t.IOType == TagIOType.Output) return;
                    var tag = PcControl.DicActionIn[t];
                    tag.BoxedValue = t.Value;

                    Global.Logger.Debug($"PaixI {tag.Address} value: {tag.BoxedValue} [{tag.Name}]");
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
                            var txtStatus = "";
                            var sys = t.Target.Parent.GetSystem();
                            var tagKind = TagKindExt.GetVertexTagKindText(t.Tag);
                            switch (t.TagKind)
                            {
                                case VertexTag.ready:  txtStatus = "[R]"; dicStatus[t.Target] = Status4.Ready; break;
                                case VertexTag.going:  txtStatus = "[G]"; dicStatus[t.Target] = Status4.Going; break;
                                case VertexTag.finish: txtStatus = "[F]"; dicStatus[t.Target] = Status4.Finish; break;
                                case VertexTag.homing: txtStatus = "[H]"; dicStatus[t.Target] = Status4.Homing; break;
                                default:
                                    break;
                            }
                            if (txtStatus != "" &&  (bool)t.Tag.BoxedValue)
                            {
                                Task.Delay(ControlProperty.GetDelayMsec()).Wait();
                                var logV = CreateLogicLog(t.Tag.Name, txtStatus, tagKind, sys);
                                LogicLog.TryAdd(logV);
                            }
                          
                               
                            Global.StatusChangeSubject.OnNext(Tuple.Create(t.Target, dicStatus[t.Target]));
                            var logS = CreateLogicLog(t.Target.QualifiedName, t.Tag.BoxedValue.ToString(), tagKind, sys);
                            LogicLog.TryAdd(logS);
                           

                        }
                        else if (evt.IsEventAction && Global.CpuRunMode.IsPackagePC())
                        {
                            var tag = (evt as EventAction).Tag as Tag<bool>;
                            if (PcControl.DicActionOut.ContainsKey(tag))
                            {
                                var tagHW = PcControl.DicActionOut[tag];
                                tagHW.WriteRequestValue = tag.Value;
                            }

                            Global.Logger.Debug($"PaixO {tag.Address} value: {tag.Value} [{tag.Name}]");
                        }
                    });
            }

        }

        private static ValueLog CreateLogicLog(string name, string value, string tagKind, ISystem sys)
        {
            var valueLog = new ValueLog()
            {
                Name = name,
                Value = value.ToString(),
                System = ((DsSystem)sys).Name,
                TagKind = tagKind
            };

            return valueLog;
        }

      
        public static void CPUUnsubscribe()
        {
            //DisposableCPUEventStatus?.Dispose();
            //DisposableCPUEventStatus = null;
            //DisposableCPUEventValue?.Dispose();
            //DisposableCPUEventValue = null;
            DisposableHWPaixInput?.Dispose();
            DisposableHWPaixInput = null;
            DisposableTagDS?.Dispose();
            DisposableTagDS = null;
        }

    }

}

