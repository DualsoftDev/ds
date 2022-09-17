using Engine.Core.Obsolete;

namespace Engine.Core;

public class Latch : BitReEvaluatable, IBitWritable
{
    internal IBit _setCondition { get; }
    internal IBit _resetCondition { get; }

    public Latch(Cpu cpu, string name, IBit setCondition, IBit resetCondition)
        : base(cpu, name, new[] { setCondition, resetCondition })
    {
        Assert(setCondition != null && resetCondition != null);
        _setCondition = setCondition;
        _resetCondition = resetCondition;
        _value = Evaluate();
    }

    public static Latch Create(Cpu cpu, string name, IBit setCondition, IBit resetCondition)
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
