namespace Engine.Core;


public class FlipFlop : Bit, IBitWritable
{
    internal IBit _setCondition { get; }
    internal IBit _resetCondition { get; }

    public FlipFlop(Cpu cpu, string name, IBit setCondition, IBit resetCondition, bool value=false)
        : base(cpu, name, value)
    {
        Debug.Assert(setCondition != null && resetCondition != null);
        _setCondition = setCondition;
        _resetCondition = resetCondition;
    }
}
