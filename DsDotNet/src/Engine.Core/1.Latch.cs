namespace Engine.Core;

public class Latch : BitReEvaluatable
{
    public override bool Value { get => _value; set => throw new DsException("Not Supported."); }
    IBit _setCondition { get; }
    IBit _resetCondition { get; }

    public Latch(Cpu cpu, string name, IBit setCondition, IBit resetCondition)
        : base(cpu, name, new[] {setCondition, resetCondition})
    {
        Debug.Assert(setCondition != null && resetCondition != null);
        _setCondition = setCondition;
        _resetCondition = resetCondition;
        ReEvaulate(null);
    }

    protected override void ReEvaulate(BitChange _bitChange)
    {
        var value = (_setCondition.Value, _resetCondition.Value) switch
        {
            (true, false) => true,
            (false, false) => _value,
            (_, true) => false,
        };
        if (_value != value)
        {
            _value = value;
            Global.RawBitChangedSubject.OnNext(new BitChange(this, value, true));
        }
    }

}
