using System;
using System.Reactive.Linq;

using Dual.Common.Core;
using Dual.Common.Winform;

using Dual.Common.Core.FS;
using Engine.Core;

using static Dual.Common.Core.FS.MessageEvent;
using static Engine.Core.CoreModule;
using Model.Import.Office;
using System.Diagnostics;

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

        static IDisposable DisposableCPUEvent;
        public static void CPUSubscribe()
        {
            if (DisposableCPUEvent == null)
            {
                DisposableCPUEvent = CpusEvent.StatusSubject.Subscribe(rx =>
                {
                    var v = rx.vertex as Vertex;
                    var ui = FormMain.TheMain;
                    if (ui._DicVertexMy.ContainsKey(v))
                        UpdateView(rx, ui.SelectedViewMy, ui._DicVertexMy[v]);
                    else if (ui._DicVertexEx.ContainsKey(v))
                        UpdateView(rx, ui.SelectedViewEx, ui._DicVertexEx[v]);
                });

                CpusEvent.ValueSubject.Subscribe(rx =>
                {
                    var sys = rx.Item1;
                    var storage = rx.Item2;
                    var value = rx.Item3;
                    if (value is bool)
                    {
                        Debug.WriteLine($"{DateTime.Now.ToString("hh:mm:ss.fff")}\t{storage.ToText()} : {value}");
                        FormMain.TheMain.UpdateLogComboBox(storage, value, sys);
                    }
                });

            }
        }

        private static void UpdateView(CpusEvent.VertexStatusParam rx, UCView ucView, ViewModule.ViewNode viewNode)
        {
            viewNode.Status4 = rx.status;
            var v = rx.vertex as Vertex;

            ucView.UpdateStatus(viewNode);

            FormMain.TheMain.WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{v.Name}:{rx.status}", true);
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

