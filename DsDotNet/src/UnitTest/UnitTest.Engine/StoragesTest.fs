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
        check testCodeNormal
        
    [<Test>]
    member __.``Test 한글 및 공란 Generation Name`` () =

        let testCodeAdvance = """
            [sys] HelloDS = {
                [flow] "00-한글" = {
                    Work2 => "1" => Work2;
                    "1" = {
                        "00-한글__+11_RET", "00-한글__-11_RET", "00-한글__+11_ADV", "00-한글__-11_ADV", "00-한글__?11_ADV"; 
                    }
                }
                [flow] "00+한글" = {
                    "00+한글__외부시작_ADV_INTrue" > Work1;
                    Work2 => "1" => Work2;
                    "1" = {
                        "00+한글__한글_ADV" > "00+한글__Device2_ADV" > "00+한글__Device3_ADV" > "00+한글__Device4_ADV" > "00+한글__Device1_RET", "00+한글__Device2_RET", "00+한글__Device3_RET" > "00+한글__Device4_RET";
                    }
                }
                [jobs] = {
                    "00-한글__+11_RET" = { "00-한글__+11".RET(-, -); }
                    "00-한글__-11_RET" = { "00-한글__-11".RET(-, -); }
                    "00-한글__+11_ADV" = { "00-한글__+11".ADV(-, -); }
                    "00-한글__-11_ADV" = { "00-한글__-11".ADV(-, -); }
                    "00-한글__?11_ADV" = { "00-한글__?11".ADV(-, -); }
                    "00+한글__한글_ADV" = { "00+한글__한글".ADV(-, -); }
                    "00+한글__Device2_ADV" = { "00+한글__Device2".ADV(-, -); }
                    "00+한글__Device3_ADV" = { "00+한글__Device3".ADV(-, -); }
                    "00+한글__Device4_ADV" = { "00+한글__Device4".ADV(-, -); }
                    "00+한글__Device1_RET" = { "00+한글__Device1".RET(-, -); }
                    "00+한글__Device2_RET" = { "00+한글__Device2".RET(-, -); }
                    "00+한글__Device3_RET" = { "00+한글__Device3".RET(-, -); }
                    "00+한글__Device4_RET" = { "00+한글__Device4".RET(-, -); }
                    "00+한글__외부시작_ADV_INTrue" = { "00+한글__외부시작".ADV(-:Boolean:True, -); }
                }
                [buttons] = {
                    [a] = { AutoSelect(M00628, -) = { "00-한글"; "00+한글"; } }
                    [m] = { ManualSelect(M00629, -) = { "00-한글"; "00+한글"; } }
                    [d] = { DrivePushBtn(M0062A, -) = { "00-한글"; "00+한글"; } }
                    [e] = { EmergencyBtn(M0062D, -) = { "00-한글"; "00+한글"; } }
                    [p] = { PausePushBtn(M0062B, -) = { "00-한글"; "00+한글"; } }
                    [c] = { ClearPushBtn(M0062C, -) = { "00-한글"; "00+한글"; } }
                }
                [lamps] = {
                    [a] = { AutoModeLamp(-, -) = {  } }
                    [m] = { ManualModeLamp(-, -) = {  } }
                    [d] = { DriveLamp(-, -) = {  } }
                    [e] = { ErrorLamp(-, -) = {  } }
                    [r] = { ReadyStateLamp(-, -) = {  } }
                    [i] = { IdleModeLamp(-, -) = {  } }
                    [o] = { OriginStateLamp(-, -) = {  } }
                }
                [prop] = {
                    [layouts] = {
                        "00-한글__+11" = (588, 406, 220, 80);
                        "00-한글__-11" = (598, 687, 220, 80);
                        "00-한글__?11" = (588, 529, 220, 80);
                        "00+한글__한글" = (543, 298, 220, 80);
                        "00+한글__Device2" = (854, 580, 220, 80);
                        "00+한글__Device3" = (1154, 580, 220, 80);
                        "00+한글__Device4" = (854, 800, 220, 80);
                        "00+한글__Device1" = (554, 580, 220, 80);
                        "00+한글__외부시작" = (1103, 95, 220, 80);
                    }
                }
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] 
                    "00-한글__+11",
                    "00-한글__-11",
                    "00-한글__?11",
                    "00+한글__한글",
                    "00+한글__Device2",
                    "00+한글__Device3",
                    "00+한글__Device4",
                    "00+한글__Device1",
                    "00+한글__외부시작"; 
            }
            //DS Language Version = [1.0.0.1]
            //DS Library Date = [Library Release Date 24.3.26]
            //DS Engine Version = [0.9.8.24]
            """
        check testCodeAdvance
        
        