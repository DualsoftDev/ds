using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public abstract class Bit : Named, IBit
    {
        public virtual bool Value { get; set; }
        public CpuBase OwnerCpu { get; set; }
        public Bit(string name = "", bool bit = false, CpuBase ownerCpu=null) : base(name) {
            Value = bit;
            OwnerCpu = ownerCpu;
        }
    }

    public class Flag : Bit {
        public Flag(string name, bool bit = false) : base(name, bit) { }
    }

    public abstract class Port : Bit
    {
        public Segment OwnerSegment { get; set; }
        public Port(Segment ownerSegment) => OwnerSegment = ownerSegment;
    }
    public class PortS : Port
    {
        public PortS(Segment ownerSegment) : base(ownerSegment) { Name = "PortS"; }
    }
    public class PortR : Port
    {
        public PortR(Segment ownerSegment) : base(ownerSegment) { Name = "PortR"; }
    }
    public class PortE : Port
    {
        public PortE(Segment ownerSegment) : base(ownerSegment) { Name = "PortE"; }
    }


    public class Expression : Bit
    {
        public override bool Value { get => base.Value; set => throw new Exception("ERROR"); }
    }

    public class And : Expression
    {
        public override bool Value { get { return Bits.All(b => b.Value); } set { throw new Exception("ERROR"); } }
        public List<IBit> Bits;
        public And(IBit[] bits)
        {
            Bits = bits.ToList();
        }
    }
    public class Or : Expression
    {
        public override bool Value => Bits.Any(b => b.Value);
        public List<IBit> Bits;
        public Or(IBit[] bits)
        {
            Bits = bits.ToList();
        }
    }

    public class Not : Expression
    {
        public override bool Value => !Bit.Value;
        public IBit Bit;
        public Not(IBit bit) => Bit = bit;
    }


    public class Rising : Bit { }
    public class Falling : Bit { }
    public class Latch : Bit {
        public IBit SetCondition { get; set; }
        public IBit ResetCondition { get; set; }
    }


    public class Relay : Bit { }
    public class WeakRelay : Relay { }
    public class StrongRelay : Relay { }


    public class BitChange
    {
        public IBit Bit { get; }
        public bool NewValue { get;  }
        public bool Applied { get; }
        public DateTime Time { get; }
        public BitChange(IBit bit, bool newValue, bool applied=false)
        {
            Bit = bit;
            NewValue = newValue;
            Applied = applied;
            Time = DateTime.Now;
        }

    }
}
