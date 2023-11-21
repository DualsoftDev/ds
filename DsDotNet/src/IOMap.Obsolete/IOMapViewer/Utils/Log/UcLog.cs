using Dual.Common.Core;
using Dual.Common.Winform;
using log4net.Appender;
using log4net.Core;
using System.Data;
using System.Text.RegularExpressions;
using Clipboard = System.Windows.Forms.Clipboard;
using FontStyle = System.Drawing.FontStyle;

namespace IOMapViewer;

[SupportedOSPlatform("windows")]
public partial class UcLog
    : UserControl
        , IAppender
{
    public bool TrackEndOfLine { get; set; } = true;
    public ListBoxItemCollection Items => listBoxControlOutput.Items;

    public int SelectedIndex
    {
        get => listBoxControlOutput.SelectedIndex;
        set => listBoxControlOutput.SelectedIndex = value;
    }

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

    private readonly System.ComponentModel.ComponentResourceManager resources = new(typeof(UcLog));

    private Image copyImg => resources.GetObject("copyBtn.ImageOptions.Image") as Image;

    private Image clearImg => resources.GetObject("clearBtn.ImageOptions.Image") as Image;

    private Image clearAllImg => resources.GetObject("copyAllBtn.ImageOptions.Image") as Image;

    private Image logLevelBtnImg => resources.GetObject("logLevelBtn.ImageOptions.Image") as Image;

    private Image logLevelChkImg => resources.GetObject("ImgChk.ImageOptions.Image") as Image;


    private void UcLog_Load(object sender, EventArgs args)
    {
        Dock = DockStyle.Fill;
    }

    public void InitLoad()
    {
        listBoxControlOutput.Dock = DockStyle.Fill;
        listBoxControlOutput.AllowHtmlDraw = DevExpress.Utils.DefaultBoolean.True;

        ToolStripItemCollection items = listBoxControlOutput.ContextMenuStrip.Items;
        _ = items.Add(new ToolStripMenuItem("Clear", clearImg, (o, a) =>
        {
            listBoxControlOutput.Items.Clear();
            listBoxControlOutput.SelectedIndex = 0;
        }));

        _ = items.Add(new ToolStripMenuItem("Copy all", clearAllImg, (o, a) =>
        {
            IEnumerable<string> strings =
                    from n in Enumerable.Range(0, listBoxControlOutput.Items.Count)
                    let t = listBoxControlOutput.Items[n].ToString()
                    select Regex.Replace(t, "<.*?>", "")
                ;

            string text = string.Join("\r\n", strings);
            if (!text.IsNullOrEmpty())
            {
                Clipboard.Clear();
                Clipboard.SetText(text);
            }
        }));

        _ = items.Add(new ToolStripMenuItem("Copy selected", copyImg, (o, a) =>
        {
            IEnumerable<string> strings =
                    from item in listBoxControlOutput.SelectedItems
                    let str = item.ToString()
                    select Regex.Replace(str, "<.*?>", "")
                ;

            string text = string.Join("\r\n", strings);
            if (!text.IsNullOrEmpty())
            {
                Clipboard.Clear();
                Clipboard.SetText(text);
            }
        }));
        log4net.ILog a = Log4NetLogger.Logger;
        log4net.Repository.Hierarchy.Logger logger = (log4net.Repository.Hierarchy.Logger)Log4NetLogger.Logger.Logger;
        logger.Level = Level.All;

        ToolStripMenuItem log_menu = new("Log Level", logLevelBtnImg);
        ToolStripMenuItem logItemError = new("Error", null);
        ToolStripMenuItem logItemWarn = new("Warn", null);
        ToolStripMenuItem logItemInfo = new("Info", null);
        ToolStripMenuItem logItemDebug = new("Debug", null);
        ToolStripMenuItem logItemAll = new("All", null)
        {
            Image = logLevelChkImg
        };

        List<ToolStripMenuItem> logLvlControls = new()
        {
            logItemError, logItemWarn, logItemInfo, logItemDebug, logItemAll
        };

        logLvlControls.Iter(i => i.Click += (ss, ee) =>
        {
            ToolStripMenuItem tool = (ToolStripMenuItem)ss;
            Level sellvl = getLevel(tool.Text);
            logLvlControls.Iter(c =>
            {
                c.Checked = false;
                c.Image = null;
            });
            tool.Checked = true;
            tool.Image = logLevelChkImg;

            logger.Level = sellvl;

            static Level getLevel(string lvl)
            {
                Level l = lvl.ToUpper() switch
                {
                    "ERROR" => Level.Error,
                    "WARN" => Level.Warn,
                    "INFO" => Level.Info,
                    "DEBUG" => Level.Debug,
                    "ALL" => Level.All,
                    _ => Level.All
                };
                return l;
            }
        });
        log_menu.DropDownItems.AddRange(logLvlControls.ToArray());
        _ = items.Add(log_menu);


        listBoxControlOutput.MouseClick += (s, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                listBoxControlOutput.ContextMenuStrip.Show();
            }
        };

        listBoxControlOutput.DrawItem += (s, e) =>
        {
            Brush selectedItemBackBrush = Brushes.SteelBlue;
            string itemText = listBoxControlOutput.GetItemText(e.Index);
            if ((e.State & DrawItemState.Selected) != 0)
            {
                e.Cache.FillRectangle(selectedItemBackBrush, e.Bounds);
                using (Font f = new(e.Appearance.Font.Name, e.Appearance.Font.Size, FontStyle.Bold))
                {
                    Match m = Regex.Match(itemText, @"(?<=\<.+\>)(.*?)(?=<\/.+>)");
                    e.Cache.DrawString(m.Value, f, Brushes.White, e.Bounds, e.Appearance.GetStringFormat());
                }

                e.Handled = true;
            }
        };
    }

    public static Color GetLogLevelColor(string levelName)
    {
        return levelName switch
        {
            "DEBUG" => Color.Chocolate,
            "INFO" => Color.Navy,
            "ERROR" => Color.Red,
            "WARN" => Color.Brown,
            _ => Color.Black
        };
    }

    public void Close()
    {
    }

    public async void DoAppend(LoggingEvent logEntry)
    {
        await this.DoAsync(tcs =>
        {
            try
            {
                string msg = logEntry.MessageObject.ToString();
                string level = logEntry.Level.Name;
                string cr = GetLogLevelColor(level).Name;
                string now = logEntry.TimeStamp.ToString("HH:mm:ss.fff");
                //Trace.WriteLine(msg);
                /*
                 * multi-line message 처리
                 */
                string[] lines = msg.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
                if (lines.Length > 0)
                {
                    string fmtMsg = string.Format($"<color={cr}>{now} [{level}]:{lines[0]}</color>");
                    _ = Items.Add(fmtMsg);


                    for (int i = 1; i < lines.Length; i++)
                    {
                        fmtMsg = $"<color={cr}>    {lines[i]}</color>";
                        _ = Items.Add(fmtMsg);
                    }

                    if (TrackEndOfLine)
                    {
                        SelectedIndex = Items.Count - 1;
                    }
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