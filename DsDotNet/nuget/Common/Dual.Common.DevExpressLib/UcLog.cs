using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using log4net.Appender;
using log4net.Core;
using DevExpress.XtraEditors.Controls;
using Images = Dual.Common.Resources.Images;
using Dual.Common.Winform;

using LoggerT = log4net.Repository.Hierarchy.Logger;

namespace Dual.Common.DevExpressLib
{
    public partial class UcLog
        : UserControl
        , IAppender
    {
        LoggerT _logger;
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

        /// <summary>
        /// e.g  var logger = ((log4net.Repository.Hierarchy.Logger)Log4NetLogger.Logger.Logger);
        /// </summary>
        public UcLog(LoggerT logger)
        {
            InitializeComponent();
            _logger = logger;
        }

        private void UcLog_Load(object sender, EventArgs args)
        {
            listBoxControlOutput.Dock = DockStyle.Fill;
            listBoxControlOutput.AllowHtmlDraw = DevExpress.Utils.DefaultBoolean.True;

            var items = listBoxControlOutput.ContextMenuStrip.Items;
            items.Add(new ToolStripMenuItem("Clear", Images.Clear, (o, a) =>
            {
                listBoxControlOutput.Items.Clear();
                listBoxControlOutput.SelectedIndex = 0;
            }));

            items.Add(new ToolStripMenuItem("Copy all", Images.Copy, (o, a) =>
            {
                var strings =
                    from n in Enumerable.Range(0, listBoxControlOutput.Items.Count)
                    let t = listBoxControlOutput.Items[n].ToString()
                    select Regex.Replace(t, "<.*?>", "")
                    ;

                var text = String.Join("\r\n", strings);
                Clipboard.SetText(text);
            }));

            items.Add(new ToolStripMenuItem("Copy selected", Images.Copy, (o, a) =>
            {
                var strings =
                    from item in listBoxControlOutput.SelectedItems
                    let str = item.ToString()
                    select Regex.Replace(str, "<.*?>", "")
                ;

                var text = String.Join("\r\n", strings);
                Clipboard.SetText(text);
            }));

            listBoxControlOutput.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Right)
                    listBoxControlOutput.ContextMenuStrip.Show();
            };

            // Set Logger Level
            if (_logger != null)
            {
                ToolStripMenuItem log_menu =
                    new ToolStripMenuItem("Log Level", Images.Copy, (o, a) =>
                    {
                        var strings =
                            from item in listBoxControlOutput.SelectedItems
                            let str = item.ToString()
                            select Regex.Replace(str, "<.*?>", "")
                        ;

                        var text = String.Join("\r\n", strings);
                        if (text != null && text.Any())
                            Clipboard.SetText(text);
                    });

                var logItemError = new ToolStripMenuItem("Error", null, (o, a) => _logger.Level = Level.Error);
                var logItemWarn = new ToolStripMenuItem("Warn", null, (o, a) => _logger.Level = Level.Warn);
                var logItemInfo = new ToolStripMenuItem("Info", null, (o, a) => _logger.Level = Level.Info);
                var logItemDebug = new ToolStripMenuItem("Debug", null, (o, a) => _logger.Level = Level.Debug);

                log_menu.DropDownItems.Add(logItemError);
                log_menu.DropDownItems.Add(logItemWarn);
                log_menu.DropDownItems.Add(logItemInfo);
                log_menu.DropDownItems.Add(logItemDebug);
                items.Add(log_menu);
                var level = _logger.Level;
                var logItem = level.ToString() switch
                {
                    "ERROR" => logItemError,
                    "WARN" => logItemWarn,
                    "INFO" => logItemInfo,
                    "DEBUG" => logItemDebug,
                    _ => logItemDebug
                };
                logItem.CheckState = CheckState.Checked;
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
                        var fmtMsg = string.Format($"<color={cr}>{now} [{level}]: {lines[0]}</color>");
                        Items.Add(fmtMsg);

                        for (int i = 1; i < lines.Length; i++)
                        {
                            fmtMsg = $"<color={cr}>    {lines[i]}</color>";
                            Items.Add(fmtMsg);
                        }

                        SelectedIndex = Items.Count - 1;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Failed to append log: {ex}");
                    //Logger.Error($"Failed to append log: {ex}");
                }
                finally
                {
                    tcs.SetResult(true);
                }
            });

            Color GetLogLevelColor(string levelName)
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
        }
    }
}
