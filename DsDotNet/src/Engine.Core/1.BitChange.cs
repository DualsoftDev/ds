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

public abstract class PortInfoChange : BitChange
{
    public PortInfo PortInfo { get; }
    //public PortInfoChange(PortInfo bit, bool newValue, object cause = null, ExceptionHandler onError =null)
    //    : base(bit, newValue, cause, onError)
    //{
    //    PortInfo = bit;
    //}
    public PortInfoChange(BitChange bc)
        : base(bc.Bit, bc.NewValue, bc.Cause, bc.OnError)
    {
        PortInfo = (PortInfo)bc.Bit;
    }
}
public class PortInfoPlanChange : PortInfoChange
{
    public bool Applied { get; set; }
    public PortInfoPlanChange(BitChange bc) : base(bc) { }
    //public PortInfoPlanChange(PortInfo bit, bool newValue, object cause = null, ExceptionHandler onError = null)
    //    : base(bit, newValue, cause, onError)
    //{}
}
public class PortInfoActualChange : PortInfoChange
{
    public PortInfoActualChange(BitChange bc) : base(bc) { }
    //public PortInfoActualChange(PortInfo bit, bool newValue, object cause = null, ExceptionHandler onError = null)
    //    : base(bit, newValue, cause, onError)
    //{}
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
