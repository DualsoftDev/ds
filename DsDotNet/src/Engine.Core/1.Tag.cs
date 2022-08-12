namespace Engine.Core;

[Flags]
public enum TagType
{
    None     = 0,
    Q        = 1 << 0,
    I        = 1 << 1,
    M        = 1 << 2,
    Special  = 1 << 5,

    // segment tag
    Start    = 1 << 11,
    Reset    = 1 << 12,
    End      = 1 << 13,
    Going    = 1 << 14,
    Ready    = 1 << 15,

    /// <summary> 공정(flow) auto start, auto reset </summary>
    Auto = 1 << 18,
    Manual = 1 << 19,
    Flow = 1 << 20,
    /// <summary> 외부 접근 용 Tag 여부 </summary>
    External = 1 << 21,

    // call tag
    TX       = 1 << 25,
    RX       = 1 << 26,
};



public class Tag : Bit, IBitReadWritable, ITxRx
{
    public ICoin Owner { get; set; }
    public TagType Type { get; set; }

    // todo: Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(Name, value));
    public void SetValue(bool newValue) => _value = newValue;

    /// <summary> 내부 추적용 Tag 이름 : QualifiedName + 기능명.  e.g "L.F.Main.AutoStart.  사용자가 지정하는 이름과는 별개 </summary>
    public string InternalName { get; set; }

    public Tag(Cpu ownerCpu, ICoin owner, string name, TagType tagType = TagType.None, bool value = false)
        : base(ownerCpu, name, value)
    {
        Debug.Assert(! ownerCpu.TagsMap.ContainsKey(name));

        Owner = owner;
        Type = tagType;
        ownerCpu.TagsMap.Add(name, this);
    }
    public Tag(Tag tag)
        : this(tag.Cpu, tag.Owner, tag.Name, tag.Type, tag.Value)
    {
    }

    public static Tag CreateAutoStart(Cpu ownerCpu, Segment ownerSegment, string name, string internalName) =>
        new Tag(ownerCpu, ownerSegment, name, TagType.Auto | TagType.Start | TagType.Q | TagType.External) { InternalName = internalName }
        ;
    public static Tag CreateAutoReset(Cpu ownerCpu, Segment ownerSegment, string name, string internalName) =>
        new Tag(ownerCpu, ownerSegment, name, TagType.Auto | TagType.Reset | TagType.Q | TagType.External) { InternalName = internalName }
        ;
}
