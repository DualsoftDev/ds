namespace Engine.Core;

public class Flag : Bit, IBitReadWritable
{
    public Flag(Cpu cpu, string name, bool bit = false) : base(cpu, name, bit) { }
}


