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


type XgxConvertDsCPU(target:PlatformTarget) =
    inherit EngineTestBaseClass()

    let myTemplate testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgx/Xgi/Xmls/{testName}.xml")


    member __.``Test DS Case`` () =
        RuntimeDS.Package <- RuntimePackage.PLC
        
        let f = getFuncName()
        let testCode = """
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

        let systemRepo = ShareableSystemRepository()
        let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        DsAddressModule.setMemoryIndex(0) 
        ModelParser.ClearDicParsingText() 
        let helper = ModelParser.ParseFromString2(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
        
        let result = exportXMLforLSPLC(target, helper.TheSystem, myTemplate f, None, 0, 0)
        result === result

type XgiConvertDsCPU() =
    inherit XgxConvertDsCPU(XGI)
    [<Test>] member __.``Test DS Code`` () = base.``Test DS Case``()


type XgkConvertDsCPU() =
    inherit XgxConvertDsCPU(XGK)
    [<Test>] member __.``Test DS Code`` () = base.``Test DS Case``()
