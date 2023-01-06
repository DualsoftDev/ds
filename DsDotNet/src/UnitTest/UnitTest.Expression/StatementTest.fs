namespace T.Statement

open NUnit.Framework

open T
open T.Expression
open Engine.Core
open Engine.Parser.FS
open Engine.Common.FS

//[<AutoOpen>]
//module StatementTestModule =

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
            use _ = setRuntimeTarget WINDOWS
            let coutnerStatement:Statement = "ctu myCounter = createWinCTU(100us, false)" |> tryParseStatement storages |> Option.get
            let counter = toCounter coutnerStatement
            let timerStatement2:Statement = "ton myTimer = createWinTON(100us, false)" |> tryParseStatement storages |> Option.get

            let cs2:Statement = "ton mytimer = createWinTON(1000us, $tag1 || $tag2)" |> tryParseStatement storages |> Option.get
            let timer = toTimer cs2


            let statements = [
                "ctu myCounter1 = createWinCTU(100us, false)"
                "ctr myCtr1 = createWinCTR(100us, $tag1)"
                "ton myTimer2 = createWinTON(100us, false)"
                "ton mytimer3 = createWinTON(1000us, $tag1 || $tag2)"
                //"ton mytimer4 = createWinTON(1000us, $tag1 || $tag2, $tag3)"
            ]
            for s in statements do
                (tryParseStatement storages s |> Option.get).ToText() === s

            let fails = [
                "Counter declaration error"      , "ctu myCtu1 = createWinCTU(100us, $tag1, $tag1, $tag1, $tag1)"    // 'Counter declaration error: ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)'
                "Unable to cast"                 , "ctu myCtu2 = createWinCTU(100us, $tagDouble)"                    // 'Unable to cast object of type 'DuTerminal[System.Double]' to type 'IExpression`1[System.Boolean]'.'
        //<<help kwak>> 예외나는데 ShouldFailWithSubstringT 에서 처리가  안되네요  ( 한글 Visual studio 이라 처리안됨 ) 
        //        "The index was outside the range", "ctu myTon1 = createWinTON()"                                     // 'The index was outside the range of elements in the list. (Parameter 'n')'
        //<<help kwak>>
                "Unable to cast"                 , "ctu myTon2 = createWinTON(100us, $tag1)"                         // 'Unable to cast object of type 'DuFunction[Engine.Core.ExpressionModule+Timer]' to type 'Expression`1[Engine.Core.ExpressionModule+Counter]'.'
                "Failed to find"                 , "ctu myTon3 = createWinTON(100us, $undefinedTag)"                 // 'Failed to find variable/tag name in $undefinedTag'
                "Resolution Error"               , "ton myTon4 = createWinTON(1us, $tag1)"                           // 'Timer Resolution Error: Preset value should be larger than 20us'
            ]
            for (expectedFailMessage, failText) in fails do
                (fun () ->
                    tracefn $"Checking {expectedFailMessage} for {failText}"
                    failText |> tryParseStatement storages |> ignore
                ) |> ShouldFailWithSubstringT expectedFailMessage


        [<Test>]
        member __.``COPY statement parsing test`` () =
            let storages = Storages()
            let tCond = PlcTag("tagCondition", "%M1.1", false)
            let tTarget = PlcTag("tag1", "%M1.1", 99us)
            storages.Add(tCond.Name, tCond)
            storages.Add(tTarget.Name, tTarget)
            let text = "copyIf($tagCondition, 100us, $tag1)"
            let copyStatement:Statement = text |> tryParseStatement storages |> Option.get
            copyStatement.ToText() === text

            copyStatement.Do()
            tTarget.Value === 99us

            tCond.Value <- true
            copyStatement.Do()
            tTarget.Value === 100us
            ()


