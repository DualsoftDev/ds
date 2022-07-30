namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract class Bit : Named, IBit
{
    protected bool _value;
    public virtual bool Value
    {
        get => _value;
        set
        {
            Debug.Assert(this is IBitWritable);
            /*NOTIFYACTION*/ //InternalSetValueNowAngGetLaterNotifyAction(value, true).Invoke();
            if (_value != value)
            {
                _value = value;
                Global.RawBitChangedSubject.OnNext(new BitChange(this, value, true));
            }

        }
    }
    /*NOTIFYACTION*/ //protected Action InternalSetValueNowAngGetLaterNotifyAction(bool newValue, bool notifyChange)
    /*NOTIFYACTION*/ //{
    /*NOTIFYACTION*/ //    if (_value != newValue)
    /*NOTIFYACTION*/ //    {
    /*NOTIFYACTION*/ //        _value = newValue;
    /*NOTIFYACTION*/ //        if (notifyChange)
    /*NOTIFYACTION*/ //            return new Action(() => Global.RawBitChangedSubject.OnNext(new BitChange(this, newValue, true)));
    /*NOTIFYACTION*/ //    }
    /*NOTIFYACTION*/
    /*NOTIFYACTION*/ //    return new Action(() => { });
    /*NOTIFYACTION*/ //}

    public Cpu Cpu { get; set; }
    public Bit(Cpu cpu, string name, bool bit = false) : base(name)
    {
        Debug.Assert(cpu != null);

        Value = bit;
        Cpu = cpu;
        cpu.BitsMap.Add(name, this);
    }

    // Value setter 를 수행하지 않기 위한 생성자
    protected Bit(string name, Cpu cpu) : base(name)
    {
        Debug.Assert(cpu != null);
        Cpu = cpu;
        cpu.BitsMap.Add(name, this);
    }
    // null cpu 를 허용하기 위한 생성자.  OpcTag 만 cpu null 허용
    internal Bit(string name, bool bit = false) : base(name)
    {
        Value = bit;
        Debug.Assert(GetType().Name.Contains("OpcTag"));
    }


    public override string ToString() => ToText();
    public override string ToText() => $"{base.ToText()}@{Cpu.Name}";
}


/// <summary> 다른 bit 요소(monitoringBits)에 의해서 값이 변경될 수 있는 bit 에 대한 추상 class </summary>
public abstract class BitReEvaluatable : Bit, IBitReadable
{
    protected IBit[] _monitoringBits;
    protected abstract void ReEvaulate(BitChange bitChange);
    public override bool Value { set => throw new DsException("Not Supported."); }
    protected BitReEvaluatable(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(name, cpu)
    {
        // PortExpression 의 경우, plan 대비 actual 에 null 을 허용
        _monitoringBits = monitoringBits.Where(b => b is not null).ToArray();
        Global.BitChangedSubject
            .Where(bc => monitoringBits.Contains(bc.Bit))
            .Subscribe(bc =>
            {
                ReEvaulate(bc);
            })
            ;
    }
}



public class Flag : Bit, IBitReadWritable
{
    public Flag(Cpu cpu, string name, bool bit = false) : base(cpu, name, bit) { }

    /*NOTIFYACTION*/ //public Action SetValueNowAngGetLaterNotifyAction(bool newValue, bool notifyChange) => InternalSetValueNowAngGetLaterNotifyAction(newValue, notifyChange);
}



[DebuggerDisplay("{QualifiedName}")]
[Obsolete("PortExpression 으로 대체 예정")]
public abstract class Port : Bit, IBitReadWritable
{
    public Segment OwnerSegment { get; set; }
    public Port(Segment ownerSegment, string name)
        : base(ownerSegment.Cpu, $"{ownerSegment.QualifiedName}_{name}")
    {
        OwnerSegment = ownerSegment;
    }
    public string QualifiedName => $"{OwnerSegment.QualifiedName}.{GetType().Name}";
    public override string ToString() => $"{QualifiedName}[{this.GetType().Name}]@{Cpu.Name}={Value}";
    /*NOTIFYACTION*/ //public virtual Action SetValueNowAngGetLaterNotifyAction(bool newValue, bool notifyChange) => InternalSetValueNowAngGetLaterNotifyAction(newValue, notifyChange);
}
public class PortS : Port
{
    public PortS(Segment ownerSegment) : base(ownerSegment, "PortS") {}
}
public class PortR : Port
{
    public PortR(Segment ownerSegment) : base(ownerSegment, "PortR") {}
}
public class PortE : Port
{
    public PortE(Segment ownerSegment) : base(ownerSegment, "PortE") {}
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

    /*NOTIFYACTION*/ //public override Action SetValueNowAngGetLaterNotifyAction(bool newValue, bool notifyChange)
    /*NOTIFYACTION*/ //{
    /*NOTIFYACTION*/ //    if (_value != newValue)
    /*NOTIFYACTION*/ //    {
    /*NOTIFYACTION*/ //        var act = InternalSetValueNowAngGetLaterNotifyAction(newValue, notifyChange);
    /*NOTIFYACTION*/ //        if (notifyChange)
    /*NOTIFYACTION*/ //        {
    /*NOTIFYACTION*/ //            return new Action(() =>
    /*NOTIFYACTION*/ //            {
    /*NOTIFYACTION*/ //                act.Invoke();
    /*NOTIFYACTION*/ //                Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(Name, newValue));
    /*NOTIFYACTION*/ //            });
    /*NOTIFYACTION*/ //        }
    /*NOTIFYACTION*/ //        return act;
    /*NOTIFYACTION*/ //    }
    /*NOTIFYACTION*/ //    return new Action(() => {});
    /*NOTIFYACTION*/ //}


}


//public class Relay : Bit { }
//public class WeakRelay : Relay { }
//public class StrongRelay : Relay { }


// class or record?
public class BitChange
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
