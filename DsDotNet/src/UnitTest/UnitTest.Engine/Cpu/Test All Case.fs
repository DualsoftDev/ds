namespace T.CPU
open Dual.UnitTest.Common.FS

open System.IO
open NUnit.Framework

open T
open Engine.Core
open Dual.Common.Core.FS
open Engine.CodeGenPLC
open Engine.CodeGenCPU


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


type XgiConvertDsCPU() =
    inherit XgxConvertDsCPU(XGI)
    [<Test>] member __.``Test All Case`` () = base.``Test All Case``()


type XgkConvertDsCPU() =
    inherit XgxConvertDsCPU(XGK)
    [<Test>] member __.``Test All Case`` () = base.``Test All Case``()

