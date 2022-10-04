using Engine.Common;
using Engine.Common.FS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static Engine.Core.CoreModule;

namespace Model.Simulator
{
    public partial class FormMain : Form
    {
        internal void ExportTextModel(string dsText, bool bShowLine = false)
        {

            this.Do(() => richTextBox_ds.Clear());

            var textLines = dsText.Split('\n');
            Color rndColor = Color.Black;

            this.Do(() =>
            {
                int lineCur = 0;
                textLines.ToList().ForEach(f =>
                {
                    if (bShowLine) richTextBox_ds.AppendText((lineCur++).ToString("000") + ";");

                    richTextBox_ds.AppendText(f + "\n");
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

        internal void CreateNewTabViewer(DsSystem sys)
        {

            var flows = sys.Flows;
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