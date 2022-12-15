using Engine.Common;
using Engine.Common.FS;
using Engine.Core;
using Microsoft.Msagl.Core.DataStructures;
using System;
using System.Data.SqlTypes;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
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
            CpuEvent.ValueSubject.Subscribe(rx =>
            {
                FormMain.TheMain.WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{rx.Name}:{rx.Value}");
            });

            CpuEvent.StatusSubject.Subscribe(rx =>
            {
                var v = rx.vertex as Vertex;
                FormMain.TheMain.Do(() =>
                {
                    var ucView = FormMain.TheMain.SelectedView;
                    var viewNode = FormMain.TheMain._DicVertex[v];
                    viewNode.Status4 = rx.status;

                    ucView.UpdateStatus(viewNode);
                });

                FormMain.TheMain.WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{v.Name}:{rx.status}");
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

