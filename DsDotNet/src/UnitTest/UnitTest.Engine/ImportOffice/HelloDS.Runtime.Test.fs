namespace T.Runtime
open T

open NUnit.Framework

open Dual.Common.UnitTest.FS
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
            RuntimeDS.ChangeRuntimePackage(Simulation)
        let modelConfig = createDefaultModelConfig()    
        let helloDSPptPath = @$"{__SOURCE_DIRECTORY__}/../../../../Apps/OfficeAddIn/PowerPointAddInHelper/Utils/HelloDS.pptx"
        let getSystem() =
            let pptParms:PptParams = defaultPptParams()

            let result = ImportPpt.GetDSFromPptWithLib (helloDSPptPath, false, pptParms, modelConfig)
            let {
                System = system
                ActivePath =  exportPath
                LoadingPaths = loadingPaths
                LayoutImgPaths = layoutImgPaths
            } = result

            system

        // - 항상 최신 버젼의 HelloDS.dsz 파일 만듬
        // - HelloDS.Logger.UnitTest.sqlite3 파일도 최신 HelloDS.dsz 으로 시뮬레이션 된 것이거나 삭제 필요
        [<Test>]
        member __.``HelloDS runtime model test``() =
            let system = getSystem()
            let userTagConfig = createDefaultUserTagConfig()
            let _dsCPU, hMIPackage, _pous = DsCpuExt.CreateRuntime(system) (WINDOWS) modelConfig userTagConfig
            system.TagManager.Storages.Count > 0 === true

            let json = SystemTextJson.Serialize(hMIPackage)
            let pkg = SystemTextJson.Deserialize<HMIPackage>(json)
            pkg.BuildTagMap()

            ()

