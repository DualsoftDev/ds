namespace Engine.Core;

public class Latch : BitReEvaluatable
{
    protected Tag subordinateTag;

    public override bool Value { get => subordinateTag.Value; set => subordinateTag.Value = value; }
    IBit _setCondition { get; }
    IBit _resetCondition { get; }

    public Latch(Cpu cpu, string name, IBit setCondition, IBit resetCondition)
        : base(name, cpu, new[] {setCondition, resetCondition})
    {
        _setCondition = setCondition;
        _resetCondition = resetCondition;
        subordinateTag = new Tag(cpu, null, $"{name}_internal", TagType.Subordinate);
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
