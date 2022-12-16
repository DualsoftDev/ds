namespace UnitTest.Engine.Statement

open NUnit.Framework

open UnitTest.Engine
open UnitTest.Engine.Expression
open Engine.Core
open Engine.Parser.FS.ExpressionParser
open Engine.Parser.FS

[<AutoOpen>]
module StatementTestModule =

    type StatementTest() =
        do Fixtures.SetUpTest()

        let storages = Storages()
        let t1 = PlcTag("my_counter_control_tag", "%M1.1", false)
        let tag1 = PlcTag("tag1", "%M1.1", false)
        let tag2 = PlcTag("tag2", "%M1.1", false)
        let tag3 = PlcTag("tag3", "%M1.1", false)
        let tagDouble = PlcTag("tagDouble", "%M1.1", 3.14)
        do
            for t in [t1 :> IStorage; tag1; tag2; tag3; tagDouble] do
                storages.Add(t.Name, t)

        [<Test>]
        member __.``CTU/TON parsing test`` () =
            let coutnerStatement:Statement = "ctu myCounter = createCTU(100us, false)" |> parseStatement storages |> Option.get
            let counter = toCounter coutnerStatement
            let timerStatement2:Statement = "ton myTimer = createTON(100us, false)" |> parseStatement storages |> Option.get

            let cs2:Statement = "ton mytimer = createTON(1000us, $tag1 || $tag2)" |> parseStatement storages |> Option.get
            let timer = toTimer cs2


            let statements = [
                "ctu myCounter = createCTU(100us, false)"
                "ton myTimer = createTON(100us, false)"
                "ton mytimer = createTON(1000us, $tag1 || $tag2)"
                "ton mytimer = createTON(1000us, $tag1 || $tag2, $tag3)"
            ]
            for s in statements do
                (parseStatement storages s |> Option.get).ToText() === s

            let fails = [
                "Counter declaration error"      , "ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)"    // 'Counter declaration error: ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)'
                "The index was outside the range", "ctu myCounter = createTON()"                                     // 'The index was outside the range of elements in the list. (Parameter 'n')'
                "Unable to cast"                 , "ctu myCounter = createCTU(100us, $tagDouble)"                    // 'Unable to cast object of type 'DuTerminal[System.Double]' to type 'IExpression`1[System.Boolean]'.'
                "Unable to cast"                 , "ctu myCounter = createTON(100us, $tag1)"                         // 'Unable to cast object of type 'DuFunction[Engine.Core.ExpressionModule+Timer]' to type 'Expression`1[Engine.Core.ExpressionModule+Counter]'.'
                "Failed to find"                 , "ctu myCounter = createTON(100us, $undefinedTag)"                 // 'Failed to find variable/tag name in $undefinedTag'
            ]
            for (expectedFailMessage, failText) in fails do
                (fun () -> failText |> parseStatement storages |> ignore) |> ShouldFailWithSubstringT expectedFailMessage




