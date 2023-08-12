using Dual.Common.Core;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using static Engine.Core.CoreModule;

namespace DSModeler
{
    public static class EventCPU
    {

        static IDisposable DisposableCPUEventValue;
        static IDisposable DisposableCPUEventStatus;
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
                    Global.StatusChangeSubject.OnNext(Tuple.Create(v, rx.status));

                    dicStatus[v] = rx.status;
                    var txt = $"{DateTime.Now:hh:mm:ss.fff}  {txtStatus}  {v.ToText()}";
                    Global.Logger.Info(txt);
                });
            }
            if (DisposableCPUEventValue == null)
            {
                DisposableCPUEventValue = CpusEvent.ValueSubject.Subscribe(rx =>
                {
                    var sys = rx.Item1;
                    var storage = rx.Item2;
                    var value = rx.Item3;
                    if (value is bool)
                    {
                        var txtValue = value.ToString();
                        txtValue = txtValue == "True"
                                       ? "●" : txtValue == "False" ?
                                               "○" : txtValue;

                        var txt = $"{DateTime.Now.ToString("hh:mm:ss.fff")}  {txtValue}  {storage.ToText()}";
                        Global.Logger.Info(txt);
                    }
                });
            }
        }
        public static void CPUUnsubscribe()
        {
            DisposableCPUEventStatus?.Dispose();
            DisposableCPUEventStatus = null;
            DisposableCPUEventValue?.Dispose();
            DisposableCPUEventValue = null;
        }

    }

}

