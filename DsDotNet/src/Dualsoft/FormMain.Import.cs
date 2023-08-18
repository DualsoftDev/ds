using DevExpress.XtraEditors;
using DSModeler.Tree;
using Dual.Common.Core;
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
            if (Global.BusyCheck()) return;

            EventCPU.CPUUnsubscribe();
            string[] files;
            if (lastFiles.IsNullOrEmpty())
                files = FileOpenSave.OpenFiles();
            else
                files = lastFiles;

            if (files != null)
            {

                ClearModel();
                Task.Run(async () =>
                {
                    await PPT.ImportPowerPoint(files, this, tabbedView1, ace_Model, ace_System, ace_Device, ace_HMI);


                    Tree.LogicTree.CreateRungExprCombobox(SIMControl.DicCpu[Global.ActiveSys], this, tabbedView1, gridLookUpEdit_Expr);
                    Files.SetLast(files);

                    ViewDraw.DicStatus = new Dictionary<Vertex, Status4>();

                    foreach (var item in SIMControl.DicCpu)
                    {
                        var sys = item.Key;
                        var reals = sys.GetVertices().OfType<Vertex>();
                        foreach (var r in reals)
                            ViewDraw.DicStatus.Add(r, Status4.Homing);
                    }

                    EventCPU.CPUSubscribe(ViewDraw.DicStatus);
                    Global.Logger.Info("PPTX 파일 로딩이 완료 되었습니다.");
                });
            }
        }

        void ClearModel()
        {
            if(Global.ActiveSys != null)
                SIMControl.Reset(ace_Play, ace_HMI);

            SIMControl.RunCpus.Iter(cpu => cpu.Dispose());
            SIMControl.DicCpu = new Dictionary<DsSystem, DsCPU>();

            tabbedView1.Controller.CloseAll();
            tabbedView1.Documents.Clear();
            barStaticItem_logCnt.Caption = "";
            LogicLog.ValueLogs.Clear();

            Global.ActiveSys = null;

            Tree.ModelTree.ClearSubBtn(ace_System);
            Tree.ModelTree.ClearSubBtn(ace_Device);
            Tree.ModelTree.ClearSubBtn(ace_HMI);
        }
    }
}