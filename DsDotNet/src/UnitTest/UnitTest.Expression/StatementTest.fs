namespace T.Statement

open NUnit.Framework

open T
open T.Expression
open Engine.Core
open Engine.Parser.FS
open Engine.Common.FS

type StatementTest() =
    inherit ExpressionTestBaseClass()

    let prepareStorage() =
        let storages = Storages()
        let t1 = createTag("my_counter_control_tag", "%M1.1", false)
        let tag1 = createTag("tag1", "%M1.1", false)
        let tag2 = createTag("tag2", "%M1.1", false)
        let tag3 = createTag("tag3", "%M1.1", false)
        let tagDouble = createTag("tagDouble", "%M1.1", 3.14)
        for t in [t1 :> IStorage; tag1; tag2; tag3; tagDouble] do
            storages.Add(t.Name, t)
        storages

    let storages = prepareStorage()

    [<Test>]
    member __.``CTU/TON AB parsing test`` () =
        use _ = setRuntimeTarget AB
        let coutnerStatement:Statement = "ctu myCounter = createAbCTU(100us, false)" |> tryParseStatement storages |> Option.get
        let counter = toCounter coutnerStatement
        let timerStatement2:Statement = "ton myTimer = createAbTON(100us, false)" |> tryParseStatement storages |> Option.get

        let cs2:Statement = "ton mytimer = createAbTON(1000us, $tag1 || $tag2)" |> tryParseStatement storages |> Option.get
        let timer = toTimer cs2


        let statements = [
            "ctu myCounter1 = createAbCTU(100us, false)"
            // "ctr myCtr1 = createAbCTR(100us, $tag1, $tag2)"  : AB does not support CTR
            "ton myTimer2 = createAbTON(100us, false)"
            "ton mytimer3 = createAbTON(1000us, $tag1 || $tag2)"
        ]
        for s in statements do
            (tryParseStatement storages s |> Option.get).ToText() === s

        let fails = [
            "Counter declaration error"      , "ctu myCtu1 = createAbCTR(100us, $tag1, $tag2)"                  // 'Counter declaration error: ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)'
            "Counter declaration error"      , "ctu myCtu1 = createAbCTU(100us, $tag1, $tag1, $tag1, $tag1)"    // 'Counter declaration error: ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)'
            "Unable to cast"                 , "ctu myCtu2 = createAbCTU(100us, $tagDouble)"                    // 'Unable to cast object of type 'DuTerminal[System.Double]' to type 'IExpression`1[System.Boolean]'.'
            "Unable to cast"                 , "ctu myTon2 = createAbTON(100us, $tag1)"                         // 'Unable to cast object of type 'DuFunction[Engine.Core.ExpressionModule+Timer]' to type 'Expression`1[Engine.Core.ExpressionModule+Counter]'.'
            "Failed to find"                 , "ctu myTon3 = createAbTON(100us, $undefinedTag)"                 // 'Failed to find variable/tag name in $undefinedTag'
            "Resolution Error"               , "ton myTon4 = createAbTON(1us, $tag1)"                           // 'Timer Resolution Error: Preset value should be larger than 20us'
        ]
        for (expectedFailMessage, failText) in fails do
            (fun () ->
                tracefn $"Checking {expectedFailMessage} for {failText}"
                failText |> tryParseStatement storages |> ignore
            ) |> ShouldFailWithSubstringT expectedFailMessage




    [<Test>]
    member __.``CTU/TON WINDOWS, XGI parsing test`` () =
        let storages = prepareStorage()

        //let storages = storages.ToArray() |> map Tuple.ofKeyValuePair |> Tuple.toDictionary
        use _ = setRuntimeTarget WINDOWS
        let coutnerStatement:Statement = "ctu myCounter = createWinCTU(100us, false, false)" |> tryParseStatement storages|> Option.get
        let counter = toCounter coutnerStatement
        let timerStatement2:Statement = "ton myTimer = createWinTON(100us, false)" |> tryParseStatement storages|> Option.get

        let cs2:Statement = "ton mytimer = createWinTON(1000us, $tag1 || $tag2)" |> tryParseStatement storages |> Option.get
        let timer = toTimer cs2


        let statements = [
            "ctu myCounter1 = createWinCTU(100us, false, false)"
            "ctr myCtr1 = createWinCTR(100us, $tag1, $tag2)"
            "ton myTimer2 = createWinTON(100us, false)"
            "ton mytimer3 = createWinTON(1000us, $tag1 || $tag2)"
            //"ton mytimer4 = createWinTON(1000us, $tag1 || $tag2, $tag3)"
        ]
        for s in statements do
            (tryParseStatement storages s |> Option.get).ToText() === s

        let fails = [
            "Counter declaration error"      , "ctu myCtu1 = createWinCTU(100us, $tag1, $tag1, $tag1, $tag1)"    // 'Counter declaration error: ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)'
            "Unable to cast"                 , "ctu myCtu2 = createWinCTU(100us, $tagDouble, $tag1)"                    // 'Unable to cast object of type 'DuTerminal[System.Double]' to type 'IExpression`1[System.Boolean]'.'
            ////<<help kwak>> 예외나는데 ShouldFailWithSubstringT 에서 처리가  안되네요  ( 한글 Visual studio 이라 처리안됨 )
            ////        "The index was outside the range", "ctu myTon1 = createWinTON()"                                     // 'The index was outside the range of elements in the list. (Parameter 'n')'
            ////<<help kwak>>
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
        let tCond = createTag("tagCondition", "%M1.1", false)
        let tTarget = createTag("tag1", "%M1.1", 99us)
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


