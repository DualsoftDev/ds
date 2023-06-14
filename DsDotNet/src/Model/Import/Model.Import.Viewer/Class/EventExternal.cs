using System;
using System.Reactive.Linq;
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
            //FormMain.TheMain._DicCpu.ForEach(x =>
            //{
            //    var sys = x.Key;
            //    var cpu = x.Value;
            //    x.Key.ValueChangeSubject.Subscribe(async tuple =>
            //    {
            //        var (storage, newValue) = tuple;
            //        await Task.Delay(1000);
            //        FormMain.TheMain.UpdateLogComboBox(storage, newValue, cpu);
            //    });
            //});

            if (DisposableCPUEvent == null)
            {

                DisposableCPUEvent = CpuEvent.StatusSubject.Subscribe(async rx =>
                {
                    var v = rx.vertex as Vertex;
                    await Task.Delay(1);
                    FormMain.TheMain.Do(() =>
                    {
                        if (FormMain.TheMain._DicVertex.ContainsKey(v))
                        {
                            var ucView = FormMain.TheMain.SelectedView;
                            var viewNode = FormMain.TheMain._DicVertex[v];
                            viewNode.Status4 = rx.status;

                            ucView.UpdateStatus(viewNode);
                            FormMain.TheMain.WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{v.Name}:{rx.status}", true);
                        }
                        else { }
                    });

                });

                CpuEvent.ValueSubject.Subscribe(async rx =>
                {
                    await Task.Delay(1);
                    FormMain.TheMain.UpdateLogComboBox(rx.storage, rx.value, rx.sys);
                });

            }
        }




        public static void MSGSubscribe()
        {
            MessageEvent.MSGSubject.Subscribe(rx =>
                {
                    FormMain.TheMain.WriteDebugMsg(rx.Time, rx.Level, $"{rx.Message}");
                });
        }

    }

}

