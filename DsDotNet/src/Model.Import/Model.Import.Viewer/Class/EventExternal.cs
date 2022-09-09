using Model.Import.Office;
using System;
using System.Linq;
using static Model.Import.Office.Object;

namespace Dual.Model.Import
{
    public static class EventExternal
    {

        public static void ProcessSubscribe()
        {
            Event.ProcessSubject.Subscribe(rx =>
            {
                FormMain.TheMain.UpdateProgressBar(rx.pro);
            });
        }

        public static void MSGSubscribe()
        {
            Event.MSGSubject.Subscribe(rx =>
                {
                    FormMain.TheMain.WriteDebugMsg(rx.Time, rx.Level, $"{rx.Message}");
                });
        }

        public static void SegSubscribe()
        {
            Event.SegSubject.Subscribe(rx =>
            {
                var sys = rx.Seg.BaseSys as DsSystem;
                var seg = rx.Seg as Seg;

                sys.RootFlow().ToList().ForEach(flow =>
                {
                    if (flow.UsedSegs.Contains(seg))
                        if (FormMain.TheMain.DicUI.ContainsKey(flow))
                            ((UCView)FormMain.TheMain.DicUI[flow].Tag).Update(seg);
                });
            });
        }
    }

}

