namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract class Bit : Named, IBit
{
    public virtual bool Value { get; set; }
    public Cpu OwnerCpu { get; set; }
    public Bit(string name = "", bool bit = false, Cpu ownerCpu=null) : base(name) {
        Value = bit;
        OwnerCpu = ownerCpu;
    }
    protected Bit(string name, Cpu ownerCpu) : base(name)
    {
        OwnerCpu = ownerCpu;
    }


    public override string ToString() => ToText();
    public override string ToText() => $"{base.ToText()}@{OwnerCpu.Name}";
}


/// <summary> 다른 bit 요소(monitoringBits)에 의해서 값이 변경될 수 있는 bit 에 대한 추상 class </summary>
public abstract class BitReEvaluatable : Bit
{
    protected abstract void ReEvaulate(BitChange bitChange);
    protected BitReEvaluatable(string name, Cpu cpu, params IBit[] monitoringBits)
        : base(name, cpu)
    {
        Global.BitChangedSubject
            .Where(bc => monitoringBits.Contains(bc.Bit))
            .Subscribe(ReEvaulate)
            ;
    }
}



public class Flag : Bit {
    public Flag(string name, bool bit = false) : base(name, bit) { }
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


public class Expression : Bit
{
    public override bool Value { get => base.Value; set => throw new Exception("ERROR"); }
}

public class And : Expression
{
    public override bool Value { get { return Bits.All(b => b.Value); } set { throw new Exception("ERROR"); } }
    public List<IBit> Bits;
    public And(IBit[] bits)
    {
        Bits = bits.ToList();
    }
}
public class Or : Expression
{
    public override bool Value => Bits.Any(b => b.Value);
    public List<IBit> Bits;
    public Or(IBit[] bits)
    {
        Bits = bits.ToList();
    }
}

public class Not : Expression
{
    public override bool Value => !Bit.Value;
    public IBit Bit;
    public Not(IBit bit) => Bit = bit;
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
