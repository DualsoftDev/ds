using Engine.Common.FS;
using System;

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

        public static void MSGSubscribe()
        {
            MessageEvent.MSGSubject.Subscribe(rx =>
                {
                    FormMain.TheMain.WriteDebugMsg(rx.Time, rx.Level, $"{rx.Message}");
                });
        }

        public static void SegSubscribe()
        {
            //CoreEvent.SegSubject.Subscribe(rx =>
            //{
            //    var seg = rx.Seg as MSeg;
            //    var sys = seg.BaseSys;

            //    sys.RootMFlow().ToList().ForEach(f =>
            //    {
            //        var flow = f as MFlow;
            //        if (flow.UsedSegs.Contains(seg))
            //            if (FormMain.TheMain.DicUI.ContainsKey(flow))
            //                ((UCView)FormMain.TheMain.DicUI[flow].Tag).Update(seg);
            //    });
            //});
        }
    }

}

