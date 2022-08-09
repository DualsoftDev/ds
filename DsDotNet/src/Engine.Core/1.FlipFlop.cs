namespace Engine.Core;


/// <summary>
/// S-R FlipFlop.   ↑S 에 의해서 ON 되고 ↑R 에 의해서 OFF 된다.
/// S-R 값에 의해서 재평가 될 수 없고, (do not derive from BitReEvaluatable)
/// S 나 R 의 rising 순간에 결과를 저장하고 있어야 한다.
/// </summary>
internal class FlipFlop : Bit, IBitWritable
{
    public IBit S { get; }
    public IBit R { get; }

    public FlipFlop(Cpu cpu, string name, IBit set, IBit reset, bool value=false)
        : base(cpu, name, value)
    {
        Debug.Assert(set != null && reset != null);
        S = set;
        R = reset;
    }
}
