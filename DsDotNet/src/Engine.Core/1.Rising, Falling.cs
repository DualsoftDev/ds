namespace Engine.Core;


public abstract class RisingFallingBase : BitReEvaluatable
{
    protected IBit _target { get; }
    bool _value;
    public override bool Value
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

    protected RisingFallingBase(Cpu cpu, string name, IBit target)
        : base(cpu, name, target)
    {
        _target = target;
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
        Value = _target.Value;

        // end of rising
        Value = false;

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
        Value = ! _target.Value;

        // end of falling
        Value = false;
    }
}



