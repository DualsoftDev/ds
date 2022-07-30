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
    /// <summary> 공정(flow) auto start, auto reset </summary>
    Auto     = 1 << 15,
    /// <summary> 외부 접근 용 Tag 여부 </summary>
    External = 1 << 16,

    // call tag
    TX       = 1 << 21,
    RX       = 1 << 22,
};



public class Tag : Bit, IBitReadWritable, ITxRx
{
    public ICoin Owner { get; set; }
    public TagType Type { get; set; }
    public override bool Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(Name, value));
                Global.RawBitChangedSubject.OnNext(new BitChange(this, value, true));
            }
        }
        /*NOTIFYACTION*/ //set => SetValueNowAngGetLaterNotifyAction(value, true).Invoke();
    }


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

    public static Tag CreateAutoStart(Cpu ownerCpu, Segment ownerSegment, string name) =>
        new Tag(ownerCpu, ownerSegment, name, TagType.Auto | TagType.Start | TagType.Q | TagType.External)
        ;
    public static Tag CreateAutoReset(Cpu ownerCpu, Segment ownerSegment, string name) =>
        new Tag(ownerCpu, ownerSegment, name, TagType.Auto | TagType.Reset | TagType.Q | TagType.External)
        ;

    /*NOTIFYACTION*/ //public Action SetValueNowAngGetLaterNotifyAction(bool newValue, bool notifyChange)
    /*NOTIFYACTION*/ //{
    /*NOTIFYACTION*/ //    if (_value != newValue)
    /*NOTIFYACTION*/ //    {
    /*NOTIFYACTION*/ //        var act = InternalSetValueNowAngGetLaterNotifyAction(newValue, notifyChange);
    /*NOTIFYACTION*/ //        if (notifyChange)
    /*NOTIFYACTION*/ //        {
    /*NOTIFYACTION*/ //            return new Action(() =>
    /*NOTIFYACTION*/ //            {
    /*NOTIFYACTION*/ //                act.Invoke();
    /*NOTIFYACTION*/ //                Global.TagChangeToOpcServerSubject.OnNext(new OpcTagChange(Name, newValue));
    /*NOTIFYACTION*/ //            });
    /*NOTIFYACTION*/ //
    /*NOTIFYACTION*/ //        }
    /*NOTIFYACTION*/ //        return act;
    /*NOTIFYACTION*/ //    }
    /*NOTIFYACTION*/ //    return new Action(() => { });
    /*NOTIFYACTION*/ //
    /*NOTIFYACTION*/ //}
}
