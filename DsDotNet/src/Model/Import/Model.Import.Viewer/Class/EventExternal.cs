using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Engine.Common;
using Engine.Common.FS;
using Engine.Core;

using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;

namespace Dual.Model.Import
{
    public static class EventExternal
    {

        public static void ProcessSubscribe()
        {
            ProcessEvent.ProcessSubject.Subscribe(rx =>
            {
                FormMain.TheMain.UpdateProgressBar(rx.pro);
            });
        }
        public static IDisposable DisposableCPUEvent = null;
        public static void CPUSubscribe()
        {
            if (DisposableCPUEvent == null)
            {
                DisposableCPUEvent = CpusEvent.StatusSubject.Subscribe(rx =>
                {
                    var v = rx.vertex as Vertex;
                    var ui = FormMain.TheMain;
                    if (ui._DicVertex.ContainsKey(v))
                    {
                        var ucView = ui.SelectedView;
                        var viewNode = ui._DicVertex[v];
                        viewNode.Status4 = rx.status;

                        ucView.UpdateStatus(viewNode);
                        ui.WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{v.Name}:{rx.status}", true);
                    }
                });

                CpusEvent.ValueSubject.Subscribe(rx =>
                {
                    var sys = rx.Item1;
                    var storage = rx.Item2;
                    var value = rx.Item3;

                    Debug.WriteLine($"{DateTime.Now.ToString("hh:mm:ss.fff")}\t{storage.ToText()} : {value}");
                    FormMain.TheMain.UpdateLogComboBox(storage, value, sys);
                });

            }
        }




    }

}

