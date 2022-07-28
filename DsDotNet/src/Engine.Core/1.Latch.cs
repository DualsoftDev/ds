namespace Engine.Core;

public class Latch : BitReEvaluatable
{
    protected Flag subordinateTag;

    public override bool Value { get => subordinateTag.Value; set => throw new DsException("Not Supported."); }
    IBit _setCondition { get; }
    IBit _resetCondition { get; }

    public Latch(Cpu cpu, string name, IBit setCondition, IBit resetCondition)
        : base(cpu, name, new[] {setCondition, resetCondition})
    {
        Debug.Assert(setCondition != null && resetCondition != null);
        _setCondition = setCondition;
        _resetCondition = resetCondition;
        subordinateTag = new Flag(cpu, $"{name}_LatchInternal");
        ReEvaulate(null);
    }

    protected override void ReEvaulate(BitChange _bitChange)
    {
        var value = (_setCondition.Value, _resetCondition.Value) switch
        {
            (true, false) => true,
            (false, false) => subordinateTag.Value,
            (_, true) => false,
        };
        if (subordinateTag.Value != value)
        {
            subordinateTag.Value = value;
            Global.BitChangedSubject.OnNext(new BitChange(this, value, true));
        }
    }

}
