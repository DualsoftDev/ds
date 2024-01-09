namespace Engine.Runtime
open System
open System.Linq
open IO.Core
open Engine.Cpu
open Engine.Core
open Engine.Parser.FS
open System.Collections.Generic
open Dual.Common.Core.FS
open Engine.CodeGenCPU
open Engine.CodeGenHMI.ConvertHMI
open DsStreamingModule
type FilePath = string


type RuntimeModel(zipDsPath:FilePath) =
    let jsonPath = unZip zipDsPath
    let model:Model = ParserLoader.LoadFromConfig (jsonPath) 
    let dsCPU, hmiPackage = DsCpuExt.GetDsCPU(model.System)
    let kindDescriptions = GetAllTagKinds() |> Tuple.toDictionary
    let dsStreaming = DsStreaming(model.System, PathManager.getDirectoryName(jsonPath|>DsFile))
    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.HMIPackage = hmiPackage
    member x.SourceDsZipPath = zipDsPath
    member x.TagKindDescriptions = kindDescriptions
    member x.JsonPath = jsonPath
    member x.DsStreaming = dsStreaming

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
