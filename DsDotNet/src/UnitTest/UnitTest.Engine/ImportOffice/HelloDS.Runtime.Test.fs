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

    type HelloDSRuntimeTest() =
        inherit EngineTestBaseClass()
        do
            RuntimeDS.Package <- PCSIM

        let helloDSPptPath = @$"{__SOURCE_DIRECTORY__}/../../../../Apps/OfficeAddIn/PowerPointAddInHelper/Utils/HelloDS.pptx"
        let helloDSZipPath = @"Z:\ds\DsDotNet\src\UnitTest\TestData\HelloDS.zip"
        let runtimeModel = new RuntimeModel(helloDSZipPath, PlatformTarget.WINDOWS)

        let getSystem() =
            let pptParms:PPTParams = {TargetType = WINDOWS; AutoIOM = true; CreateFromPPT = false;  CreateBtnLamp = true}
            let result = ImportPPT.GetDSFromPPTWithLib (helloDSPptPath, false, pptParms)
            let { 
                System = system
                ActivePath =  exportPath 
                LoadingPaths = loadingPaths 
                LayoutImgPaths = layoutImgPaths 
            } = result

            system.TagManager === null
            let _ = DsCpuExt.GetDsCPU (system) PlatformTarget.WINDOWS
            system.TagManager.Storages.Count > 0 === true

            system


        [<Test>]
        member __.``HelloDS runtime model test``() =
            let system = getSystem()
            let runtimeModel = runtimeModel

            ()

