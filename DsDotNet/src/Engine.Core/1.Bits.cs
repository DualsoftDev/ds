namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract class Bit : Named, IBit
{
    bool _value;
    public virtual bool Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                Global.BitChangedSubject.OnNext(new BitChange(this, value, true));
            }
        }
    }

    public Cpu OwnerCpu { get; set; }
    public Bit(Cpu ownerCpu = null, string name = "", bool bit = false) : base(name)
    {
        Debug.Assert(ownerCpu != null);

        Value = bit;
        OwnerCpu = ownerCpu;
    }
    protected Bit(Cpu ownerCpu, string name) : base(name)
    {
        Debug.Assert(ownerCpu != null);
        OwnerCpu = ownerCpu;
    }


    public override string ToString() => ToText();
    public override string ToText() => $"{base.ToText()}@{OwnerCpu.Name}";
}


/// <summary> 다른 bit 요소(monitoringBits)에 의해서 값이 변경될 수 있는 bit 에 대한 추상 class </summary>
public abstract class BitReEvaluatable : Bit
{
    protected IBit[] _monitoringBits;
    protected abstract void ReEvaulate(BitChange bitChange);
    public override bool Value { set => throw new DsException("Not Supported."); }
    protected BitReEvaluatable(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(cpu, name)
    {
        _monitoringBits = monitoringBits;
        Global.BitChangedSubject
            .Where(bc => monitoringBits.Contains(bc.Bit))
            .Subscribe(ReEvaulate)
            ;
    }
}



public class Flag : Bit {
    public Flag(Cpu cpu, string name, bool bit = false) : base(cpu, name, bit) { }
}



[DebuggerDisplay("{QualifiedName}")]
public abstract class Port : Bit
{
    public Segment OwnerSegment { get; set; }
    public Port(Segment ownerSegment) => OwnerSegment = ownerSegment;
    public string QualifiedName => $"{OwnerSegment.QualifiedName}.{GetType().Name}";
    public override string ToString() => $"{QualifiedName}[{this.GetType().Name}]@{OwnerCpu.Name}={Value}";
}
public class PortS : Port
{
    public PortS(Segment ownerSegment) : base(ownerSegment) { Name = "PortS"; }
}
public class PortR : Port
{
    public PortR(Segment ownerSegment) : base(ownerSegment) { Name = "PortR"; }
}
public class PortE : Port
{
    public PortE(Segment ownerSegment) : base(ownerSegment) { Name = "PortE"; }
    bool _value;
    public override bool Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OwnerSegment.TagsEnd.Iter(et => et.Value = value);
            }
        }
    }

}


public class Relay : Bit { }
public class WeakRelay : Relay { }
public class StrongRelay : Relay { }


public record BitChange
{
    public IBit Bit { get; }
    public bool NewValue { get;  }
    public bool Applied { get; }
    public DateTime Time { get; }
    public BitChange(IBit bit, bool newValue, bool applied=false)
    {
        Bit = bit;
        NewValue = newValue;
        Applied = applied;
        Time = DateTime.Now;
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
