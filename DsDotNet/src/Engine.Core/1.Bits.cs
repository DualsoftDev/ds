using System.Threading.Tasks;

namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract class Bit : Named, IBit
{
    protected bool _value;
    public virtual bool Value => _value;    //{ get => _value; set => _value = value; }
    public List<IBit> Containers { get; } = new ();

    public Cpu Cpu { get; set; }
    public Bit(Cpu cpu, string name, bool bit = false) : base(name)
    {
        Assert(cpu != null);

        _value = bit;
        Cpu = cpu;
        cpu.BitsMap.Add(name, this);
    }

    /// <summary>  Bit 생성 이전에 동일 이름이 존재하는지 check 하기 위한 용도. </summary>
    public static T GetExistingBit<T>(Cpu cpu, string name) where T: Bit
    {
        if (cpu.BitsMap.ContainsKey(name))
        {
            var existing = cpu.BitsMap[name];
            if (existing is T)
                return existing as T;
            else
                throw new Exception($"ERROR: duplicate name {name} exists with other type {existing.GetType().Name}");
        }
        return null;
    }

    // Value setter 를 수행하지 않기 위한 생성자.  BitReEvaluatable 의 base 생성자로 사용됨
    protected Bit(string name, Cpu cpu) : base(name)
    {
        Assert(cpu != null);
        Cpu = cpu;
        cpu.BitsMap.Add(name, this);
    }
    // null cpu 를 허용하기 위한 생성자.  OpcTag 만 cpu null 허용
    internal Bit(string name, bool bit = false) : base(name)
    {
        _value = bit;
        Assert(GetType().Name.Contains("OpcTag"));
    }


    public override string ToText() => BitExtension.ToText(this);
}


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

    public static void SetName(this IBit bit, string name)
    {
        if (bit is Named named)
            named.Name = name;
        else
            throw new Exception("ERROR");
    }

    public static string ToText(this IBit bit, bool expand = false)
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

            FlipFlop ff => $"[Set={ff.S.ToText()}, Reset={ff.R.ToText()}]",
            Bit b => b.Name,
            _ => throw new Exception("ERROR")   //$"ToStringText=>{bit.Cpu.Name}[{bit.GetType().Name}]",
        };
    }
}
