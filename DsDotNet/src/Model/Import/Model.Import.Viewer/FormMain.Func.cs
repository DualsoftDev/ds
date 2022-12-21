using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Spreadsheet;
using Engine.CodeGenCPU;
using Engine.Common;
using Engine.Common.FS;
using Engine.Core;
using Microsoft.FSharp.Collections;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using static Engine.CodeGenCPU.VertexMemoryManagerModule;
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Engine.Core.DsTextProperty;
using static Engine.Core.ExpressionForwardDeclModule;
using static Engine.Core.ExpressionModule;
using static Engine.Core.Interface;
using static Engine.Cpu.RunTime;
using static Model.Import.Office.ViewModule;
using Color = System.Drawing.Color;

namespace Dual.Model.Import
{

    public partial class FormMain : Form
    {
      
        //복수 Active system ppt 불러오기 
        internal void ImportPPT(List<string> paths)
        {
            try
            {
                var results = ImportM.FromPPTXS(paths);
                var model = results.Item1;
                var views = results.Item2;

                model.Systems.ForEach(system =>
                {
                    var rungs = CpuLoader.LoadStatements(system);
                    var storages = new Dictionary<string, IStorage>();
                    var cpu = new DsCPU(storages, "", rungs.Select(s => s.Item2));
                    cpu.Run();
                    _DicVertex = new Dictionary<Vertex, ViewNode>();
                    system.GetVertices()
                        .ForEach(v =>
                        {
                            var viewNode = views[system].SelectMany(s => s.UsedViewNodes)
                                                        .Where(w => w.CoreVertex != null)
                                                        .First(w => w.CoreVertex.Value == v);

                            _DicVertex.Add(v, viewNode);
                            if (v is Real)
                            {
                                comboBox_Segment.Items
                                .Add(new SegmentHMI { Display = v.QualifiedName, Vertex = v, ViewNode = viewNode, VertexM = v.VertexMemoryManager as VertexMemoryManager });
                            }
                        });

                    var text = rungs.Select(rung =>
                    {
                        var description = rung.Item1;
                        var statement = rung.Item2;
                        return $"***{description}***\t{rung.Item2.ToText().Replace("%", " ")}";
                    });

                    UpdateCpuUI(text);
                    UpdateGraphUI(views[system]);

                    ExportTextModel(Color.Transparent, system.ToDsText());
                    ExportTextExpr(cpu.ToTextStatement(), Color.WhiteSmoke);
                });

                model.Config.DsFilePaths.ForEach(f =>
                {
                    WriteDebugMsg(DateTime.Now, MSGLevel.MsgWarn, $"{f} 불러오기 성공!!");
                });

                ProcessEvent.DoWork(0);
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
        private void UpdateCpuUI(IEnumerable<string> text)
        {
            StartResetBtnUpdate(true);
            comboBox_Segment.Items.Clear();

            comboBox_Segment.DisplayMember = "Display";

            if (comboBox_Segment.Items.Count > 0)
                comboBox_Segment.SelectedIndex = 0;


            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"\r\n{text.JoinWith("\n")}");
        }
        internal void ExportTextExpr(string dsExpr, Color color)
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

        internal void ExportTextModel(Color color, string dsText, bool bShowLine = false)
        {

            //this.Do(() => richTextBox_ds.Clear());

            var textLines = dsText.Split('\n');
            Random r = new Random();
            Color rndColor = Color.LightGoldenrodYellow;
            int lineCnt = textLines.Count();
            int lineCur = 0;
            this.Do(() =>
            {
                textLines.ToList().ForEach(f =>
                {
                    int pro = 50 + Convert.ToInt32(Convert.ToSingle(lineCur++) / (lineCnt) * 50f);
                    if (bShowLine) richTextBox_ds.AppendText(lineCur.ToString("000") + ";");

                    if (color == Color.Transparent)
                    {
                        if (f.StartsWith($"[{TextSystem}") || (f.Contains($"[{TextFlow}]"))  //[flow] F = {} 한줄제외
                        || f.Contains($"[{TextAddress}]") || f.Contains($"[{TextLayout}]"))
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

        internal void ImportExcel(string path)
        {
            try
            {

                if (UtilFile.BusyCheck()) return;
                Busy = true;
                MSGInfo($"{_PathXLS} 불러오는 중!!");
                ImportIOTable.ApplyExcel(path, _mySystem);
                _dsText = _mySystem.ToDsText();
                ExportTextModel(Color.FromArgb(0, 150, 0), _dsText);
                this.Do(() =>
                {
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

        internal void ExportExcel()
        {
            if (UtilFile.BusyCheck()) return;
            Busy = true;
            ProcessEvent.DoWork(10);

            button_copy.Visible = false;
            button_CreateExcel.Enabled = false;
            _PathXLS = UtilFile.GetNewPath(_PathPPTs.First());
            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{_PathXLS} 생성시작!!");

            Directory.CreateDirectory(Path.GetDirectoryName(_PathXLS));
            ExportIOTable.ToFiie(_mySystem, _PathXLS);

            WriteDebugMsg(DateTime.Now, MSGLevel.MsgInfo, $"{_PathXLS} 생성완료!!");
            this.Do(() =>
            {
                button_CreateExcel.Enabled = true;
                button_OpenFolder.Visible = true;
            });
            Process.Start($"{_PathXLS}");

            FileWatcher.CreateFileWatcher();
            Busy = false;
        }


        internal void WriteDebugMsg(DateTime time, MSGLevel level, string msg)
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

                richTextBox_Debug.ScrollToCaret();
            });
        }
       // CoreModule.Model _Demo = new CoreModule.Model();
        internal void HelpLoad()
        {
            splitContainer1.Panel1Collapsed = false;

            this.Size = new Size(1600, 1000);
            //if (_Demo.Systems.Count == 0)
            //    _Demo = ImportCheck.GetDemoModel("test");

            //_Demo.Systems.OrderBy(sys => sys.Name).ToList()
            //      .ForEach(sys =>
            //          CreateNewTabViewer(sys, null)
            //      );
        }

        internal void UpdateGraphUI(IEnumerable<ViewNode> lstViewNode)
        {
            lstViewNode.ForEach(f =>
            {
                var flow = f.Flow.Value;
                if (_DicMyUI.ContainsKey(flow) || _DicExUI.ContainsKey(flow))
                {
                    xtraTabControl_My.SelectedTab = _DicMyUI[flow];
                }
                else
                {
                    UCView viewer = new UCView { Dock = DockStyle.Fill };
                    viewer.SetGraph(f);
                    TabPage tab = new TabPage();
                    tab.Controls.Add(viewer);
                    tab.Tag = viewer;
                    tab.Text = $"{flow.System.Name}.{flow.Name}({f.Page})";
                    this.Do(() =>
                    {
                            xtraTabControl_My.TabPages.Add(tab);
                            xtraTabControl_My.SelectedTab = tab;
                            _DicMyUI.Add(flow, tab);
                        
                    });
                }
            });
        }
        internal void RefreshGraph()
        {
            foreach (KeyValuePair<Flow, TabPage> view in _DicMyUI)
            {
                ((UCView)view.Value.Tag).RefreshGraph();
            }
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
            string path = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\")) + "src\\UnitTest\\UnitTest.Engine\\ImportOffice\\Sample\\TEST.pptx";
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
