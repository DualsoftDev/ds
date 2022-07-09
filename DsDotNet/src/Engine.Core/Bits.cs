using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Core
{
    public abstract class Bit : Named, IBit
    {
        bool _value;
        public virtual bool Value => _value;
        public Cpu OwnerCpu { get; set; }
        public Bit(string name = "", bool bit = false, Cpu ownerCpu=null) : base(name) {
            _value = bit;
            OwnerCpu = ownerCpu;
        }

        public virtual void Set() => _value = true;
        public virtual void Reset() => _value = false;
    }

    public class Flag : Bit {
        public Flag(string name, bool bit = false) : base(name, bit) { }
    }

    public enum TagType { Unknown, Q, I, M, Special };
    public class Tag : Bit, ITxRx
    {
        public Segment OwnerSegment { get; set; }
        public TagType Type { get; set; }
        /// <summary> 외부 접근 용 Tag 여부 </summary>
        public bool IsExternal { get; set; }
        public Tag(Segment ownerSegment, string name, bool bit = false, bool isExternal = false) : base(name, bit)
        {
            IsExternal = isExternal;
            OwnerSegment = ownerSegment;
        }
    }

    public class TagAutoStart : Tag, IAutoTag
    {
        public TagAutoStart(Segment ownerSegment, string name, bool bit = false, bool isExternal = true)
            : base(ownerSegment, name, bit, isExternal) {}
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
        public override void Set() => throw new Exception("Not allowed");
        public override void Reset() => throw new Exception("Not allowed");
    }

    public class And : Expression
    {
        public override bool Value => Bits.All(b => b.Value);
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
}
