using Engine.Base;
using Engine.Common;
using Engine.Common.FS;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Engine.Common.FS.MessageEvent;
using static Model.Import.Office.Event;
using static Model.Import.Office.Model;
using static Model.Import.Office.Object;

namespace Dual.Model.Import
{
    public partial class FormMain : Form
    {
        internal void ExportTextModel(Color color, string dsText)
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
                    richTextBox_ds.AppendText(lineCur.ToString("000"));

                    if (color == Color.Transparent)
                    {
                        if (f.Contains($"[{DsText.TextSystem}]") || (f.Contains($"[{DsText.TextFlow}]") && !f.Contains("}"))  //[flow] F = {} 한줄제외
                        || f.Contains($"[{DsText.TextAddress}]") || f.Contains($"[{DsText.TextLayout}]") || f.Contains("//"))
                        {
                            rndColor = Color.FromArgb(r.Next(130, 230), r.Next(130, 230), r.Next(130, 230));
                            this.Do(() => richTextBox_ds.ScrollToCaret());
                            ProcessEvent.DoWork(pro);
                        }
                        richTextBox_ds.AppendTextColor(f, rndColor);
                    }
                    else
                        richTextBox_ds.AppendTextColor(f, color);
                });
            });

            this.Do(() => richTextBox_ds.Select(0, 0));
            this.Do(() => richTextBox_ds.ScrollToCaret());
        }
        internal void ImportPPT()
        {
            try
            {
                this.Do(() => button_comfile.Enabled = false);
                var lstModel = new List<DsModel>() { ImportModel.FromPPTX(PathPPT) };
                if (lstModel.Where(w => w == null).Any())
                    return;

                _model = lstModel.First();

                if (!_ConvertErr)
                {
                    _dsText = ExportModel.ToText(_model);
                    ExportTextModel(Color.Transparent, _dsText);
                    this.Do(() => xtraTabControl_My.TabPages.Clear());
                    foreach (var sys in _model.TotalSystems.OrderBy(sys => sys.Name))
                        CreateNewTabViewer(sys);
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
            if (UtilFile.BusyCheck()) return;
            Busy = true;
            MSGInfo($"{PathXLS} 불러오는 중!!");
            var sys = _model.ActiveSys;
            ImportIOTable.ApplyExcel(path, sys);
            _dsText = ExportModel.ToText(_model);
            ExportTextModel(Color.FromArgb(0, 150, 0), _dsText);
            this.Do(() =>
            {
                richTextBox_ds.ScrollToCaret();
                button_copy.Visible = true;

                MSGInfo($"{PathXLS} 적용완료!!");
                MSGWarn($"파워포인트와 엑셀을 동시에 가져오면 IO 매칭된 설정값을 가져올수 있습니다.!!");

            });
            Busy = false;

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
            ExportIOTable.ToFiie(_model, PathXLS);

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

        internal void HelpLoad()
        {
            DsModel demo = Check.GetDemoModel("test");
            splitContainer1.Panel1Collapsed = false;

            this.Size = new Size(1600, 1000);

            demo.TotalSystems.OrderBy(sys => sys.Name).ToList()
                  .ForEach(sys =>
                      CreateNewTabViewer(sys, true)
                  );
        }

        internal void CreateNewTabViewer(DsSystem sys, bool isDemo = false)
        {
            List<Flo> flows = new List<Flo>();
            if (isDemo)
                flows = sys.Flows.Values.ToList();
            else
                flows = sys.RootFlow().ToList();

            var flowTotalCnt = flows.Count();
            flows.ToList().ForEach(f =>
            {
                if (DicUI.ContainsKey(f))
                    xtraTabControl_My.SelectedTab = DicUI[f];
                else
                {
                    UCView viewer = new UCView { Dock = DockStyle.Fill };
                    viewer.SetGraph(f);
                    TabPage tab = new TabPage();
                    tab.Controls.Add(viewer);
                    tab.Tag = viewer;
                    tab.Text = $"{f.Name}({f.Page})";
                    this.Do(() =>
                    {
                        xtraTabControl_My.TabPages.Add(tab);
                        xtraTabControl_My.SelectedTab = tab;

                        DicUI.Add(f, tab);
                    });
                }
            });
        }
        internal void RefreshGraph()
        {
            foreach (KeyValuePair<Flo, TabPage> view in DicUI)
            {
                foreach (var seg in view.Key.UsedSegs)
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
            string path = @"D:\DS\test\DS.pptx";
            bool debug = File.Exists(path);
            if (debug)
            {
                PathPPT = path;
                InitModel(path);
            }
        }



    }
}
