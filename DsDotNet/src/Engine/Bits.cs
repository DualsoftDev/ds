using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public interface IBit
    {
        bool Value { get; }
        void Set();
        void Reset();
    }
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
    public class Tag : Bit {
        public bool IsExternal { get; set; }
        public Tag(string name, bool bit = false, bool isExternal = true) : base(name, bit) => IsExternal = isExternal;
    }

    public abstract class Port : Bit
    {
        public Segment OwnerSegment { get; set; }
        public Port(Segment ownerSegment) => OwnerSegment = ownerSegment;
    }
    public abstract class PortCommand : Port
    {
        /// Port S 나 R 을 시작시키는 명령.  OR'ing.  하나만 충족되면 S 나 R 을 시작
        public List<IBit> Commands { get; set; } = new List<IBit>();
        public PortCommand(Segment ownerSegment) : base(ownerSegment) {}
    }
    public class PortS : PortCommand
    {
        public PortS(Segment ownerSegment) : base(ownerSegment) {}
    }
    public class PortR : PortCommand
    {
        public PortR(Segment ownerSegment) : base(ownerSegment) {}
    }
    public class PortE : Port
    {
        /// End tag ON 시, turn on 시킬 Tag 목록
        public List<Tag> EndTags { get; set; } = new List<Tag>();
        public PortE(Segment ownerSegment) : base(ownerSegment) {}
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
