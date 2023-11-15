namespace Engine.Runtime
open System
open Dual.Common.Core.FS
open IO.Core

type FilePath = string
type ModelSource =
    | PptAndIoSetting of (FilePath * FilePath)
    | ZipDs of FilePath

type CompiledModel() =
    class end

type RuntimeModel(modelSource:ModelSource) =
    let mutable compiledModel:CompiledModel option = None
    let mutable zmqInfo:ZmqInfo option = None
    do
        match modelSource with
        | PptAndIoSetting (ppt,zmqSetting) -> ()
        | ZipDs zipDs -> ()

        // todo: compiledModel <- ....

    interface IDisposable with
        member x.Dispose() = ()
    member x.ModelSource = modelSource

    member x.CompiledModel = compiledModel.Value
    member x.IoHubInfo = zmqInfo.Value

