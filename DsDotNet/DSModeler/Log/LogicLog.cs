namespace DSModeler.Log
{
    [SupportedOSPlatform("windows")]
    public static class LogicLog
    {

        public static List<ValueLog> ValueLogs { get; set; } = new List<ValueLog>();

        private static readonly object _lock = new();
        public static void TryAdd(ValueLog v)
        {
            lock (_lock)
            {
                DateTime lastTime = ValueLogs.Any() ? ValueLogs.Last().GetTime() : DateTime.Now;

                Tuple<int, TimeSpan> evt = Tuple.Create(ValueLogs.Count, v.GapTime(lastTime));
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
            string[] logData = TagKindExt.GetTagToText(evt).Split(';');
            ValueLog valueLog = new()
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
        private readonly DateTime time = DateTime.Now;
        public string Time => time.ToString("HH:mm:ss.fff");
        public string Name { get; set; }
        public string Value { get; set; }
        public string System { get; set; }
        public string TagKind { get; set; }
        public TimeSpan GapTime(DateTime t)
        {
            return time.Subtract(t);
        }

        public DateTime GetTime()
        {
            return time;
        }
    }



}


