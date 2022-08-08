namespace Engine.Core;

public static class BitExtension
{
    public static string GetName(this IBit bit)
    {
        return bit switch
        {
            Named n => n.Name,
            _ => bit.ToText(),
        };
    }
    public static string ToText(this IBit bit, bool expand=false)
    {
        string getText(IBit bit, bool expand) => expand ? bit.ToText(true) : bit.GetName();

        return bit switch
        {
            BitReEvaluatable eval =>
                new Func<string>(() => {
                    var inners = eval._monitoringBits.Select(b => getText(b, expand));
                    return eval switch
                    {
                        And and => $"({string.Join(" & ", inners)})",
                        Or or => $"({string.Join(" | ", inners)})",
                        Not not => $"!{inners.First()}",
                        Latch latch =>
                            new Func<string>(() =>      // https://stackoverflow.com/questions/59890226/multiple-statements-in-a-switch-expression-c-sharp-8
                            {
                                var set = getText(latch._setCondition, expand);
                                var reset = getText(latch._resetCondition, expand);
                                return $"Latch[Set={set}, Reset={reset}]";
                            }).Invoke(),
                        PortInfo pe =>
                            new Func<string>(() =>
                            {
                                if (pe.Plan == null)
                                    throw new Exception($"Port [{pe.Name}] Plan is null");
                                var plan = getText(pe.Plan, expand);
                                var actual = "";
                                if (pe.Actual != null)
                                    actual = $", Actual={getText(pe.Actual, expand)}";
                                return $"{pe.Segment?.Name}.[{pe.GetType().Name}]=[Plan={plan}]{actual}";
                            }).Invoke(),
                        _ => throw new Exception("ERROR")
                    };
                }).Invoke(),
            Bit b => b.Name,
            _ => throw new Exception("ERROR")   //$"ToStringText=>{bit.Cpu.Name}[{bit.GetType().Name}]",
        };
    }
}

