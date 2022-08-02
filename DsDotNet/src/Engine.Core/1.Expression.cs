namespace Engine.Core;

public abstract class Expression : BitReEvaluatable
{
    public override bool Value
    {
        get => throw new Exception("Should be redefined");
        set => throw new Exception("ERROR");
    }

    protected Expression(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(cpu, name, monitoringBits)
    {
        // override 된 Value 를 생성자에서 호출 가능함.
        _value = this.Value;
    }

    protected override void ReEvaulate(IBit causeBit)
    {
        var newValue = Value;
        if (_value != newValue)
        {
            _value = newValue;
            BitChange.Publish(this, newValue, true, causeBit);
        }
    }

    public override string ToString() => ExpressionExtension.ToText(this);
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


public static class ExpressionExtension
{
    public static string ToText(this IBit bit)
    {
        return bit switch
        {
            BitReEvaluatable eval =>
                new Func<string>(() => {
                    var inners = eval._monitoringBits.Select(b => b.ToText());
                    return eval switch
                    {
                        And and => $"({string.Join(" & ", inners)})",
                        Or or => $"({string.Join(" | ", inners)})",
                        Not not => $"!{inners.First()}",
                        Latch latch =>
                            new Func<string>(() =>      // https://stackoverflow.com/questions/59890226/multiple-statements-in-a-switch-expression-c-sharp-8
                            {
                                var set = latch._setCondition.ToText();
                                var reset = latch._resetCondition.ToText();
                                return $"Latch[Set={set}, Reset={reset}]";
                            }).Invoke(),
                        PortExpression pe =>
                            new Func<string>(() =>
                            {
                                var plan = pe.Plan.ToText();
                                var actual = "";
                                if (pe.Actual != null)
                                    actual = $", Actual={ToText(pe.Actual)}";
                                return $"{pe.Segment.Name}.[{pe.GetType().Name}]=[Plan={plan}]{actual}";
                            }).Invoke(),
                        _ => throw new Exception("ERROR")
                    };
                }).Invoke(),
            Bit b => b.Name,
            _ => throw new Exception("ERROR")   //$"ToStringText=>{bit.Cpu.Name}[{bit.GetType().Name}]",
        };
    }
}

