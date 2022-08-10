namespace Engine.Core;

/// <summary> 다른 bit 요소(monitoringBits)에 의해서 값이 변경될 수 있는 bit 에 대한 추상 class </summary>
public abstract class BitReEvaluatable : Bit, IBitReadable
{
    internal IBit[] _monitoringBits;
    //protected abstract void ReEvaluate(IBit causeBit);
    public abstract bool Evaluate();
    public override bool Value { set => throw new DsException("Not Supported."); }
    IDisposable _subscription;
    protected BitReEvaluatable(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(name, cpu)
    {
        // PortInfo 의 경우, plan 대비 actual 에 null 을 허용
        _monitoringBits = monitoringBits.Where(b => b is not null).ToArray();

        //ReSubscribe();
    }

    //internal void ReSubscribe()
    //{
    //    _subscription?.Dispose();
    //    _subscription =
    //        Global.RawBitChangedSubject
    //            .Select(bc => bc.Bit)
    //            .Where(bit => _monitoringBits.Contains(bit))
    //            .Subscribe(bit =>
    //            {
    //                ReEvaluate(bit);
    //            });
    //}
}


