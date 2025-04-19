namespace Engine.Runtime
open System
open System.Linq
open Engine.Cpu
open Engine.Core
open Engine.Parser.FS
open Dual.Common.Core.FS
open Engine.Core.MapperDataModule
type FilePath = string


type RuntimeModel(zipDsPath:FilePath, target:PlatformTarget)  =
    let jsonPath = unZip zipDsPath
    let model:Model = ParserLoader.LoadFromConfig (jsonPath) target 
    let dsCPU, hmiPackage, _ = DsCpuExt.CreateRuntime model.System (target) model.ModelConfig 
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
    member x.PlatformTarget = target
    member x.HwIP = model.ModelConfig.HwIP
    member x.HwIO = model.ModelConfig.HwTarget.HwIO
    member x.ModelConfig = model.ModelConfig
    member x.TagConfig = model.ModelConfig.TagConfig

    /// DsCPU: call Run, Step, Reset, Stop method on DsCPU
    member x.Cpu = dsCPU
    member x.System = model.System
    member x.Dispose() = dsCPU.Dispose()

