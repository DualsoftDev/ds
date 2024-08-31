namespace T.CPU
open Dual.Common.UnitTest.FS

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
        ModelParser.ClearDicParsingText()
        let sys = ModelParser.ParseFromString(dsText, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
        /// 파싱후에는 TagManager가 없어야 한다.
        sys.TagManager === null
        let targetNDrvier =  (WINDOWS, PAIX_IO)
        DsAddressModule.assignAutoAddress(sys, 0, 100000) targetNDrvier
        let _ = DsCpuExt.CreateRuntime (sys) targetNDrvier

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
        한글 > "0-1_1" > Work1;
        "1"."ADV(INTrue)" > "1" > "0-1_1_1";
        한글 = {
            "3".ADV; 
        }
        "1" = {
            한글.ADV > 한글.RET > 한글_ADV_1;
        }
        [aliases] = {
            "0-1"."1" = { "0-1_1"; "0-1_1_1"; "0-1_1_1"; "0-1_1"; }
            "1".한글.ADV = { 한글_ADV_1; }
        }
    }
    [flow] "0-1" = {
        "1" = {
            "1".ADV > "2".RET > "3".ADV;
        }
    }
    [jobs] = {
        "0+1"."1"."ADV(INTrue)" = { "0+1_1".ADV(IB0.0:Boolean:True, -); }
        "0+1".한글.RET = { "0+1_한글".RET(IB0.3, OB0.3); }
        "0+1".한글.ADV = { "0+1_한글".ADV(IB0.2, OB0.2); }
        "0-1"."2".RET = { "0-1_2".RET(IB0.5, OB0.5); }
        "0-1"."1".ADV = { "0-1_1".ADV(IB0.4, OB0.4); }
        "0-1"."3".ADV = { "0-1_3".ADV(IB0.6, OB0.6); }
        "0+1"."3".ADV = { "0+1_3".ADV(IB0.1, OB0.1); }
    }
    [interfaces] = {
        Api1 = { "0+1"."1" ~ "0+1"."1" }
    }
    [buttons] = {
        [a] = { AutoSelect(M1001, -) = { "0+1"; "0-1"; } }
        [m] = { ManualSelect(M1002, -) = { "0+1"; "0-1"; } }
        [d] = { DrivePushBtn(M1003, -) = { "0+1"; "0-1"; } }
        [e] = { EmergencyBtn(M1004, -) = { "0+1"; "0-1"; } }
        [p] = { PausePushBtn(M1005, -) = { "0+1"; "0-1"; } }
        [c] = { ClearPushBtn(M1006, -) = { "0+1"; "0-1"; } }
    }
    [lamps] = {
        [a] = { AutoModeLamp(-, M1007) = {  } }
        [m] = { ManualModeLamp(-, M1008) = {  } }
        [d] = { DriveLamp(-, M1009) = {  } }
        [e] = { ErrorLamp(-, M1010) = {  } }
        [r] = { ReadyStateLamp(-, M1011) = {  } }
        [i] = { IdleModeLamp(-, M1012) = {  } }
        [o] = { OriginStateLamp(-, M1013) = {  } }
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
//DS Engine Version = [0.9.9.1]
            """
        check testCodeAdvance
        
        