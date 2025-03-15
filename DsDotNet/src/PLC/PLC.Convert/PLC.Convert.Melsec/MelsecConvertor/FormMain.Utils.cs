using System;
using log4net.Core;
using System.Drawing;
using log4net;
using System.Linq;
using Dsu.Common.CS.LSIS.ExtensionMethods;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.VisualBasic.Logging;


namespace MelsecConverter
{
    public partial class FormAddressMapper
    {
        public static ILog Logger { get; set; }


        public async void DoAppend(LoggingEvent logEntry)
        {
            await this.DoAsync(() =>
            {
                try
                {
                    var msg = logEntry.MessageObject.ToString().Split('\n')[0];
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
                        var msgLine = lines[0].Replace("{", "{{").Replace("}", "}}");
                        var fmtMsg = string.Format($"<color={cr}>{now} [{level}]: {msgLine}</color>");
                        ucPanelLog1.Items.Add(fmtMsg);

                        for (int i = 1; i < lines.Length; i++)
                        {
                            fmtMsg = $"<color={cr}>    {lines[i]}</color>";
                            ucPanelLog1.Items.Add(fmtMsg);
                        }

                        ucPanelLog1.SelectedIndex = ucPanelLog1.Items.Count - 1;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to append log: {ex}");
                }
            });

            Color GetLogLevelColor(string levelName)
            {
                switch (levelName)
                {
                    case "DEBUG": return Color.Orange;
                    case "INFO": return Color.Navy;
                    case "ERROR": return Color.Red;
                    case "WARN": return Color.Brown;
                    default: return Color.Black;
                }
            }

        }


        private List<ResultLog> _logs = new List<ResultLog>();
        private void AddLog(ResultCase logCase, ResultData resultData, string pou, string logMessage)
        {
            var log = new ResultLog
            {
                Case = logCase,
                Result = resultData,
                Program = pou,
                Message = logMessage
            };

            _logs.Add(log);
            label_LogCount.Text = _logs.Count.ToString();
            gridControl_Result.RefreshDataSource();
        }

        private void LogErrors(string pouName, List<string> errLogs)
        {
            errLogs.ForEach(log =>
            {
                foreach (string line in log.Split('\n'))
                {
                    var msg = System.Net.WebUtility.HtmlDecode(line.TrimEnd('\r'));
                    if(!msg.StartsWith("IL:") && !msg.IsNullOrEmpty())
                        AddLog(ResultCase.Program, ResultData.Failure, pouName, msg);
                }
            });
        }

        private void LogSuccess(string pouName)
        {
            AddLog(ResultCase.Program, ResultData.Success, pouName, $" 변환 성공 OK");
        }

        private void LogWarnings(string pouName, List<string> warnLogs)
        {
            
            warnLogs.ForEach(log =>
            {
                foreach (string line in log.Split('\n')) {
                    AddLog(ResultCase.Program, ResultData.Warning, pouName, line);
                }
            });
        }

        private void SavePathsToRegistry(List<string> paths)
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\YourAppName\PouComments"))
                {
                    key.SetValue("Paths", string.Join(";", paths));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving paths to registry: " + ex.Message);
            }
        }

        private List<string> LoadPathsFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\YourAppName\PouComments"))
                {
                    if (key != null)
                    {
                        var pathsValue = key.GetValue("Paths") as string;
                        if (!string.IsNullOrEmpty(pathsValue))
                        {
                            return pathsValue.Split(';').ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading paths from registry: " + ex.Message);
            }
            return new List<string>();
        }
    }
}