namespace Engine.Core;

public abstract class Expression : BitReEvaluatable
{
    protected bool _oldValue;
    public override bool Value
    {
        get => throw new Exception("Should be redefined");
        set => throw new Exception("ERROR");
    }

    protected Expression(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(cpu, name, monitoringBits)
    {
        // override 된 Value 를 생성자에서 호출 가능함.
        _oldValue = this.Value;
    }


    protected override BitChange NeedChange(IBit causeBit) =>
        _oldValue == Value ? null : new BitChange(this, _oldValue != Value, false, causeBit)
        ;
    protected override void ReEvaulate(IBit causeBit)
    {
        var newValue = Value;
        if (_oldValue != newValue)
        {
            _oldValue = newValue;
            BitChange.Publish(this, newValue, true, causeBit);
        }
    }

}
public class And : Expression
{
    public override bool Value => _monitoringBits.All(b => b.Value);
    public And(Cpu cpu, string name, params IBit[] bits)
        : base(cpu, name, bits)
    {
    }
}
public class Or : Expression
{
    public override bool Value => _monitoringBits.Any(b => b.Value);
    public Or(Cpu cpu, string name, params IBit[] bits)
        : base(cpu, name, bits)
    {
    }
}

public class Not : Expression
{
    public override bool Value => !_monitoringBits[0].Value;
    public IBit Bit;
    public Not(Cpu cpu, string name, IBit bit)
        : base(cpu, name, bit)
    {
    }
    public Not(Bit bit)
        : base(bit.Cpu, $"Not_{bit.Name}", bit)
    {
    }
}

