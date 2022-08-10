using System.Threading.Tasks;

namespace Engine.Core;

[DebuggerDisplay("{ToText()}")]
public abstract class Bit : Named, IBit
{
    protected bool _value;
    public virtual bool Value
    {
        get => _value;
        set
        {
            Debug.Assert(this is IBitWritable);
            if (_value != value)
            {
                _value = value;
                BitChange.Publish(this, value, true);
            }
        }
    }
    internal virtual void SetValueOnly(bool newValue)
    {
        //Debug.Assert(_value != newValue);
        _value = newValue;
    }

    public Cpu Cpu { get; set; }
    public Bit(Cpu cpu, string name, bool bit = false) : base(name)
    {
        Debug.Assert(cpu != null);

        Value = bit;
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
        Debug.Assert(cpu != null);
        Cpu = cpu;
        cpu.BitsMap.Add(name, this);
    }
    // null cpu 를 허용하기 위한 생성자.  OpcTag 만 cpu null 허용
    internal Bit(string name, bool bit = false) : base(name)
    {
        Value = bit;
        Debug.Assert(GetType().Name.Contains("OpcTag"));
    }


    public override string ToText() => BitExtension.ToText(this);
}


