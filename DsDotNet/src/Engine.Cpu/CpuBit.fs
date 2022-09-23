namespace Engine.Cpu

open Engine.Core
open System.Diagnostics

[<AutoOpen>]
module BitModule =

    [<DebuggerDisplay("{ToText()}")>]
    type Bit(cpu:ICpu, name) = 
        inherit Named(name)
        let mutable _value:bool = false

        do ()

        override x.ToText() = name


        interface ICpuBit with
            member _.Value with get () = _value and set (v) = _value <- v

        member x.Cpu = cpu

  
//[DebuggerDisplay("{ToText()}")]
//public abstract class Bit : Named, ICpuBit
//{
//    protected bool _value;
//    public virtual bool Value => _value;    //{ get => _value; set => _value = value; }
//    public List<ICpuBit> Containers { get; } = new();

//    public Cpu Cpu { get; set; }
//    public Bit(Cpu cpu, string name, bool bit = false) : base(name)
//    {
//        Assert(cpu != null);

//        _value = bit;
//        Cpu = cpu;
//        cpu.BitsMap.Add(name, this);
//    }

//    /// <summary>  Bit 생성 이전에 동일 이름이 존재하는지 check 하기 위한 용도. </summary>
//    public static T GetExistingBit<T>(Cpu cpu, string name) where T : Bit
//    {
//        if (cpu.BitsMap.ContainsKey(name))
//        {
//            var existing = cpu.BitsMap[name];
//            if (existing is T)
//                return existing as T;
//            else
//                throw new Exception($"ERROR: duplicate name {name} exists with other type {existing.GetType().Name}");
//        }
//        return null;
//    }

//    // Value setter 를 수행하지 않기 위한 생성자.  BitReEvaluatable 의 base 생성자로 사용됨
//    protected Bit(string name, Cpu cpu) : base(name)
//    {
//        Assert(cpu != null);
//        Cpu = cpu;
//        cpu.BitsMap.Add(name, this);
//    }
//    // null cpu 를 허용하기 위한 생성자.  OpcTag 만 cpu null 허용
//    internal Bit(string name, bool bit = false) : base(name)
//    {
//        _value = bit;
//        Assert(GetType().Name.Contains("DataTag"));
//    }


//    public override string ToText() => BitExtension.ToText(this);
//}

