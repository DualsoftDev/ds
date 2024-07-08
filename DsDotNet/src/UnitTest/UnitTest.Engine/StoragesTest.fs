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
    let check(dsText:string) = 
        let systemRepo = ShareableSystemRepository()
        let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        let helper = ModelParser.ParseFromString2(dsText, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
        let sys = helper.TheSystem  
        /// 파싱후에는 TagManager가 없어야 한다.
        sys.TagManager === null
        DsAddressModule.assignAutoAddress(sys, 0, 100000) PlatformTarget.WINDOWS
        let _ = DsCpuExt.GetDsCPU (sys) PlatformTarget.WINDOWS

        /// CPU 생성후 기본 Storage 생성 확인
        sys.TagManager.Storages.Count > 0 === true
        /// 시스템 TAG 정상 생성 확인
        sys.TagManager.Storages[getStorageName sys (int SystemTag.autoMonitor)].Name  === getStorageName sys (int SystemTag.autoMonitor)
        sys.TagManager.Storages[getStorageName sys (int SystemTag.errorMonitor)].Name === getStorageName sys (int SystemTag.errorMonitor)


        let call = sys.GetVerticesOfJobCalls().Head()

        sys.TagManager.Storages[getStorageName call (int VertexTag.going)].Target.Value === call

        sys.TagManager.Storages[getStorageName call (int VertexTag.finish)].Name === getStorageName call (int VertexTag.finish)
        

    [<Test>]
    member __.``Test Generation Name`` () =

        let testCodeNormal = """
             [sys] HelloDS = {
                [flow] STN1 = {
                    Work1 = {
                        Device1.ADV; 
                        Device2.ADV; 
                        Device1.RET; 
                        Device2.RET; 
                    }
                }
                [jobs] = {
                    STN1.Device1.ADV = { STN1_Device1.ADV(-, -); }
                    STN1.Device2.ADV = { STN1_Device2.ADV(-, -); }
                    STN1.Device1.RET = { STN1_Device1.RET(-, -); }
                    STN1.Device2.RET = { STN1_Device2.RET(-, -); }
                }
   
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_EXT; 
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_Device1; 
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_Device2; 
            }
            """
        check testCodeNormal
        
    [<Test>]
    member __.``Test 한글 및 공란 Generation Name`` () =

        let testCodeAdvance = """
        [sys] 한글시스템 = {
    [flow] "0+1" = {
        "1".ADV.INTrue > "1" > "0-1_1";
        한글 > "0-1"."1" > Work1;
        한글 = {
            "3".ADV; 
        }
        "1" = {
            한글.ADV > 한글.RET > 한글_ADV_1;
        }
        [aliases] = {
            "0-1"."1" = { "0-1_1"; }
            "1".한글.ADV = { 한글_ADV_1; }
        }
    }
    [flow] "0-1" = {
        "1" = {
            "1".ADV > "2".RET > "3".ADV;
        }
    }
    [jobs] = {
        "0+1"."1".ADV.INTrue = { "0+1_1".ADV(_:Boolean:True, _); }
        "0+1".한글.RET = { "0+1_한글".RET(_, _); }
        "0+1".한글.ADV = { "0+1_한글".ADV(_, _); }
        "0-1"."2".RET = { "0-1_2".RET(_, _); }
        "0-1"."1".ADV = { "0-1_1".ADV(_, _); }
        "0-1"."3".ADV = { "0-1_3".ADV(_, _); }
        "0+1"."3".ADV = { "0+1_3".ADV(_, _); }
    }
    [interfaces] = {
        Api1 = { "0+1"."1" ~ "0+1"."1" }
    }
    [buttons] = {
        [a] = { AutoSelect(_, -) = { "0+1"; "0-1"; } }
        [m] = { ManualSelect(_, -) = { "0+1"; "0-1"; } }
        [d] = { DrivePushBtn(_, -) = { "0+1"; "0-1"; } }
        [e] = { EmergencyBtn(_, -) = { "0+1"; "0-1"; } }
        [p] = { PausePushBtn(_, -) = { "0+1"; "0-1"; } }
        [c] = { ClearPushBtn(_, -) = { "0+1"; "0-1"; } }
    }
    [lamps] = {
        [a] = { AutoModeLamp(-, _) = {  } }
        [m] = { ManualModeLamp(-, _) = {  } }
        [d] = { DriveLamp(-, _) = {  } }
        [e] = { ErrorLamp(-, _) = {  } }
        [r] = { ReadyStateLamp(-, _) = {  } }
        [i] = { IdleModeLamp(-, _) = {  } }
        [o] = { OriginStateLamp(-, _) = {  } }
    }
    [prop] = {
        [layouts] = {
            "0+1_1" = (133, 266, 220, 80);
            "0+1_한글" = (991, 346, 220, 80);
            "0-1_2" = (984, 469, 220, 80);
            "0-1_1" = (697, 493, 220, 80);
            "0-1_3" = (1203, 542, 220, 80);
            "0+1_3" = (334, 676, 220, 80);
        }
    }
    [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
        "0+1_1",
        "0+1_한글",
        "0-1_2",
        "0-1_1",
        "0-1_3",
        "0+1_3"; 
}
//DS Language Version = [1.0.0.1]
//DS Library Date = [Library Release Date 24.3.26]
//DS Engine Version = [0.9.8.36]
            """
        check testCodeAdvance
        
        