using System;

namespace Engine.Core;

[Flags]
public enum TagType
{
    None = 0,
    Q = 1 << 0,
    I = 1 << 1,
    M = 1 << 2,
    Special = 1 << 3,

    // segment tag
    Start = 1 << 4,
    Reset = 1 << 5,
    End = 1 << 6,
    /// <summary> 공정(flow) auto start, auto reset </summary>
    Auto = 1 << 7,
    /// <summary> 외부 접근 용 Tag 여부 </summary>
    External = 1 << 8,

    // call tag
    TX = 1 << 9,
    RX = 1 << 10,
};



public class Tag : Bit, ITxRx
{
    public ICoin Owner { get; set; }
    public TagType Type { get; set; }
    bool _value;
    public override bool Value
    {
        get => _value;
        set {
            if (_value != value)
            {
                _value = value;
                Global.BitChangedSubject.OnNext(new BitChange(this, value, true));
            }
        }
    }

    public Tag(ICoin owner, string name, TagType tagType=TagType.None, CpuBase ownerCpu = null, bool value = false)
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

}
