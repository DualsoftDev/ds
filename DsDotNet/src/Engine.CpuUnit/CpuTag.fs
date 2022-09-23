namespace Engine.CpuUnit

open Engine.Core

[<AutoOpen>]
module CpuTag =

    [<AbstractClass>]
    type Tag(cpu:Cpu, name, tagType:TagType) as this = 
        inherit Bit(cpu, name)
        interface IBitReadWritable with
            member _.SetValue(v: bool) = this.Value <- v
            member _.Value = this.Value
             
        interface ITxRx  

        member x.Name = name
        member x.TagType = tagType
        
///// <summary> Tag Actual (w/ address) </summary>
    type TagA(cpu:Cpu, name, tagType:TagType, address) = 
        inherit Tag(cpu, name, tagType)
        member x.Address = address

///// <summary> Tag Plan </summary>
    type TagP(cpu:Cpu, name, tagType:TagType) = 
        inherit Tag(cpu, name, tagType)

///// <summary> Tag Etc : flow auto, going/ready tag,</summary>
    type TagE(cpu:Cpu, name, tagType:TagType, address) = 
        inherit Tag(cpu, name, tagType)
        member x.Address = address
