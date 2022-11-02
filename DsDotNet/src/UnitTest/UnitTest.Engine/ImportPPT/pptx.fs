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
            let path = $"{__SOURCE_DIRECTORY__}\\2.Call.pptx"
            let result  = ImportM.FromPPTX(path);
            1  === 1
           