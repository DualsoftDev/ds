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
open Engine.Info
open Engine.CodeGenHMI.ConvertHMI
type FilePath = string


type RuntimeModel(zipDsPath:FilePath) =
    let model:Model = ParserLoader.LoadFromConfig (unZip zipDsPath) 
    let dsCPU, hmiPackage = DsCpuExt.GetDsCPU(model.System, RuntimePackage.StandardPC)
    let kindDescriptions = DBLoggerApi.GetAllTagKinds() |> Tuple.toDictionary
    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.HMIPackage = hmiPackage
    member x.SourceDsZipPath = zipDsPath
    member x.TagKindDescriptions = kindDescriptions

    /// DsCPU: call Run, Step, Reset, Stop method on DsCPU
    member x.Cpu = dsCPU
    member x.Dispose() = dsCPU.Dispose()

type IoHub(zmqSettingsJson:FilePath) =
    let zmqInfo = Zmq.InitializeServer zmqSettingsJson

    interface IDisposable with
        member x.Dispose() = x.Dispose()

    member x.Server = zmqInfo.Server
    member x.Spec   = zmqInfo.IOSpec
    //member x.IoHubInfo = zmqInfo

    member x.Dispose() = zmqInfo.Dispose()
