using DevExpress.XtraEditors.Controls;
using Dual.Common.Core;
using Dual.Common.Winform;
using log4net.Appender;
using log4net.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DSModeler
{
    [SupportedOSPlatform("windows")]
    public partial class UcLog
        : UserControl
        , IAppender
    {
        public bool TrackEndOfLine { get; set; } = true;
        public ListBoxItemCollection Items => listBoxControlOutput.Items;
        public int SelectedIndex { get => listBoxControlOutput.SelectedIndex; set => listBoxControlOutput.SelectedIndex = value; }

        private class LogItem
        {
            public Color ItemColor { get; set; }
            public string Message { get; set; }
            public LogItem(string msg, Color clr)
            {
                ItemColor = clr;
                Message = msg;
            }
        }

        public UcLog()
        {
            InitializeComponent();
        }
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UcLog));
        Image copyImg => resources.GetObject("copyBtn.ImageOptions.Image") as Image;
        Image clearImg => resources.GetObject("clearBtn.ImageOptions.Image") as Image;
        Image clearAllImg => resources.GetObject("copyAllBtn.ImageOptions.Image") as Image;
        Image logLevelBtnImg => resources.GetObject("logLevelBtn.ImageOptions.Image") as Image;
        Image logLevelChkImg => resources.GetObject("ImgChk.ImageOptions.Image") as Image;




        private void UcLog_Load(object sender, EventArgs args)
        {
            this.Dock = DockStyle.Fill;
        }

        public void InitLoad()
        {
            listBoxControlOutput.Dock = DockStyle.Fill;
            listBoxControlOutput.AllowHtmlDraw = DevExpress.Utils.DefaultBoolean.True;

            var items = listBoxControlOutput.ContextMenuStrip.Items;
            items.Add(new ToolStripMenuItem("Clear", clearImg, (o, a) =>
            {
                listBoxControlOutput.Items.Clear();
                listBoxControlOutput.SelectedIndex = 0;
            }));

            items.Add(new ToolStripMenuItem("Copy all", clearAllImg, (o, a) =>
            {
                var strings =
                    from n in Enumerable.Range(0, listBoxControlOutput.Items.Count)
                    let t = listBoxControlOutput.Items[n].ToString()
                    select Regex.Replace(t, "<.*?>", "")
                    ;

                var text = String.Join("\r\n", strings);
                if (!text.IsNullOrEmpty())
                {
                    Clipboard.Clear();
                    Clipboard.SetText(text);
                }
            }));

            items.Add(new ToolStripMenuItem("Copy selected", copyImg, (o, a) =>
            {
                var strings =
                    from item in listBoxControlOutput.SelectedItems
                    let str = item.ToString()
                    select Regex.Replace(str, "<.*?>", "")
                ;

                var text = String.Join("\r\n", strings);
                if (!text.IsNullOrEmpty())
                {
                    Clipboard.Clear();
                    Clipboard.SetText(text);
                }
            }));

            var logger = ((log4net.Repository.Hierarchy.Logger)Log4NetLogger.Logger.Logger);
            logger.Level = Level.All;

            ToolStripMenuItem log_menu = new ToolStripMenuItem("Log Level", logLevelBtnImg);
            var logItemError = new ToolStripMenuItem("Error", null);
            var logItemWarn = new ToolStripMenuItem("Warn", null);
            var logItemInfo = new ToolStripMenuItem("Info", null);
            var logItemDebug = new ToolStripMenuItem("Debug", null);
            var logItemAll = new ToolStripMenuItem("All", null);
            logItemAll.Image = logLevelChkImg;

            var logLvlControls = new List<ToolStripMenuItem>()
            {
                logItemError
                    ,logItemWarn
                    ,logItemInfo
                    ,logItemDebug
                    ,logItemAll
            };

            logLvlControls.Iter(i => i.Click += (ss, ee) =>
            {
                var tool = (ToolStripMenuItem)ss;
                Level sellvl = getLevel(tool.Text);
                logLvlControls.Iter(c =>
                {
                    c.Checked = false;
                    c.Image = null;
                });
                tool.Checked = true;
                tool.Image = logLevelChkImg;

                logger.Level = sellvl;

                Level getLevel(string lvl)
                {
                    Level l;
                    switch (lvl.ToUpper())
                    {
                        case "ERROR": l = Level.Error; break;
                        case "WARN": l = Level.Warn; break;
                        case "INFO": l = Level.Info; break;
                        case "DEBUG": l = Level.Debug; break;
                        case "ALL": l = Level.All; break;
                        default: l = Level.All; break;
                    }
                    return l;
                }
            });
            log_menu.DropDownItems.AddRange(logLvlControls.ToArray());
            items.Add(log_menu);


            listBoxControlOutput.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    listBoxControlOutput.ContextMenuStrip.Show();
            };

            listBoxControlOutput.DrawItem += (s, e) =>
            {
                Brush selectedItemBackBrush = Brushes.SteelBlue;
                string itemText = this.listBoxControlOutput.GetItemText(e.Index);
                if ((e.State & DrawItemState.Selected) != 0)
                {
                    e.Cache.FillRectangle(selectedItemBackBrush, e.Bounds);
                    using (Font f = new Font(e.Appearance.Font.Name, e.Appearance.Font.Size, FontStyle.Bold))
                    {
                        var m = Regex.Match(itemText, @"(?<=\<.+\>)(.*?)(?=<\/.+>)");
                        e.Cache.DrawString(m.Value, f, Brushes.White, e.Bounds, e.Appearance.GetStringFormat());
                    }
                    e.Handled = true;
                }
            };

        }

        public static Color GetLogLevelColor(string levelName)
        {
            switch (levelName)
            {
                case "DEBUG": return Color.Chocolate;
                case "INFO": return Color.Navy;
                case "ERROR": return Color.Red;
                case "WARN": return Color.Brown;
                default: return Color.Black;
            }
        }

        public void Close() { }

        public async void DoAppend(LoggingEvent logEntry)
        {
            await this.DoAsync(tcs =>
            {
                try
                {
                    var msg = logEntry.MessageObject.ToString();
                    var level = logEntry.Level.Name;
                    var cr = GetLogLevelColor(level).Name;
                    var now = logEntry.TimeStamp.ToString("HH:mm:ss.fff");
                    //Trace.WriteLine(msg);
                    /*
                     * multi-line message 처리
                     */
                    var lines = msg.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                    if (lines.Length > 0)
                    {
                        var fmtMsg = string.Format($"<color={cr}>{now} [{level}]:{lines[0]}</color>");
                        Items.Add(fmtMsg);


                        for (int i = 1; i < lines.Length; i++)
                        {
                            fmtMsg = $"<color={cr}>    {lines[i]}</color>";
                            Items.Add(fmtMsg);
                        }

                        if (TrackEndOfLine)
                            SelectedIndex = Items.Count - 1;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failed to append log: {ex}");
                    //Logger.Error($"Failed to append log: {ex}");
                }

                tcs.SetResult(true);
            });

        }


    }
}
