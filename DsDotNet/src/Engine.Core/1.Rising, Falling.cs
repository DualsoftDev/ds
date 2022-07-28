namespace Engine.Core;


public abstract class RisingFallingBase : BitReEvaluatable
{
    bool _value;
    public override bool Value
    {
        get => _value;
        set => throw new DsException("Not Supported.");
    }

    protected void SetValue(bool value)
    {
        if (_value != value)
        {
            _value = value;
            Global.BitChangedSubject.OnNext(new BitChange(this, value, true));
        }
    }

    protected RisingFallingBase(Cpu cpu, string name, IBit target)
        : base(cpu, name, target)
    {
    }

}

public class Rising : RisingFallingBase
{
    public Rising(Cpu cpu, string name, IBit target)
        : base(cpu, name, target)
    {
    }

    protected override void ReEvaulate(BitChange bitChange)
    {
        SetValue(_monitoringBits[0].Value);

        // end of rising
        SetValue(false);

    }
}
public class Falling : RisingFallingBase
{
    public Falling(Cpu cpu, string name, IBit target)
        : base(cpu, name, target)
    {
    }

    protected override void ReEvaulate(BitChange bitChange)
    {
        SetValue(!_monitoringBits[0].Value);

        // end of falling
        SetValue(false);
    }
}



