namespace Engine.Core;


internal static class RisingFalling
{
    public static Subject<IBit> SourceSubject { get; } = new();
    public static Subject<BitChange> ChangedSubject { get; } = new();
}



internal abstract class RisingFallingBase : BitReEvaluatable
{
    /// <summary> Rising/Falling 관찰 대상 bit 의 이전 value 값 저장
    protected bool _previousValue;
    public override bool Value
    {
        get => _value;
        set => throw new DsException("Not Supported.");
    }

    protected List<BitChange> GetAffectedChangeInfos()
    {
        List<BitChange> changes = new();
        using var _subscription =
            RisingFalling.ChangedSubject
                .Subscribe(bit => changes.Add(bit))
                ;
        RisingFalling.SourceSubject.OnNext(this);

        return changes;
    }

    protected RisingFallingBase(Cpu cpu, string name, IBit target)
        : base(cpu, name, target)
    {
        Debug.Assert(target != null);
    }

    protected override BitChange NeedChange(IBit causeBit) => throw new Exception("ERROR");

    protected override void ReEvaulate(IBit causeBit)
    {
        var value = _monitoringBits[0].Value;
        if (value != _previousValue)
        {
            if ((value && this is Rising) || (!value && this is Falling))
            {
                SetValueSilently(true);

                var bitChanges = GetAffectedChangeInfos();

                // end of rising/falling
                SetValueSilently(false);

                if (bitChanges.Any())
                {
                    var str = string.Join(", ", bitChanges.Select(ch => ch.ToString()));
                    Global.Logger.Debug($"\tFalling/Rising changes by bit {Name} => {str}");
                }


                foreach (var bc in bitChanges)
                {
                    var bit = bc.Bit as BitReEvaluatable;
                    Debug.Assert(bit is not null);
                    bc.Apply();
                }
            }

            _previousValue = value;
        }
    }

}

internal class Rising : RisingFallingBase
{
    public Rising(Cpu cpu, string name, IBit target)
        : base(cpu, name, target)
    {
    }
    public Rising(Bit target)
        : this(target.Cpu, $"{target.Name}_Rising", target)
    { }
}
internal class Falling : RisingFallingBase
{
    public Falling(Cpu cpu, string name, IBit target)
        : base(cpu, name, target)
    {
    }
    public Falling(Bit target)
        : this(target.Cpu, $"↓{target.Name}", target)
    {}
}



