using System;
using System.Reactive.Linq;

using Dual.Common.Core;
using Dual.Common.Winform;

using Dual.Common.Core.FS;
using Engine.Core;

using static Dual.Common.Core.FS.MessageEvent;
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

        public static void CPUSubscribe()
        {
            FormMain.TheMain._DicCpu.ForEach(x =>
            {
                var sys = x.Key;
                var cpu = x.Value;
                x.Key.ValueChangeSubject.Subscribe(tuple =>
                {
                    var (storage, newValue) = tuple;

                    //FormMain.TheMain.WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{storage.Name}:{newValue}", true);
                    FormMain.TheMain.UpdateLogComboBox(storage, newValue, cpu);

                });
            });

            CpuEvent.StatusSubject.Subscribe(rx =>
            {
                var v = rx.vertex as Vertex;
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

