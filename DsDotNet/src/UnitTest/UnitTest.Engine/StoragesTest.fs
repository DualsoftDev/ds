namespace T.CPU
open Dual.UnitTest.Common.FS

open System.IO
open NUnit.Framework

open T
open Engine.Core
open Dual.Common.Core.FS
open Engine.CodeGenPLC
open Engine.CodeGenCPU
open Engine.Parser.FS
open Engine.Cpu


type StoragesTest() =
    inherit EngineTestBaseClass()


    [<Test>]
    member __.``Test Generation Name`` () =
        let f = getFuncName()
        let testCode = """
             [sys] HelloDS = {
                [flow] STN1 = {
                    STN1_EXT_ADV > Work1;
                    Work1 = {
                        STN1_Device1_ADV; 
                        STN1_Device2_ADV; 
                        STN1_Device1_RET; 
                        STN1_Device2_RET; 
                    }
                }
                [jobs] = {
                    STN1_EXT_ADV = { STN1_EXT.ADV(-, -); }
                    STN1_Device1_ADV = { STN1_Device1.ADV(-, -); }
                    STN1_Device2_ADV = { STN1_Device2.ADV(-, -); }
                    STN1_Device1_RET = { STN1_Device1.RET(-, -); }
                    STN1_Device2_RET = { STN1_Device2.RET(-, -); }
                }
   
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_EXT; 
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_Device1; 
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_Device2; 
            }
            """
        let systemRepo = ShareableSystemRepository()
        let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        let helper = ModelParser.ParseFromString2(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
        let sys = helper.TheSystem  
        /// 파싱후에는 TagManager가 없어야 한다.
        sys.TagManager === null

        let _ = DsCpuExt.GetDsCPU (sys) PlatformTarget.WINDOWS
        /// CPU 생성후 기본 Storage 생성 확인
        sys.TagManager.Storages.Count > 0 === true
        /// 시스템 TAG 정상 생성 확인
        sys.TagManager.Storages[getStorageName sys (int SystemTag.autoMonitor)].Name  === getStorageName sys (int SystemTag.autoMonitor)
        sys.TagManager.Storages[getStorageName sys (int SystemTag.errorMonitor)].Name === getStorageName sys (int SystemTag.errorMonitor)


        let call = sys.GetVerticesOfJobCalls().Head()

        sys.TagManager.Storages[getStorageName call (int VertexTag.going)].Target.Value === call

        sys.TagManager.Storages[getStorageName call (int VertexTag.finish)].Name === getStorageName call (int VertexTag.finish)
        
