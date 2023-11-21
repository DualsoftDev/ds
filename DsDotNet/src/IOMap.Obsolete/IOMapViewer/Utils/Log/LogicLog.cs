namespace IOMapViewer.Log;

[SupportedOSPlatform("windows")]
public static class LogicLog
{
    public static List<ValueLog> ValueLogs { get; set; } = new();

    private static readonly object _lock = new();

    public static void TryAdd(ValueLog v)
    {
        lock (_lock)
        {
            DateTime lastTime = ValueLogs.Any() ? ValueLogs.Last().GetTime() : DateTime.Now;

            Tuple<int, TimeSpan> evt = Tuple.Create(ValueLogs.Count, v.GapTime(lastTime));

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