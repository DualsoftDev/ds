using System;
using System.Reactive.Linq;

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

        public static void CPUSubscribe()
        {
            CpuEvent.ValueSubject.Subscribe(tuple =>
            {
                var (storage, newValue) = tuple;
                FormMain.TheMain.WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{storage.Name}:{newValue}", true);
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

