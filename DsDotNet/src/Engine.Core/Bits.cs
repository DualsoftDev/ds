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

    [Flags]
    public enum TagType {
        None       = 0,
        Q          = 1 << 0,
        I          = 1 << 1,
        M          = 1 << 2,
        Special    = 1 << 3,

        // segment tag
        Start      = 1 << 4,
        Reset      = 1 << 5,
        End        = 1 << 6,
        /// <summary> 공정(flow) auto start, auto reset </summary>
        Auto       = 1 << 7,
        /// <summary> 외부 접근 용 Tag 여부 </summary>
        External   = 1 << 8,

        // call tag
        TX         = 1 << 9,
        RX         = 1 << 10,
    };
    public class Tag : Bit, ITxRx
    {
        public ISegmentOrCall Owner { get; set; }
        public TagType Type { get; set; }
        public Tag(ISegmentOrCall owner, string name, TagType tagType=TagType.None, CpuBase ownerCpu = null, bool value = false)
            : base(name, value, ownerCpu)
        {
            Owner = owner;
            Type = tagType;
        }
        public Tag(Tag tag)
            : this(tag.Owner, tag.Name, tag.Type, tag.OwnerCpu, tag.Value)
        {
        }

        public static Tag CreateAutoStart(Segment ownerSegment, string name, CpuBase ownerCpu) =>
            new Tag(ownerSegment, name, TagType.Auto | TagType.Start | TagType.Q | TagType.External, ownerCpu)
            ;
        public static Tag CreateAutoReset(Segment ownerSegment, string name, CpuBase ownerCpu) =>
            new Tag(ownerSegment, name, TagType.Auto | TagType.Reset | TagType.Q | TagType.External, ownerCpu)
            ;

        public static Tag CreateCallTx(Call ownerCall, Tag proto) =>
            new Tag(ownerCall, $"{proto.Name}_{ownerCall.QualifiedName}_TX", TagType.TX | TagType.Q | TagType.External, ownerCall.OwnerCpu)
            ;
        public static Tag CreateCallRx(Call ownerCall, Tag proto) =>
            new Tag(ownerCall, $"{proto.Name}_{ownerCall.QualifiedName}_RX", TagType.RX | TagType.I | TagType.External, ownerCall.OwnerCpu)
            ;

    }

    public static class TagExtension
    {
        public static bool IsExternal(this Tag tag) => tag.Type.HasFlag(TagType.External);
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
