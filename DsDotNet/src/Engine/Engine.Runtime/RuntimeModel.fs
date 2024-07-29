namespace Engine.Runtime
open System
open System.Linq
open IO.Core
open Engine.Cpu
open Engine.Core
open Engine.Parser.FS
open Dual.Common.Core.FS
type FilePath = string


type RuntimeModel(zipDsPath:FilePath, target)  =
    let jsonPath = unZip zipDsPath
    let model:Model = ParserLoader.LoadFromConfig (jsonPath) target
    let dsCPU, hmiPackage, _ = DsCpuExt.GetDsCPU model.System target
    let kindDescriptions = GetAllTagKinds() |> Tuple.toDictionary
    let storages =
        let skipInternal = true
        model.System.GetStorages(skipInternal).ToDictionary(fun stg -> stg.Name)
   
    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.HMIPackage = hmiPackage
    member x.SourceDsZipPath = zipDsPath
    member x.TagKindDescriptions = kindDescriptions
    member x.JsonPath = jsonPath
    member x.Storages = storages

    /// DsCPU: call Run, Step, Reset, Stop method on DsCPU
    member x.Cpu = dsCPU
    member x.System = model.System
    member x.Dispose() = dsCPU.Dispose()

type IoHub(zmqSettingsJson:FilePath) =
    let zmqInfo = Zmq.InitializeServer zmqSettingsJson

    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Server = zmqInfo.Server
    member x.Spec   = zmqInfo.IOSpec
    //member x.IoHubInfo = zmqInfo

    member x.Dispose() = zmqInfo.Dispose()
