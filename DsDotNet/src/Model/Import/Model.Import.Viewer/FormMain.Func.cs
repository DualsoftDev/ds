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
using static Engine.Common.FS.MessageEvent;
using static Engine.Core.CoreModule;
using static Engine.Core.DsTextProperty;

namespace Dual.Model.Import
{
    public partial class FormMain : Form
    {
        internal void ExportTextModel(Color color, string dsText, bool bShowLine = false)
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
                    int pro = 50 + Convert.ToInt32(Convert.ToSingle(lineCur++) / (lineCnt) * 50f);
                    if (bShowLine) richTextBox_ds.AppendText(lineCur.ToString("000") + ";");

                    if (color == Color.Transparent)
                    {
                        if (f.StartsWith($"[{TextSystem}") || (f.Contains($"[{TextFlow}]"))  //[flow] F = {} 한줄제외
                        || f.Contains($"[{TextAddress}]") || f.Contains($"[{TextLayout}]") )
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
        internal void ImportPPT()
        {
            try
            {
                this.Do(() => button_comfile.Enabled = false);
                var result = ImportM.FromPPTX(PathPPT);
              
                _Model = result.Item1;
                var dicFlow = result.Item2;
                if (!_ConvertErr)
                {
                    _dsText = _Model.ToDsText();
                    ExportTextModel(Color.Transparent, _dsText);
                    this.Do(() => xtraTabControl_Ex.TabPages.Clear());

                    foreach (var sys in _Model.Systems.OrderBy(sys => sys.Name))
                        CreateNewTabViewer(sys, dicFlow);

                    WriteDebugMsg(DateTime.Now, MSGLevel.Info, $"{PathPPT} 불러오기 성공!!");
                    this.Do(() =>
                    {
                        button_CreateExcel.Visible = true;
                        pictureBox_xls.Visible = true;
                        button_TestORG.Visible = true;
                        button_TestStart.Visible = true;
                        button_copy.Visible = false;
                    });

                    ProcessEvent.DoWork(0);
                }
                else
                    WriteDebugMsg(DateTime.Now, MSGLevel.Error, $"{PathPPT} 불러오기 실패!!");

                this.Do(() => button_comfile.Enabled = true);

            }
            catch (Exception ex)
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.Error, ex.Message);
            }
            finally
            {
                Busy = false;
            }
        }
        internal void ImportExcel(string path)
        {
            try
            {

                if (UtilFile.BusyCheck()) return;
                Busy = true;
                MSGInfo($"{PathXLS} 불러오는 중!!");
                //ImportIOTable.ApplyExcel(path, _OldModel.ActiveSys);
                //_dsText = ExportM.ToText(_model);
                ExportTextModel(Color.FromArgb(0, 150, 0), _dsText);
                this.Do(() =>
                {
                    richTextBox_ds.ScrollToCaret();
                    button_copy.Visible = true;

                    MSGInfo($"{PathXLS} 적용완료!!");
                    MSGWarn($"파워포인트와 엑셀을 동시에 가져오면 IO 매칭된 설정값을 가져올수 있습니다.!!");

                });
            }

            catch (Exception ex)
            {
                WriteDebugMsg(DateTime.Now, MSGLevel.Error, ex.Message);
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
            PathXLS = UtilFile.GetNewPath(PathPPT);
            WriteDebugMsg(DateTime.Now, MSGLevel.Info, $"{PathXLS} 생성시작!!");

            Directory.CreateDirectory(Path.GetDirectoryName(PathXLS));
            //ExportIOTable.ToFiie(_OldModel, PathXLS);

            WriteDebugMsg(DateTime.Now, MSGLevel.Info, $"{PathXLS} 생성완료!!");
            this.Do(() =>
            {
                button_CreateExcel.Enabled = true;
                button_OpenFolder.Visible = true;
            });
            Process.Start($"{PathXLS}");

            FileWatcher.CreateFileWatcher();
            Busy = false;
        }


        internal void WriteDebugMsg(DateTime time, MSGLevel level, string msg)
        {
            this.Do(() =>
            {
                var color = Color.Black;
                if (level.IsError)
                {
                    _ConvertErr = true;
                    richTextBox_Debug.AppendTextColor($"\r\n{msg}", Color.Red);
                    ProcessEvent.DoWork(0);
                }
                else
                {
                    if (level.IsWarn) color = Color.Purple;
                    richTextBox_Debug.AppendTextColor($"\r\n{time} : {msg}", color);
                }

                richTextBox_Debug.ScrollToCaret();
            });
        }
        CoreModule.Model _Demo = new CoreModule.Model();
        internal void HelpLoad()
        {
            splitContainer1.Panel1Collapsed = false;

            this.Size = new Size(1600, 1000);
            if (_Demo.Systems.Count == 0)
                _Demo = ImportCheck.GetDemoModel("test");

            _Demo.Systems.OrderBy(sys => sys.Name).ToList()
                  .ForEach(sys =>
                      CreateNewTabViewer(sys, null)
                  );
        }

        internal void CreateNewTabViewer(DsSystem sys, Dictionary<int, Flow> dicFlow)
        {
            //List<MFlow> flows = sys.Flows.Cast<MFlow>().OrderBy(o => o.Page).ToList();

            sys.Flows.ToList().ForEach(f =>
            {
                if (_DicMyUI.ContainsKey(f) || _DicExUI.ContainsKey(f))
                {
                    if (f.System.Active)
                        xtraTabControl_My.SelectedTab = _DicMyUI[f];
                    else
                        xtraTabControl_Ex.SelectedTab = _DicExUI[f];
                }
                else
                {
                    int pageNum = 0;
                    if(dicFlow != null)
                        pageNum = dicFlow.First(flow => flow.Value.System.Name == sys.Name 
                                                     && flow.Value.Name == f.Name).Key;

                    UCView viewer = new UCView { Dock = DockStyle.Fill };
                    viewer.SetGraph(f, sys);
                    TabPage tab = new TabPage();
                    tab.Controls.Add(viewer);
                    tab.Tag = viewer;
                    tab.Text = $"{f.System.Name}.{f.Name}({(pageNum > 0 ? pageNum: 0)})";
                    this.Do(() =>
                    {
                        if (f.System.Active)
                        {
                            xtraTabControl_My.TabPages.Add(tab);
                            xtraTabControl_My.SelectedTab = tab;
                            _DicMyUI.Add(f, tab);
                        }
                        else
                        {
                            xtraTabControl_Ex.TabPages.Add(tab);
                            xtraTabControl_Ex.SelectedTab = tab;
                            _DicExUI.Add(f, tab);
                        }
                    });
                }
            });
        }
        internal void RefreshGraph()
        {
            foreach (KeyValuePair<Flow, TabPage> view in _DicMyUI)
            {
                foreach (var seg in view.Key.Graph.Vertices)
                {
                    ((UCView)view.Value.Tag).Update(seg);
                }

                ((UCView)view.Value.Tag).RefreshGraph();
            }
        }
        internal void ReloadPPT()
        {
            if (File.Exists(PathPPT))
                InitModel(PathPPT);
        }
        internal void TestDebug()
        {

            string path = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\..\..\"))+ "Model.Import.Office\\sample\\DS.pptx";
            bool debug = File.Exists(path);
            if (debug)
            {
                PathPPT = path;
                InitModel(path);
            }
        }

        internal void TestUnitTest()
        {

            //0_CaseAll
            //1_System
            //2_Flow
            //3_Real
            //4_Api
            //5_Call
            //6_Alias
            //7_CopySystem
            //8_Safety
            string path = Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\..\..\..\..\"))+ "UnitTest\\UnitTest.Engine\\ImportPPT\\T9_GroupEdge.pptx";
            bool debug = File.Exists(path);
            if (debug)
            {
                PathPPT = path;
                InitModel(path);
            }
        }

    }
}
