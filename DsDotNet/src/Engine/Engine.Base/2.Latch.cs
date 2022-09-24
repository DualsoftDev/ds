namespace Engine.Base;

public class Latch : BitReEvaluatable, IBitWritable
{
    internal ICpuBit _setCondition { get; }
    internal ICpuBit _resetCondition { get; }

    public Latch(Cpu cpu, string name, ICpuBit setCondition, ICpuBit resetCondition)
        : base(cpu, name, new[] { setCondition, resetCondition })
    {
        Assert(setCondition != null && resetCondition != null);
        _setCondition = setCondition;
        _resetCondition = resetCondition;
        _value = Evaluate();
    }

    public static Latch Create(Cpu cpu, string name, ICpuBit setCondition, ICpuBit resetCondition)
    {
        var existing = GetExistingBit<Latch>(cpu, name);
        if (existing != null)
        {
            LogWarn($"Bit {name} already exists.  Using it instead creating new one.");
            return existing;
        }

        return new Latch(cpu, name, setCondition, resetCondition);
    }

    public override bool Evaluate()
    {
        return (_setCondition.Value, _resetCondition.Value) switch
        {
            (true, false) => true,
            (false, false) => _value,
            (_, true) => false,
        };
    }

    public void SetValue(bool newValue)
    {
        var realValue = Evaluate();
        Assert(realValue == newValue);
        _value = newValue;
    }

}
