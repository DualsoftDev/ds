namespace Engine.Core;

// class or record?
public class BitChange
{
    public IBit Bit { get; }
    public bool NewValue { get; }
    /// <summary>IBit or string description</summary>
    public object Cause { get; }
    public bool Applied { get; internal set; }
    public DateTime Time { get; }
    public BitChange(IBit bit, bool newValue, bool applied= false, object cause = null)
    {
        Debug.Assert(cause is null || cause is IBit || cause is string);
        Bit = bit;
        NewValue = newValue;
        Applied = applied;
        Time = DateTime.Now;
        Cause = cause;
        if (cause == null)
            Console.WriteLine();
    }

    public string CauseRepr => Cause switch
    {
        IBit b => $"{b.GetName()}={b.Value}",
        string s => s,
        null => null,
        _ => throw new Exception("ERROR"),
    };


    public static void Publish(IBit bit, bool newValue, bool applied, object cause = null)
    {
        (new BitChange(bit, newValue, applied, cause)).Publish();
    }
    public void Publish()
    {
        Debug.Assert(false);

        //if (Global.IsSupportParallel && Bit is PortInfoEnd)
        //{
        //    //! 현재값 publish 를 threading 으로 처리...
        //    var capturedThis = this;
        //    var task = new Task(() =>
        //    {
        //        Global.RawBitChangedSubject.OnNext(capturedThis);
        //    });
        //    PendingTasks.Add(task);
        //    task.ContinueWith(t => PendingTasks.TryRemove(t, out Task _task));
        //    task.Start();
        //}
        //else
        //    Global.RawBitChangedSubject.OnNext(this);
    }

    public override string ToString() => $"{Bit.GetName()}={Bit}={NewValue}";
}



public record OpcTagChange
{
    public string TagName { get; }
    public bool Value { get; }
    public OpcTagChange(string tagName, bool value)
    {
        TagName = tagName;
        Value = value;
    }
}
