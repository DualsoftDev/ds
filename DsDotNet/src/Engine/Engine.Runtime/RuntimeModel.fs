namespace Engine.Runtime
open System
open Dual.Common.Core.FS
open IO.Core
open Engine.Cpu
open Engine.Core
open Engine.Parser.FS
open System.IO
open System.IO.Compression

type FilePath = string

type CompiledModel(zipDsPath:FilePath) =
    member x.SourceDsZipPath = zipDsPath

type RuntimeModel(zipDsPath:FilePath) =
    let compiledModel = CompiledModel(zipDsPath)
    //let mutable zmqInfo = Zmq.InitializeServer "zmqsettings.json" |> Some
    let mutable zmqInfo: ZmqInfo option = None
    let mutable dsCPU : DsCPU option = None

    do
        let model = ParserLoader.LoadFromConfig (unZip zipDsPath) 
        dsCPU <- Some(DsCpuExt.GetDsCPU(model.System, RuntimePackage.StandardPC))


        // todo: compiledModel <- ....
        ()

    interface IDisposable with
        member x.Dispose() = x.Dispose()
    member x.ModelSource = zipDsPath

    member x.CompiledModel = compiledModel
    //member x.IoHubInfo = zmqInfo

    member x.IoServer = zmqInfo |> map (fun x -> x.Server) |> Option.toObj
    //member x.IoHubClient = zmqInfo |> map (fun x -> x.Client) |> Option.toObj
    member x.IoSpec = zmqInfo |> map (fun x -> x.IOSpec) |> Option.toObj
    member x.CpuRun() = dsCPU.Value.Run()  
    member x.CpuStep()= dsCPU.Value.Step()  
    member x.CpuReset()= dsCPU.Value.Reset()  
    member x.CpuStop()= dsCPU.Value.Stop()  

    member x.Dispose() =
        match zmqInfo with
        | Some info ->
            info.Dispose()
            zmqInfo <- None
        | None -> failwith "IoHubInfo is already disposed"
