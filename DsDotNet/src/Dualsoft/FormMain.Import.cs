using DevExpress.XtraEditors;
using DSModeler.Tree;
using Dual.Common.Core.FS;
using Dual.Common.Winform;
using Engine.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeExt;
using static Engine.Cpu.RunTime;

namespace DSModeler
{
    public partial class FormMain : XtraForm
    {
        public void ImportPowerPointWapper(string[] lastFiles)
        {

            if (0 < ProcessEvent.CurrProcess && ProcessEvent.CurrProcess < 100)
                XtraMessageBox.Show("파일 처리중 입니다.", $"{K.AppName}", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                EventCPU.CPUUnsubscribe();
                string[] files;
                if (lastFiles.IsNullOrEmpty())
                    files = FileOpenSave.OpenFiles();
                else
                    files = lastFiles;

                if (files != null)
                {
                    Task.Run(async () =>
                    {
                        await this.DoAsync(tsc =>
                        {
                            ClearModel();

                            DicCpu = PPT.ImportPowerPoint(files, this, tabbedView1, ace_Model, ace_System, ace_Device, ace_HMI);

                            Tree.LogicTree.CreateRungExprCombobox(DicCpu[Global.ActiveSys], this, tabbedView1, gridLookUpEdit_Expr);
                            Files.SetLast(files);

                            DicStatus = new Dictionary<Vertex, Status4>();

                            foreach (var item in DicCpu)
                            {
                                var sys = item.Key;
                                var reals = sys.GetVertices().OfType<Vertex>();
                                foreach (var r in reals)
                                    DicStatus.Add(r, Status4.Homing);
                            }

                            EventCPU.CPUSubscribe(DicStatus);
                            Global.Logger.Info("PPTX 파일 로딩이 완료 되었습니다.");
                            tsc.SetResult(true);
                        });
                    });
                }
            }
        }

        void ClearModel()
        {
            if(Global.ActiveSys != null)
                SIMControl.Reset(DicCpu, ace_Play, ace_HMI);

            foreach (var item in DicCpu.Values)
                item.Dispose();
            DicCpu = new Dictionary<DsSystem, DsCPU>();

            tabbedView1.Controller.CloseAll();
            tabbedView1.Documents.Clear();


            Tree.ModelTree.ClearSubBtn(ace_System);
            Tree.ModelTree.ClearSubBtn(ace_Device);
            Tree.ModelTree.ClearSubBtn(ace_HMI);
        }
    }
}