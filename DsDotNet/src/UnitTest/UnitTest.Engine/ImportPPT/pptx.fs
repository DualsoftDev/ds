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
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\1_System.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\2_Flow.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\3_Real.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\4_Api.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\5_Call.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\6_Alias.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\7_CopySystem.pptx");
            let result  = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\8_Safety.pptx");
            1  === 1
           