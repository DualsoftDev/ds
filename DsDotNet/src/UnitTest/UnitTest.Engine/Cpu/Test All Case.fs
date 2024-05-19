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
    let myExistIO  = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgx/Xgi/Xmls/Templates/myTemplateExistIO.xml")

    let testAddressSetting (sys:DsSystem) =
        let mutable index = 0
        let addr() = index <- index+1
                     sprintf "%%MX%d" index
        for j in sys.Jobs do
            for dev in j.DeviceDefs  do
                    dev.InAddress <- ( addr())
                    dev.OutAddress <- ( addr())

        for b in sys.HWButtons do
            b.InAddress <- addr()
            b.OutAddress <- addr()

        for l in sys.HWLamps do
            l.OutAddress <-  addr()

        for c in sys.HWConditions do
            c.InAddress <-  addr()


    member __.``Test All Case`` () =
        let t = CpuTestSample(target)
        let f = getFuncName()
        let result = exportXMLforLSPLC(target, t.Sys, myTemplate f, None, 0, 0, 0)
        result === result

    member __.``Test DS Case`` () =
        let f = getFuncName()
        let testCode = """
             [sys] HelloDS = {
                [flow] STN1 = {
                    STN1_EXT_ADV > Work1;
                    Work1 = {
                        STN1_Device4_RET; 
                    }
                }
                [jobs] = {
                    STN1_EXT_ADV = { STN1_EXT.ADV(P0010:66, -); }
                    STN1_Device4_RET = { STN1_Device4.RET(P0020:300, -:200); }
                }
   
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_EXT; 
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] STN1_Device4; 
            }
            """

        let systemRepo = ShareableSystemRepository()
        let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        let helper = ModelParser.ParseFromString2(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
        
        let result = exportXMLforLSPLC(target, helper.TheSystem, myTemplate f, None, 0, 0, 0)
        result === result

type XgiConvertDsCPU() =
    inherit XgxConvertDsCPU(XGI)
    [<Test>] member __.``Test All Case`` () = base.``Test All Case``()
    [<Test>] member __.``Test DS Code`` () = base.``Test DS Case``()


type XgkConvertDsCPU() =
    inherit XgxConvertDsCPU(XGK)
    [<Test>] member __.``Test All Case`` () = base.``Test All Case``()
    [<Test>] member __.``Test DS Code`` () = base.``Test DS Case``()
