namespace Engine.Core;

// class or record?
public class BitChange
{
    public IBit Bit { get; }
    public bool NewValue { get; }
    /// <summary>IBit or string description</summary>
    public object Cause { get; }
    public DateTime Time { get; }
    public Action BeforeAction { get; set; }
    public Action AfterAction { get; set; }
    public BitChange(IBit bit, bool newValue, object cause = null)
    {
        //DAssert(bit.Value != newValue);

        //if (newValue && bit.GetName().IsOneOf("StartPlan_A_F_Vm", "StartPlan_B_F_Vp"))
        //    Global.NoOp();

        DAssert(bit != null);
        DAssert(cause is null || cause is IBit || cause is string);
        //DAssert(bit.Value != newValue);

        Bit = bit;
        NewValue = newValue;
        Time = DateTime.Now;
        Cause = cause;
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

public class EndPortChange : BitChange
{
    public EndPortChange(IBit bit, bool newValue, object cause = null)
        : base(bit, newValue, cause)
    {
    }
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
