namespace Engine.Core;

#if false

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

    protected override void ReEvaluate(IBit causeBit)
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





public abstract class BitReEvaluatable : Bit, IBitReadable
{
    protected abstract BitChange NeedChange(IBit causeBit);
    // 생성자
    protected BitReEvaluatable(Cpu cpu, string name, params IBit[] monitoringBits)
    {
        // PortInfo 의 경우, plan 대비 actual 에 null 을 허용
        _monitoringBits = monitoringBits.Where(b => b is not null).ToArray();

        //! 여기
        RisingFalling.SourceSubject
            .Where(bit => monitoringBits.Contains(bit))
            .Subscribe(bit =>
            {
                var bitChange = NeedChange(bit);
                if (bitChange != null)
                    RisingFalling.ChangedSubject.OnNext(bitChange);
            });

        // 나머지...
            ;
    }
}

public class Latch : BitReEvaluatable
{
    protected override BitChange NeedChange(IBit causeBit)
    {
        var newValue = EvaluateGetValue();
        if (_value == newValue)
            return null;

        return new BitChange(this, newValue, false, causeBit);
    }

}


public class PortInfoEnd : PortInfo
{
    protected override BitChange NeedChange(IBit causeBit)
    {
        Debug.Assert(false);
        Debug.Assert(causeBit == Actual);
        return new BitChange(this, causeBit == Actual, false, causeBit);
    }
}


public abstract class PortInfoCommand : PortInfo
{
    protected override BitChange NeedChange(IBit causeBit)
    {
        Debug.Assert(causeBit == Plan);
        if (causeBit == Plan)
            return new BitChange(this, causeBit.Value, false, causeBit);
        return null;
    }
}


public interface IBit
{
    void SetValueSilently(bool newValue);
}

public abstract class Bit : Named, IBit
{
    public virtual void SetValueSilently(bool newValue) => _value = newValue;
    public void Apply()
    {
        Debug.Assert(!Applied);
        Bit.SetValueSilently(NewValue);
        Applied = true;
        Publish();
    }
}
public class Child, Coin, Segment, Edge
{
    public virtual void SetValueSilently(bool newValue) => Value = newValue;
    public virtual void SetValueSilently(bool newValue) => Value = newValue;
}


#endif
