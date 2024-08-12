namespace T.Runtime
open T

open NUnit.Framework

open Dual.UnitTest.Common.FS
open Engine.Core
open Engine.Import.Office
open Engine.Cpu
open Engine.Runtime


[<AutoOpen>]
module HelloDSRuntimeTestModule =
    type SystemTextJson = System.Text.Json.JsonSerializer

    type HelloDSRuntimeTest() =
        inherit EngineTestBaseClass()
        do
            RuntimeDS.Package <- PCSIM
            RuntimeDS.TimeoutCall <- 15000u

        let helloDSPptPath = @$"{__SOURCE_DIRECTORY__}/../../../../Apps/OfficeAddIn/PowerPointAddInHelper/Utils/HelloDS.pptx"
        let helloDSZipPath = @"Z:\ds\DsDotNet\src\UnitTest\TestData\HelloDS.dsz"

        let runtimeModel = new RuntimeModel(helloDSZipPath, PlatformTarget.WINDOWS)

        let getSystem() =
            let pptParms:PptParams = defaultPptParams()
            let result = ImportPpt.GetDSFromPptWithLib (helloDSPptPath, false, pptParms)
            let {
                System = system
                ActivePath =  exportPath
                LoadingPaths = loadingPaths
                LayoutImgPaths = layoutImgPaths
            } = result

            system.TagManager === null
            let _ = DsCpuExt.GetDsCPU (system) (pptParms.TargetType, pptParms.DriverIO)
            system.TagManager.Storages.Count > 0 === true

            system


        // Z:\ds\DsDotNet\src\UnitTest\TestData\ 폴더에
        // - 최신 버젼의 HelloDS.dsz 파일 필요
        // - HelloDS.Logger.UnitTest.sqlite3 파일도 최신 HelloDS.dsz 으로 시뮬레이션 된 것이거나 삭제 필요
        [<Test>]
        member __.``X HelloDS runtime model test``() =
            let system = getSystem()
            let runtimeModel = runtimeModel

            let json = SystemTextJson.Serialize(runtimeModel.HMIPackage)
            let pkg = SystemTextJson.Deserialize<HMIPackage>(json)
            pkg.BuildTagMap()

            ()

