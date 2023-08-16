using Engine.Core;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;

namespace DSModeler
{
    public static class EventCPU
    {

        static IDisposable DisposableCPUEventValue;
        static IDisposable DisposableCPUEventStatus;
        private static int GetDelayMsec()
        {
            int delayMsec = 0;
            switch (Global.SimSpeed)
            {
                case 0: delayMsec = 1000; ; break;
                case 1: delayMsec = 500; ; break;
                case 2: delayMsec = 100; ; break;
                case 3: delayMsec = 50; ; break;
                case 4: delayMsec = 20; ; break;
                case 5: delayMsec = 1; ; break;
                default: delayMsec = 1; ; break;
            }
            return delayMsec;
        }

        public static void CPUSubscribe(Dictionary<Vertex, DsType.Status4> dicStatus)
        {
            if (DisposableCPUEventStatus == null)
            {
                DisposableCPUEventStatus = CpusEvent.StatusSubject.Subscribe(rx =>
                {
                    Task.Run(async () =>
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
                        if (DisposableCPUEventStatus != null) //외부에서 CPUUnsubscribe() 했을 경우가 아니면
                            Global.StatusChangeSubject.OnNext(Tuple.Create(v, rx.status));

                        dicStatus[v] = rx.status;
                        if (!Global.SimLogHide)
                        {
                            var txt = $"{DateTime.Now:hh:mm:ss.fff}  {txtStatus}  {v.ToText()}";
                            if (DisposableCPUEventStatus != null) //외부에서 CPUUnsubscribe() 했을 경우가 아니면
                                Global.Logger.Info(txt);
                        }
                        //await Task.Yield();
                        await Task.Delay(10);
                    }).Wait();
                });
            }
            if (DisposableCPUEventValue == null)
            {
                DisposableCPUEventValue = CpusEvent.ValueSubject.Subscribe(rx =>
                {
                    if (Global.SimLogHide) return;
                    Task.Run(async () =>
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
                             if (DisposableCPUEventValue != null) //외부에서 CPUUnsubscribe() 했을 경우가 아니면
                                 Global.Logger.Info(txt);
                         }
                         await Task.Delay(GetDelayMsec());
                     }).Wait();
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

