namespace Engine.Core;

public static class BitExtension
{
    public static string ToText(this IBit bit)
    {
        return bit switch
        {
            BitReEvaluatable eval =>
                new Func<string>(() => {
                    var inners = eval._monitoringBits.Select(b => b.ToText());
                    return eval switch
                    {
                        And and => $"({string.Join(" & ", inners)})",
                        Or or => $"({string.Join(" | ", inners)})",
                        Not not => $"!{inners.First()}",
                        Latch latch =>
                            new Func<string>(() =>      // https://stackoverflow.com/questions/59890226/multiple-statements-in-a-switch-expression-c-sharp-8
                            {
                                var set = latch._setCondition.ToText();
                                var reset = latch._resetCondition.ToText();
                                return $"Latch[Set={set}, Reset={reset}]";
                            }).Invoke(),
                        PortExpression pe =>
                            new Func<string>(() =>
                            {
                                if (pe.Plan == null)
                                    throw new Exception($"Port [{pe.Name}] Plan is null");
                                var plan = pe.Plan.ToText();
                                var actual = "";
                                if (pe.Actual != null)
                                    actual = $", Actual={ToText(pe.Actual)}";
                                return $"{pe.Segment.Name}.[{pe.GetType().Name}]=[Plan={plan}]{actual}";
                            }).Invoke(),
                        _ => throw new Exception("ERROR")
                    };
                }).Invoke(),
            Bit b => b.Name,
            _ => throw new Exception("ERROR")   //$"ToStringText=>{bit.Cpu.Name}[{bit.GetType().Name}]",
        };
    }
}

