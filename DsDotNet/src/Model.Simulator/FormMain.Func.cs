using Engine.Core;
using Engine.Common;
using Engine.Common.FS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Model.Import.Office.Object;

namespace Model.Simulator
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
                    if (bShowLine) richTextBox_ds.AppendTextColor(lineCur.ToString("000") + ";", Color.White);

                    if (color == Color.Transparent)
                    {
                        if (f.Contains($"[{DsText.TextSystem}]") || (f.Contains($"[{DsText.TextFlow}]") && !f.Contains("}"))  //[flow] F = {} 한줄제외
                        || f.Contains($"[{DsText.TextAddress}]") || f.Contains($"[{DsText.TextLayout}]") || f.Contains("//"))
                        {
                            rndColor = Color.FromArgb(r.Next(0, 100), r.Next(0, 100), r.Next(0, 100));
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
        }

        internal void WriteDebugMsg(DateTime time, MessageEvent.MSGLevel level, string msg)
        {
            this.Do(() =>
            {
                var color = Color.Black;
                if (level.IsError)
                {
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
            //DsModel demo = Check.GetDemoModel("test");
            //splitContainer1.Panel1Collapsed = false;

            //this.Size = new Size(1600, 1000);

            //demo.TotalSystems.OrderBy(sys => sys.Name).ToList()
            //    .ForEach(sys =>
            //        CreateNewTabViewer(sys, true)
            //    );
        }

        internal void CreateNewTabViewer(DsSys sys)
        {

            var flows = sys.RootFlows;
            var flowTotalCnt = flows.Count();
            flows.ToList().ForEach(f =>
            {
                if (DicUI.ContainsKey(f))
                    xtraTabControl_My.SelectedTab = DicUI[f];
                else
                {
                    UCSim viewer = new UCSim { Dock = DockStyle.Fill };
                    viewer.SetGraph(f);
                    TabPage tab = new TabPage();
                    tab.Controls.Add(viewer);
                    tab.Tag = viewer;
                    tab.Text = $"[{f.System.Name}]{f.Name}";
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
            foreach (KeyValuePair<Flow, TabPage> view in DicUI)
            {
                //  foreach (var seg in view.Key.UsedSegs)
                //  {
                ////      ((UCView)view.Value.Tag).Update(seg);
                //  }

                ((UCSim)view.Value.Tag).RefreshGraph();
            }
        }
        internal void ReLoadText()
        {
            if (File.Exists(_dsTextPath))
                LoadText(_dsTextPath);
        }
        internal void TestDebug()
        {
            string path = @"D:\DS\test\DS.ds";
            bool debug = File.Exists(path);
            if (debug)
            {
                LoadText(path);
            }
        }
    }
}