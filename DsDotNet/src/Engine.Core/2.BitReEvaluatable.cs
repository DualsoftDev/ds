using Engine.Core.Obsolete;

namespace Engine.Core;

/// <summary> 다른 bit 요소(monitoringBits)에 의해서 값이 변경될 수 있는 bit 에 대한 추상 class </summary>
public abstract class BitReEvaluatable : Bit, IBitReadable
{
    internal IBit[] _monitoringBits;
    public abstract bool Evaluate();
    protected BitReEvaluatable(Cpu cpu, string name, params IBit[] monitoringBits)
        : base(name, cpu)
    {
        // PortInfo 의 경우, plan 대비 actual 에 null 을 허용
        _monitoringBits = monitoringBits.Where(b => b is not null).ToArray();
        _monitoringBits.OfType<Bit>().Iter(b => b.Containers.Add(this));
    }
}


