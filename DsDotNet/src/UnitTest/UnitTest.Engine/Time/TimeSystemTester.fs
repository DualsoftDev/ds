namespace T

open Dual.Common.UnitTest.FS
open Engine.Core
open NUnit.Framework
open System.Linq
open Engine.Parser.FS

[<AutoOpen>]
module TimeSystemTestModule =

    type TimeSystemTest() =

        
        let systemRepo = ShareableSystemRepository()
        let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        let helper testCode =
            let system = ModelParser.ParseFromString(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
            system.VertexAddRemoveHandlers <- None
            system
            
        [<Test>]
        member _.``System Api GetDuration`` () =

            let testCode = """
            [sys] DoubleCylinder = {
                [flow] FLOW = {
                    ADV <|> RET;
                }
                [interfaces] = {
                    "+" = { FLOW.ADV ~ FLOW.ADV }
                    "-" = { FLOW.RET ~ FLOW.RET }
                }
            }
            """
            let sys = helper testCode 
            if(sys.ApiItems.Any())
            then
                for api in sys.ApiItems do
                    TimeExt.GetDuration(api) |> ignore

