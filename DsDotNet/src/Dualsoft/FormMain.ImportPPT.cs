using DevExpress.XtraEditors;
using DSModeler.Tree;
using Dual.Common.Core;
using Dual.Common.Winform;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeExt;


namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        bool ImportingPPT = false;
        public void ImportPowerPointWapper(string[] lastFiles)
        {
            if (ImportingPPT) return;
            if (Global.BusyCheck()) return;

            EventCPU.CPUUnsubscribe();
            string[] files;
            if (lastFiles.IsNullOrEmpty())
                files = FileOpenSave.OpenFiles();
            else
                files = lastFiles;

            if (files == null) return;

            Task.Run(async () =>
            {
                try
                {
                    ImportingPPT = true;
                    PcControl.ClearModel(this);
                    Files.SetLast(files);
                    var dicCpu = await PPT.ImportPowerPoint(files, this);
                    if (!dicCpu.Any()) { return; }

                    await PcControl.CreateRunCpuSingle(dicCpu);
                    PcControl.UpdateDevice(gle_Device);

                    EventCPU.CPUSubscribe();

                    LogicTree.UpdateExpr(gle_Expr, toggleSwitch_showDeviceExpr.IsOn);

                    Global.Logger.Info("PPTX 파일 로딩이 완료 되었습니다.");
                    ImportingPPT = false;
                }
                catch (Exception ex) { ImportingPPT = false; Global.Logger.Error(ex.Message); }
            });
        }

    }
}