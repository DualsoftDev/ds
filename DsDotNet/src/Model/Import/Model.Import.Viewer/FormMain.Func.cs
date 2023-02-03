using Engine.CodeGenCPU;
using Engine.Common;
using Engine.Common.FS;
using Model.Import.Office;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.DsTextProperty;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ImportViewModule;
using static Model.Import.Office.PPTObjectModule;
using Color = System.Drawing.Color;

namespace Dual.Model.Import
{

    public partial class FormMain : Form
    {
        internal void DisplayTextExpr(string dsExpr, Color color)
        {
            richTextBox_ds.AppendText("\n\n\n");

            var textLines = dsExpr.Split('\n');
            this.Do(() =>
            {
                textLines.ToList().ForEach(f =>
                {
                    richTextBox_ds.AppendTextColor(f, color);
                });
            });

            this.Do(() => richTextBox_ds.Select(0, 0));
            this.Do(() => richTextBox_ds.ScrollToCaret());
        }

        internal void DisplayTextModel(Color color, string dsText, bool bShowLine = false)
        {

            this.Do(() => richTextBox_ds.Clear());

            var textLines = dsText.Split('\n');
            Random r = new Random();
            Color rndColor = Color.LightGoldenrodYellow;
            int lineCnt = textLines.Count();
            int lineCur = 0;
            this.Do(() =>
            {
                textLines.ToList().ForEach(f =>
                {
                    int pro = 50 + System.Convert.ToInt32(System.Convert.ToSingle(lineCur++) / (lineCnt) * 50f);
                    if (bShowLine) richTextBox_ds.AppendText(lineCur.ToString("000") + ";");

                    if (color == Color.Transparent)
                    {
                        if (f.StartsWith($"[{TextSystem}") || f.Contains($"[{TextFlow}]")  //[flow] F = {} 한줄제외
                        || f.Contains($"[{TextAddress}]")
                        || f.Contains($"[{TextLayout}]")
                        || f.Contains($"[{TextJobs}]")
                        )
                        {
                            rndColor = Color.FromArgb(r.Next(130, 230), r.Next(130, 230), r.Next(130, 230));
                            this.Do(() => richTextBox_ds.ScrollToCaret());
                            ProcessEvent.DoWork(pro);
                        }
                        richTextBox_ds.AppendTextColor(f + "\n", rndColor);
                    }
                    else
                        richTextBox_ds.AppendTextColor(f, color);
                });
            });

            this.Do(() => richTextBox_ds.Select(0, 0));
            this.Do(() => richTextBox_ds.ScrollToCaret());
            ProcessEvent.DoWork(0);
        }

        internal void WriteDebugMsg(DateTime time, MSGLevel level, string msg, bool bScrollToCaret = false)
        {
            this.Do(() =>
            {
                var color = Color.Black;
                if (level.IsMsgError)
                {
                    richTextBox_Debug.AppendTextColor($"\r\n{msg}", Color.Red);
                    ProcessEvent.DoWork(0);
                }
                else
                {
                    if (level.IsMsgWarn) color = Color.Purple;
                    richTextBox_Debug.AppendTextColor($"\r\n{time} : {msg}", color);
                }
                if (bScrollToCaret)
                    richTextBox_Debug.ScrollToCaret();
            });
        }
       // CoreModule.Model _Demo = new CoreModule.Model();
        internal void HelpLoad()
        {
            splitContainer1.Panel1Collapsed = false;

            this.Size = new Size(1600, 1000);
            _HelpSystem = ImportCheck.GetDemoModel("test");
            var viewNodes = ImportViewUtil.ConvertViewNodes(_HelpSystem);
            UpdateGraphUI(viewNodes);
        }

        internal void RefreshGraph()
        {
            foreach(TabPage tab in xtraTabControl_My.TabPages)
                ((UCView)tab.Tag).RefreshGraph();
        }

        internal void ReloadPPT()
        {
            if(_PathPPTs.Where(w=> !File.Exists(w)).IsEmpty())
                ImportPPTs(_PathPPTs);
        }
        internal void ResultBtnAbleUI(bool bAble)
        {
            this.Do(() =>
            {
                if (bAble)
                {
                    button_CreateExcel.Enabled = true;
                    button_OpenFolder.Visible = true;
                }
                else
                {
                    button_copy.Visible = false;
                    button_CreateExcel.Enabled = false;
                }
            });
        }
        internal void TestDebug(bool bLoadExcel, bool bDummyTestAddress)
        {
            string path = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\")) + "src\\UnitTest\\UnitTest.Model\\ImportOfficeExample\\Sample\\s.pptx";
            bool debug = File.Exists(path);
            if (debug)
            {
                _PathPPTs.Clear();
                _PathPPTs.Add(path);
                if (bDummyTestAddress)
                {
                    ImportPPTs(_PathPPTs);
                    CpuLoader.testAddressSetting(SelectedSystem);
                    var plcPath = Path.ChangeExtension(UtilFile.GetNewPathXls(_PathPPTs), "xml");
                    Directory.CreateDirectory(Path.GetDirectoryName(plcPath));
                    ExportPLC($"{Path.GetDirectoryName(plcPath)}\\DSLogic{DateTime.Now.ToString("(HH-mm-ss)")}.xml");
                }
                else
                {
                    if (bLoadExcel)
                        ImportPPTsXls(_PathPPTs, Path.ChangeExtension(path, "xlsx"));
                    else
                        ImportPPTs(_PathPPTs);
                }
            }
        }
    }
}
