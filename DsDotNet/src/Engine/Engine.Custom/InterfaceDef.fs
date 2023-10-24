namespace Engine.Custom

open System
open System.Collections.Generic
open System.Threading.Tasks

type IDsObject = interface end

type DsApi(readTag:Func<string, obj>, writeTag:Action<string, obj>) =
    member x.ReadTag(tag:string) = readTag.Invoke(tag)
    member x.ReadBit(tag:string) = readTag.Invoke(tag) :?> bool
    member x.WriteTag(tag:string, value:obj) = writeTag.Invoke(tag, value)

type IEngineExtension =
    abstract member Initialize: dsApi:DsApi -> Dictionary<string, IDsObject>

type IBitObject =
    inherit IDsObject
    abstract member SetAsync: objectName:string -> Task
    abstract member ResetAsync: objectName:string -> Task
