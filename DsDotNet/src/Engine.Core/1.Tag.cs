using Engine.Core.Obsolete;

namespace Engine.Core;

[Flags]
public enum TagType
{
    None = 0,
    Q = 1 << 0,
    I = 1 << 1,
    M = 1 << 2,
    Special = 1 << 5,

    // segment tag
    Start = 1 << 11,
    Reset = 1 << 12,
    End = 1 << 13,
    Going = 1 << 14,
    Ready = 1 << 15,

    /// <summary> 공정(flow) auto start, auto reset </summary>
    Auto = 1 << 18,
    Manual = 1 << 19,
    Flow = 1 << 20,
    /// <summary> 외부 접근 용 Tag 여부 </summary>
    External = 1 << 21,
    Plan = 1 << 22,
    Etc = 1 << 23,

    // call tag
    TX = 1 << 25,
    RX = 1 << 26,
};



public abstract class Tag : Bit, IBitReadWritable, ITxRx
{
    public ICoin Owner { get; set; }
    public TagType Type { get; set; }

    public void SetValue(bool newValue) => _value = newValue;

    protected Tag(Cpu ownerCpu, ICoin owner, string name, TagType tagType = TagType.None, bool value = false)
        : base(ownerCpu, name, value)
    {
        Assert(!ownerCpu.TagsMap.ContainsKey(name));
        //LogDebug($"Creating tag {name}");

        Owner = owner;
        Type = tagType;
        ownerCpu.TagsMap.Add(name, this);
    }
}

/// <summary> Tag Actual (w/ address) </summary>
public class TagA : Tag
{
    public string Address { get; set; }

    public TagA(Cpu ownerCpu, ICoin owner, string name, string address, TagType tagType, bool value = false)
        : base(ownerCpu, owner, name, tagType, value)
    {
        Address = address;
    }
}

/// <summary> Tag Plan </summary>
public class TagP : Tag
{
    public TagP(Cpu ownerCpu, ICoin owner, string name, TagType tagType, bool value = false)
        : base(ownerCpu, owner, name, tagType, value)
    { }
}


/// <summary> Tag Etc : flow auto, going/ready tag,</summary>
public class TagE : Tag
{
    public TagE(Cpu ownerCpu, ICoin owner, string name, TagType tagType = TagType.Etc, bool value = false)
        : base(ownerCpu, owner, name, tagType, value)
    { }
}


