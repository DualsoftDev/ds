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
        ReEvaulate(null);
    }


    bool EvaluateGetValue()
    {
        return (_setCondition.Value, _resetCondition.Value) switch
        {
            (true, false) => true,
            (false, false) => _value,
            (_, true) => false,
        };
    }
    protected override void ReEvaulate(IBit causeBit)
    {
        var value = EvaluateGetValue();
        if (_value != value)
        {
            _value = value;
            BitChange.Publish(this, value, true, causeBit);
        }
    }

}
