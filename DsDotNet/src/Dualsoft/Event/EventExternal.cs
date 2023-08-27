using DevExpress.Accessibility;
using Engine.Core;
using Server.HW.WMX3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagModule;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public static class EventCPU
    {

        static IDisposable DisposableCPUEventStatus;
        static IDisposable DisposableCPUEventValue;
        static IDisposable DisposableHWPaixOutput;
        static IDisposable DisposableHWPaixInput;


        public static void CPUSubscribe(Dictionary<Vertex, DsType.Status4> dicStatus)
        {
            if (DisposableCPUEventStatus == null)
            {
                DisposableCPUEventStatus = CpusEvent.StatusSubject.Subscribe(rx =>
                {
                    var v = rx.vertex as Vertex;
                    string txtStatus = "[ERR]";
                    switch (rx.status.ToString())
                    {
                        case "Ready": txtStatus = "[R]"; break;
                        case "Going": txtStatus = "[G]"; break;
                        case "Finish": txtStatus = "[F]"; break;
                        case "Homing": txtStatus = "[H]"; break;
                        default:
                            break;
                    }
                    // if (DisposableCPUEventStatus != null) //외부에서 CPUUnsubscribe() 했을 경우가 아니면
                    dicStatus[v] = rx.status;
                    Global.StatusChangeSubject.OnNext(Tuple.Create(v, dicStatus[v]));
                    var log = CreateLogicLog(v.QualifiedName, txtStatus, rx.status.ToString(), v.Parent.GetSystem());
                    LogicLog.TryAdd(log);

                });
            }
            if (DisposableCPUEventValue == null)
            {
                DisposableHWPaixOutput = CpusEvent.ValueSubject.Subscribe(rx =>
                {
                    var value = rx.Item3;
                    if (value is bool)
                    {
                        var sys = rx.Item1;
                        var storage = rx.Item2;
                        var log = CreateLogicLog(storage.Name, value.ToString(), TagKindExt.GetVertexTagKindText(storage), sys);
                        LogicLog.TryAdd(log);

                    }

                    if (Global.SimReset)
                        Task.Yield(); //리셋은 시뮬레이션 속도 영향 없음
                    else
                        Task.Delay(SIMProperty.GetDelayMsec()).Wait();
                });
            }
            if (DisposableHWPaixOutput == null)
            {
                DisposableHWPaixOutput = CpusEvent.ValueHWOutSubject.Subscribe(rx =>
                {
                    var value = rx.Item3;
                    var tag = rx.Item2 as Tag<bool>;

                    Global.Logger.Debug($"PaixO {tag.Address} value: {value} [{tag.Name}]");
                });
            }
            if (DisposableHWPaixInput == null)
            {
                DisposableHWPaixInput = HWEvent.ValueChangeSubjectPaixInputs.Subscribe(rx =>
                {
                    var index = rx.Item1;
                    var value = rx.Item2;
                    var tag = SIMControl.DicActionInput[$"I{index}"];
                    tag.BoxedValue = value;
                    Global.Logger.Debug($"PaixI {tag.Address} value: {value} [{tag.Name}]");
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
            DisposableCPUEventStatus?.Dispose();
            DisposableCPUEventStatus = null;
            DisposableCPUEventValue?.Dispose();
            DisposableCPUEventValue = null;
            DisposableHWPaixOutput?.Dispose();
            DisposableHWPaixOutput = null;
            DisposableHWPaixInput?.Dispose();
            DisposableHWPaixInput = null;
        }

    }

}

