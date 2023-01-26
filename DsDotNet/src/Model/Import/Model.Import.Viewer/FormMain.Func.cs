using Engine.Common;
using Engine.Common.FS;
using Engine.Core;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Engine.CodeGenCPU.CpuLoader;
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Engine.Core.DsTextProperty;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.ImportPPTModule;
using static Model.Import.Office.ImportViewModule;
using Color = System.Drawing.Color;

namespace Dual.Model.Import
{

    public partial class FormMain : Form
    {

        //복수 Active system ppt 불러오기
        internal void ImportPowerPoint(List<string> paths)
        {
            try
            {
                var results = ImportPPT.GetLoadingAllSystem(paths);
                _DicCpu = new Dictionary<DsSystem, DsCPU>();
                _DicViews = new Dictionary<DsSystem, IEnumerable<ViewModule.ViewNode>>();
                var storages = new Dictionary<string, Interface.IStorage>();
                foreach (var view in results)
                {
                    _DicViews.Add(view.System, view.Views.ToList());
                    if (!view.IsActive) continue;

                    var rungs = Cpu.LoadStatements(view.System, storages);
                    rungs.ForEach(s =>
                    {
                        _DicCpu.Add(s.ToSystem(), new DsCPU(s.CommentedStatements(), s.ToSystem()));

                        if(s.IsActive)
                        {

                            var systemView = new SystemView()
                                        { Display = s.ToSystem().Name
                                        , System = s.ToSystem()
                                        , ViewNodes = view.Views.ToList() };
                            comboBox_System.Items.Add(systemView);



                        }
                    });
                }

                comboBox_System.DisplayMember = "Display";
                if (comboBox_System.Items.Count > 0)
                    comboBox_System.SelectedIndex = 0;

                paths.ForEach(f =>
                    WriteDebugMsg(DateTime.Now, MSGLevel.MsgWarn, $"{f} 불러오기 성공!!"));


                EventExternal.CPUSubscribe();
                _DicCpu.ForEach(f =>
                {
                    f.Value.Run();
                    testReadyAutoDrive(f.Key);
                    f.Value.ScanOnce();
                });



                //_DicCpu.First().Value.Run();
                //_DicCpu.First().Value.ScanOnce();



                ProcessEvent.DoWork(0);
            }
            catch (Exception ex)
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.MsgError, ex.Message);
            }
            finally { Busy = false; }
        }
        internal void ImportExcel(string path)
        {
            try
            {
                if (UtilFile.BusyCheck()) return;
                Busy = true;
                MSGInfo($"{_PathXLS} 불러오는 중!!");
                ImportIOTable.ApplyExcel(path, GetSystems());

                this.Do(() =>
                {
                    DisplayTextModel(Color.FromArgb(0, 150, 0), SelectedSystem.ToDsText());
                    richTextBox_ds.ScrollToCaret();
                    button_copy.Visible = true;

                    MSGInfo($"{_PathXLS} 적용완료!!");
                    MSGWarn($"파워포인트와 엑셀을 동시에 가져오면 IO 매칭된 설정값을 가져올수 있습니다.!!");
                });
            }

            catch (Exception ex)
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.MsgError, ex.Message);
            }
            finally
            {
                Busy = false;
            }

        }

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


        internal void ExportExcel()
        {
            if (UtilFile.BusyCheck()) return;
            Busy = true;
            ProcessEvent.DoWork(10);

            button_copy.Visible = false;
            button_CreateExcel.Enabled = false;
            _PathXLS = UtilFile.GetNewPath(_PathPPTs);
            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{_PathXLS} 생성시작!!");

            Directory.CreateDirectory(Path.GetDirectoryName(_PathXLS));

            List<DsSystem> systems = new List<DsSystem>();
            foreach (SystemView systemView in comboBox_System.Items)
                systems.Add(systemView.System);

            ExportIOTable.ToFiie(systems, _PathXLS);

            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{_PathXLS} 생성완료!!");
            this.Do(() =>
            {
                button_CreateExcel.Enabled = true;
                button_OpenFolder.Visible = true;
            });
            Process.Start($"{_PathXLS}");
            FileWatcher.CreateFileWatcher();
            Busy = false;
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
                InitModel(_PathPPTs);
        }
        internal void TestDebug()
        {
            //F7
            //T1_System
            //T2_Flow
            //T3_Real
            //T4_Api
            //T5_Call
            //T6_Alias
            //T7_CopySystem
            //T8_Safety
            //T9_Group
            //T10_Button
            //T11_SubLoading
            string path = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\")) + "src\\UnitTest\\UnitTest.Engine\\ImportOffice\\Sample\\s.pptx";
            bool debug = File.Exists(path);
            if (debug)
            {
                _PathPPTs.Clear();
                _PathPPTs.Add(path);
                InitModel(_PathPPTs);
            }
        }


    }
}
