using Engine.Core.Obsolete;

namespace Engine.Core;

public class Flag : Bit, IBitReadWritable
{
    public void SetValue(bool newValue) => _value = newValue;
    public Flag(Cpu cpu, string name, bool bit = false) : base(cpu, name, bit) { }
}


