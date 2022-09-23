using System.Threading.Tasks;
using Engine.Core;
using static Engine.Core.DsType;

namespace Engine.Base;

// class or record?
public class BitChange
{
    public ICpuBit Bit { get; }
    public bool NewValue { get; }
    /// <summary>IBit or string description</summary>
    public object Cause { get; }
    public DateTime Time { get; }
    public Action BeforeAction { get; set; }
    public Action AfterAction { get; set; }
    public TaskCompletionSource<object> TCS { get; }
    public BitChange(ICpuBit bit, bool newValue, object cause = null)
    {
        //Assert(bit.Value != newValue);

        if (!newValue && bit.GetName().IsOneOf("ResetPlan_L_F_Main"))
            Global.NoOp();

        Assert(bit != null);
        Assert(cause is null || cause is ICpuBit || cause is string);
        //Assert(bit.Value != newValue);

        Bit = bit;
        NewValue = newValue;
        Time = DateTime.Now;
        Cause = cause;
        TCS = new TaskCompletionSource<object>();
    }

    public string CauseRepr => Cause switch
    {
        ICpuBit b => $"{b.GetName()}={b.Value}",
        string s => s,
        null => null,
        _ => throw new Exception("ERROR"),
    };

    public override string ToString() => $"{Bit.GetName()}={Bit}={NewValue} by {CauseRepr}";
}

public class EndPortChange : BitChange
{
    public EndPortChange(ICpuBit bit, bool newValue, object cause = null)
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
    public ChildStatusChange(Child child, Status4 status, bool isFlipped = false)
    {
        Child = child;
        Status = status;
        IsFlipped = isFlipped;
    }

    public Child Child { get; }
    public Status4 Status { get; }
    public bool IsFlipped { get; }
}
