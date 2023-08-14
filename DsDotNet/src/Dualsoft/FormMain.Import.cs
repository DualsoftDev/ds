using DevExpress.XtraEditors;
using Dual.Common.Core.FS;
using Engine.Core;
using Engine.Cpu;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;
using static Engine.Core.DsType;
using static Engine.Core.EdgeExt;
using static Engine.Core.ExpressionModule;
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
                    clearModel();

                    DicCpu = PPT.ImportPowerPoint(files, this, tabbedView1, ace_Model, ace_System, ace_Device, ace_HMI);

                    createRungExprCombobox(DicCpu[Global.ActiveSys]);
                    Files.SetLast(files);

                    DicStatus = new Dictionary<Vertex, Status4>();

                    foreach (var item in DicCpu)
                    {
                        var sys = item.Key;
                        var reals = sys.GetVertices().OfType<Vertex>();
                        foreach (var r in reals)
                            DicStatus.Add(r, Status4.Homing);
                    }


                    Global.Logger.Info("PPTX 파일 로딩이 완료 되었습니다.");
                }

                EventCPU.CPUSubscribe(DicStatus);

            }


        }

        void clearModel()
        {
            foreach (var item in DicCpu.Values)
                item.Dispose();
            DicCpu = new Dictionary<DsSystem, DsCPU>();

            tabbedView1.Controller.CloseAll();
            tabbedView1.Documents.Clear();

            simpleButton_OpenPLC.Visible = false;

            Model.ClearSubBtn(ace_System);
            Model.ClearSubBtn(ace_Device);
            Model.ClearSubBtn(ace_HMI);
        }

        void createRungExprCombobox(DsCPU dsCPU)
        {
            var dicStatement = new Dictionary<string, CommentedStatement>();
            comboBoxEdit_Expr.Properties.Items.Clear();
            comboBoxEdit_Expr.Properties.SelectedIndexChanged += (ss, ee) =>
            {
                var textForm = DocControl.CreateDocExpr(this, tabbedView1);
                if (textForm == null) return;
                DSFile.UpdateExpr(dicStatement, textForm, comboBoxEdit_Expr);
            };

            int cnt = 0;
            foreach (var rung in dsCPU.CommentedStatements)
            {
                dicStatement.Add($"{cnt++}", rung);
                var txt = $"{cnt};\t[{rung.TargetName}][{rung.TargetValue}] \t\t\t Spec:{rung.comment.Replace("%", " ").Replace("$", " ")}";
                comboBoxEdit_Expr.Properties.Items.Add(txt);
            }
        }

     
    }
}