namespace Engine.Core;

public abstract class Expression : BitReEvaluatable
{
    public override bool Value
    {
        get => Evaluate();
        set => throw new Exception("ERROR");
    }
    public override bool Evaluate() => throw new Exception("Should be redefined");

    protected Expression(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(cpu, name, monitoringBits)
    {
        // override 된 Value 를 생성자에서 호출 가능함.
        _value = this.Value;
    }


    //protected override void ReEvaluate(IBit causeBit)
    //{
    //    var newValue = Value;
    //    if (_value != newValue)
    //    {
    //        _value = newValue;
    //        BitChange.Publish(this, newValue, true, causeBit);
    //    }
    //}

    public override string ToString() => BitExtension.ToText(this);
}
public class And : Expression
{
    public override bool Evaluate() => _monitoringBits.All(b => b.Value);
    public And(Cpu cpu, string name, params IBit[] bits)
        : base(cpu, name, bits)
    {
    }
}
public class Or : Expression
{
    public override bool Evaluate() => _monitoringBits.Any(b => b.Value);
    public Or(Cpu cpu, string name, params IBit[] bits)
        : base(cpu, name, bits)
    {
    }
}

public class Not : Expression
{
    public override bool Evaluate() => !_monitoringBits[0].Value;

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

