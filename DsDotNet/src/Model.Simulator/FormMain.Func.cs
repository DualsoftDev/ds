using Engine.Common;
using Engine.Common.FS;
using Engine.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static Engine.Common.FS.ProcessEvent;

namespace Model.Simulator
{
    public partial class FormMain : Form
    {
        internal void ExportTextModel(StringBuilder stb)
        {
            var textLines = stb.ToString().Split('\n');
            Random r = new Random();
            Color rndColor = Color.LightGoldenrodYellow;
            int lineCnt = textLines.Count();
            int lineCur = 0;
            textLines.ToList().ForEach(f =>
            {
                int pro = 50 + Convert.ToInt32(Convert.ToSingle(lineCur++) / (lineCnt) * 50f);
                    if (f.Contains("[sys]") || (f.Contains("[flow]") && !f.Contains("}"))  //[flow] F = {} 한줄제외
                    || f.Contains("[address]") || f.Contains("[layouts]") || f.Contains("//"))
                    {
                        rndColor = Color.FromArgb(r.Next(130, 230), r.Next(130, 230), r.Next(130, 230));
                        this.Do(() => richTextBox_ds.ScrollToCaret());
                        DoWork(pro);
                    }
                    this.Do(() => richTextBox_ds.AppendTextColor(f, rndColor));
            
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
                    color = Color.Red;
                    richTextBox_ds.AppendTextColor($"\r\n{msg}", color);
                    DoWork(0);
                }
                if (level.IsWarn) color = Color.Purple;
                richTextBox_Debug.AppendTextColor($"\r\n{time} : {msg}", color);
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

        internal void CreateNewTabViewer(DsSystem sys, bool isDemo = false)
        {
            List<Flow> flows = new List<Flow>();
            //if (isDemo)
            //    flows = sys.Flows.Values.ToList();
            //else
            //    flows = sys.RootFlow().ToList();

            var flowTotalCnt = flows.Count();
            flows.ToList().ForEach(f =>
            {
                if (DicUI.ContainsKey(f))
                    xtraTabControl_My.SelectedTab = DicUI[f];
                else
                {
                    UCView viewer = new UCView { Dock = DockStyle.Fill };
                //    viewer.SetGraph(f);
                    TabPage tab = new TabPage();
                    tab.Controls.Add(viewer);
                    tab.Tag = viewer;
                  //  tab.Text = $"{f.Name}({f.Page})";
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

                ((UCView)view.Value.Tag).RefreshGraph();
            }
        }
        internal void ReLoadText()
        {
            if (File.Exists(dsTextPath))
                LoadText(dsTextPath);
        }
        internal void TestDebug()
        {
            string path = @"D:\DS\test\DS.ds";
            bool debug = File.Exists(path);
            if (debug)
            {
                dsTextPath = path;
                LoadText(path);
            }
        }
    }
}