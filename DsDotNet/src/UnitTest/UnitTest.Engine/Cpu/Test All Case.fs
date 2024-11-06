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


type XgxConvertDsCPU(target:PlatformTarget) =
    inherit EngineTestBaseClass()

    let getOutputPath testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgx/Xgi/Xmls/{testName}.xml")


    member __.``Test DS Case`` () =
        RuntimeDS.Package <- RuntimePackage.PLC

        let f = getFuncName()

        let testCode = """
             [sys] HelloDS = {
                [flow] STN1 = {
                    외부시작.ADV > Work1_1;
                    Work1 > Work3 => Work4 =|> 클리어;
                    Work1 => Work2 => Work4;
                    Work1 = {
                        Device1.ADV > Device1.RET;
                    }
                    [aliases] = {
                        Work1 = { Work1_1; }
                    }
                }
                [jobs] = {
                    STN1.Device1.ADV[N1(0,0)] =  (_, _);
                    STN1.Device1.RET[N1(0,0)] =  (_, _);
                    STN1.외부시작.ADV[N1(0,0)] = (_, _);
                }
                [prop] = {
                    [layouts] = {
                        STN1__Device1 = (402, 710, 220, 80);
                        STN1__외부시작 = (1103, 95, 220, 80);
                    }
                    [notrans] = {
                        STN1.Work2;
                    }
                }
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"]
                    STN1__Device1,
                    STN1__외부시작;
                [versions] = {
                    DS-Langugage-Version = 1.0.0.1;
                    DS-Engine-Version = 0.9.9.7;
                }
            }
            //DS Library Date = [Library Release Date 24.3.26]
            """

        let systemRepo = ShareableSystemRepository()
        let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        DsAddressModule.setMemoryIndex(0)
        ModelParser.ClearDicParsingText()
        let helperSys = ModelParser.ParseFromString(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))

        let xgxGenParams = XgxGenerationParameters.Create(target, helperSys, getOutputPath f)
        let result = exportXMLforLSPLC xgxGenParams
        result === result

type XgiConvertDsCPU() =
    inherit XgxConvertDsCPU(XGI)
    [<Test>] member __.``Test DS Code`` () = base.``Test DS Case``()


type XgkConvertDsCPU() =
    inherit XgxConvertDsCPU(XGK)
    [<Test>] member __.``Test DS Code`` () = base.``Test DS Case``()
