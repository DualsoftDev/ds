using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static Engine.Core.TagKindModule;

namespace DSModeler
{

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

        internal static void AddLogicLog(TagDS.EventVertex t)
        {
            var txtStatus = "";
            switch (t.TagKind)
            {
                case VertexTag.ready: txtStatus = "[R]"; break;
                case VertexTag.going: txtStatus = "[G]"; break;
                case VertexTag.finish: txtStatus = "[F]"; break;
                case VertexTag.homing: txtStatus = "[H]"; break;
                default:
                    break;
            }

            var valueLog = new ValueLog()
            {
                Name = t.Tag.Name,
                Value = txtStatus == "" ? t.Tag.BoxedValue.ToString() : txtStatus,
                System = t.Target.Parent.GetSystem().Name,
                TagKind = TagKindExt.GetVertexTagKindText(t.Tag)
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


