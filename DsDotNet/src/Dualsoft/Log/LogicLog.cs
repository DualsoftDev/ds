using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Dual.Common.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using static Engine.Core.TagKindModule;
using static Engine.Core.TagKindModule.TagDS;

namespace DSModeler
{
    [SupportedOSPlatform("windows")]
    public static class LogicLog
    {

        public static List<ValueLog> ValueLogs { get; set; } = new List<ValueLog>();

        private static object _lock = new object();
        public static void TryAdd(ValueLog v)
        {
            lock (_lock)
            {
                var lastTime = ValueLogs.Any() ? ValueLogs.Last().GetTime() : DateTime.Now;

                var evt = Tuple.Create(LogicLog.ValueLogs.Count, v.GapTime(lastTime));
                Global.ChangeLogCount.OnNext(evt);

                ValueLogs.Add(v);
            }
        }
        public static void InitControl(GridLookUpEdit gle, GridView gv)
        {
            gle.Properties.DisplayMember = "Name";

            gv.PreviewLineCount = 20;
            gv.OptionsView.ShowAutoFilterRow = true;
            gv.OptionsView.ShowGroupPanel = false;
        }

        internal static void AddLogicLog(TagDS evt)
        {
            var logData = TagKindExt.GetTagToText(evt).Split(';');
            var valueLog = new ValueLog()
            {
                Name = logData[0],
                Value = logData[1],
                System = logData[2],
                TagKind = logData[3],
            };

            TryAdd(valueLog);
        }
    }

   

    public class ValueLog
    {
        private DateTime time = DateTime.Now;
        public string Time => time.ToString("HH:mm:ss.fff");
        public string Name { get; set; }
        public string Value { get; set; }
        public string System { get; set; }
        public string TagKind { get; set; }
        public TimeSpan GapTime(DateTime t) => time.Subtract(t);
        public DateTime GetTime() => time;
    }



}


