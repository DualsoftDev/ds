namespace Engine.Parser.FS

open System
open System.Collections.Generic
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module rec ParserOptionModule =
    let private emptyStorage = Storages()

    type ParserOptions(systemRepo:ShareableSystemRepository, referencePath, activeCpuName, isSimulationMode, allowSkipExternalSegment, storages, absoluteFilePath, loadingType:ParserLoadingType) =
        member _.ActiveCpuName:string = activeCpuName
        member _.IsSimulationMode:bool = isSimulationMode           // { get; set; } = true
        member _.AllowSkipExternalSegment:bool = allowSkipExternalSegment // { get; set; } = true
        member _.Storages:Storages = storages
        /// [device or external system] 정의에서의 file path 속성값
        member val ReferencePath:string = referencePath with get, set

        /// [device or external system] 으로 새로 loading 된 system name.  외부 ds file 을 parsing 중일 때에만 Some 값을 가짐
        member val LoadedSystemName:string option = None with get, set

        member _.ShareableSystemRepository = systemRepo
        member val AbsoluteFilePath:string option = absoluteFilePath with get, set
        member _.LoadingType:ParserLoadingType = loadingType

        static member Create4Runtime(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType) = ParserOptions(systemRepo, referencePath, activeCpuName, false, false, emptyStorage, absoluteFilePath, loadingType)
        static member Create4Simulation(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType) = ParserOptions(systemRepo, referencePath, activeCpuName, true, false, emptyStorage, absoluteFilePath, loadingType)
        member x.Verify() = x.IsSimulationMode || (x.ActiveCpuName <> null && not x.AllowSkipExternalSegment)


