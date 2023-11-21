namespace IOMapForModeler

open Engine.Core
open System
open System.Collections.Generic
//open IOMapApi.MemoryIOApi

module HwTagWriteModule = 
    //let memorySet = Dictionary<string, MemoryIO>()

    let CreateHwWriter(_) =()// devices |> Seq.iter (fun d -> memorySet.Add(d, MemoryIO(d)))
    let ClearHwWriter() = ()(* memorySet.Clear()       *)

    let WriteAction(_:IHwTag) = ()
        //let m = memorySet.[tag.MemoryName]
        //match tag.DataType with
        //| DuBOOL   -> m.WriteBit(Convert.ToBoolean(tag.Value), tag.Index/8, Convert.ToInt32(tag.Index%8))
        //| DuUINT16 -> m.Write(BitConverter.GetBytes(tag.Value|>toUInt16), Convert.ToInt32(tag.Index/2))
        //| DuUINT32 -> m.Write(BitConverter.GetBytes(tag.Value|>toUInt32), Convert.ToInt32(tag.Index/4))
        //| DuUINT64 -> m.Write(BitConverter.GetBytes(tag.Value|>toUInt64), Convert.ToInt32(tag.Index/8))
        //| DuUINT8  -> m.Write([|Convert.ToByte(tag.Value)|], Convert.ToInt32(tag.Index))
        //| _ -> failwithf $"Unsupported WriteAction type {tag.DataType}"
