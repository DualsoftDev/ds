namespace UnitTest.Engine

open System
open Engine.Core
open NUnit.Framework
open Engine.Cpu.Expression
open System.IO
open Model.Import.Office

[<AutoOpen>]
module PPTTestModule =
    let toString x = x.ToString()
    type PPTTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``EveryScenarioPPT test`` () =
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\1.System.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\2.Flow.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\3.Real.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\4.Api.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\5.Call.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\6.Alias.pptx");
            1  === 1
           