using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using Dual.Common.Core.FS;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Core.CoreModule;
using static Engine.Core.Interface;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ViewModule;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        internal void ImportPowerPointWapper()
        {
            if (0 < ProcessEvent.CurrProcess && ProcessEvent.CurrProcess < 100)
                XtraMessageBox.Show("파일 처리중 입니다.", $"{K.AppName}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                var files = FileOpenSave.OpenFiles();
                if (files != null)
                {
                    clearModel();

                    _DicCpu = PPT.ImportPowerPoint(files, this, tabbedView1, ace_Model, ace_System, ace_Device, ace_HMI);


                    Global.Logger.Info("PPTX 파일 로딩이 완료 되었습니다.");
                }
            }
        } 

        void clearModel()
        {
            foreach (var item in _DicCpu.Values)
                item.Dispose();
            _DicCpu = new Dictionary<DsSystem, DsCPU>();

            tabbedView1.Controller.CloseAll();
            tabbedView1.Documents.Clear();

            Model.ClearSubBtn(ace_System);
            Model.ClearSubBtn(ace_Device);
            Model.ClearSubBtn(ace_HMI);
        }
       
    }
}