namespace Engine.Core;

// class or record?
public class BitChange
{
    public IBit Bit { get; }
    public bool NewValue { get; }
    /// <summary>IBit or string description</summary>
    public object Cause { get; }
    public DateTime Time { get; }
    public ExceptionHandler OnError { get; set; }
    public Action BeforeAction { get; set; }
    public Action AfterAction { get; set; }
    public string Guid { get; set; }
    public BitChange(IBit bit, bool newValue, object cause = null, ExceptionHandler onError =null)
    {
        //Debug.Assert(bit.Value != newValue);

        if (newValue && bit.GetName().IsOneOf("StartPlan_A_F_Vm", "StartPlan_B_F_Vp"))
            Console.WriteLine();
        //if (newValue && bit.GetName().IsOneOf("StartPort_A_F_Vm", "StartPort_B_F_Vp"))
        //    Console.WriteLine();

        Debug.Assert(bit != null);
        Debug.Assert(cause is null || cause is IBit || cause is string);
        //Debug.Assert(bit.Value != newValue);

        Guid = System.Guid.NewGuid().ToString().Substring(0, 4);
        Bit = bit;
        NewValue = newValue;
        Time = DateTime.Now;
        Cause = cause;
        OnError = onError;
    }

    public string CauseRepr => Cause switch
    {
        IBit b => $"{b.GetName()}={b.Value}",
        string s => s,
        null => null,
        _ => throw new Exception("ERROR"),
    };

    public override string ToString() => $"{Bit.GetName()}={Bit}={NewValue} by {CauseRepr}";
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

public record SegmentStatusChange
{
    public SegmentStatusChange(SegmentBase segment, Status4 status)
    {
        Segment = segment;
        Status = status;
    }

    public SegmentBase Segment { get; }
    public Status4 Status { get; set; }
}


public record ChildStatusChange
{
    public ChildStatusChange(Child child, Status4 status, bool isFlipped=false)
    {
        Child = child;
        Status = status;
        IsFlipped = isFlipped;
    }

    public Child Child { get; }
    public Status4 Status { get; }
    public bool IsFlipped { get; }
}
