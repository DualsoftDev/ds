namespace Engine.Parser.FS

open System
open System.Collections.Generic
open Engine.Core
open Dual.Common.Core.FS

[<AutoOpen>]
module ParserOptionModule =

    type ParserOptions =
        {
            ActiveCpuName: string
            IsSimulationMode: bool
            AllowSkipExternalSegment: bool
            AllowAutoGenDevice: bool
            Storages: Storages
            /// [device or external system] 정의에서의 file path 속성값
            ReferencePath: string

            /// [device or external system] 으로 새로 loading 된 system name.  외부 ds file 을 parsing 중일 때에만 Some 값을 가짐
            LoadedSystemName: string option

            ShareableSystemRepository: ShareableSystemRepository
            AbsoluteFilePath: string option
            LoadingType: ParserLoadingType
            IsNewModel: bool // 새로운 모델인지 여부
        }

    type ParserOptions with

        static member Create4Runtime(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType, autoGenDevice, isNewModel) =
            { ActiveCpuName = activeCpuName
              IsSimulationMode = false
              AllowSkipExternalSegment = false
              AllowAutoGenDevice = autoGenDevice
              Storages = Storages()
              ReferencePath = referencePath
              LoadedSystemName = None
              ShareableSystemRepository = systemRepo
              AbsoluteFilePath = absoluteFilePath
              LoadingType = loadingType 
              IsNewModel = isNewModel
              }

        static member Create4RuntimeLoadedSystem(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType, autoGenDevice, loadedName) =
            let runtime =
                ParserOptions.Create4Runtime(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType, autoGenDevice, false)

            { runtime with LoadedSystemName = Some loadedName }

        static member Create4Simulation(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType) =
            let runtime =
                ParserOptions.Create4Runtime(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType, false, true)

            { runtime with IsSimulationMode = true }

        static member Create4ChatGpt(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType) =
            let runtime =
                ParserOptions.Create4Runtime(systemRepo, referencePath, activeCpuName, absoluteFilePath, loadingType, true, true)

            { runtime with IsSimulationMode = true }

        member x.Verify() =
            x.IsSimulationMode
            || (x.ActiveCpuName <> null && not x.AllowSkipExternalSegment)
