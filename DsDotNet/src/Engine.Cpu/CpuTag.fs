namespace Engine.Cpu

[<AutoOpen>]
module TagModule =

    type Tag(name) = 
        member x.Name = name
        member x.TagType = TagType.AUTO ||| TagType.ETC
        


//public abstract class Tag : Bit, IBitReadWritable, ITxRx
//{
//    public ICoin Owner { get; set; }
//    public TagType Type { get; set; }

//    public void SetValue(bool newValue) => _value = newValue;

//    protected Tag(Cpu ownerCpu, ICoin owner, string name, TagType tagType = TagType.None, bool value = false)
//        : base(ownerCpu, name, value)
//    {
//        Assert(!ownerCpu.TagsMap.ContainsKey(name));
//        //LogDebug($"Creating tag {name}");

//        Owner = owner;
//        Type = tagType;
//        ownerCpu.TagsMap.Add(name, this);
//    }
//}

///// <summary> Tag Actual (w/ address) </summary>
//public class TagA : Tag
//{
//    public string Address { get; set; }

//    public TagA(Cpu ownerCpu, ICoin owner, string name, string address, TagType tagType, bool value = false)
//        : base(ownerCpu, owner, name, tagType, value)
//    {
//        Address = address;
//    }
//}

///// <summary> Tag Plan </summary>
//public class TagP : Tag
//{
//    public TagP(Cpu ownerCpu, ICoin owner, string name, TagType tagType, bool value = false)
//        : base(ownerCpu, owner, name, tagType, value)
//    { }
//}


///// <summary> Tag Etc : flow auto, going/ready tag,</summary>
//public class TagE : Tag
//{
//    public TagE(Cpu ownerCpu, ICoin owner, string name, TagType tagType = TagType.Etc, bool value = false)
//        : base(ownerCpu, owner, name, tagType, value)
//    { }
//}



  