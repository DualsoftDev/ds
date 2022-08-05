namespace Engine.Core;

public class Latch : BitReEvaluatable
{
    public override bool Value { get => _value; set => throw new DsException("Not Supported."); }
    internal IBit _setCondition { get; }
    internal IBit _resetCondition { get; }

    public Latch(Cpu cpu, string name, IBit setCondition, IBit resetCondition)
        : base(cpu, name, new[] {setCondition, resetCondition})
    {
        Debug.Assert(setCondition != null && resetCondition != null);
        _setCondition = setCondition;
        _resetCondition = resetCondition;
        ReEvaluate(null);
    }

    public static Latch Create(Cpu cpu, string name, IBit setCondition, IBit resetCondition)
    {
        var existing = GetExistingBit<Latch>(cpu, name);
        if (existing != null)
        {
            Global.Logger.Warn($"Bit {name} already exists.  Using it instead creating new one.");
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
    protected override void ReEvaluate(IBit causeBit)
    {
        var value = Evaluate();
        if (_value != value)
        {
            _value = value;
            BitChange.Publish(this, value, true, causeBit);
        }
    }

}
