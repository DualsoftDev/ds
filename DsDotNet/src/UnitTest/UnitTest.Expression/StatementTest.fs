namespace T.Statement
open Dual.Common.UnitTest.FS

open NUnit.Framework

open T
open T.Expression
open Engine.Core
open Engine.Parser.FS
open Dual.Common.Core.FS

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
    member __.``CTU/TON WINDOWS parsing test`` () =
        use _ = setRuntimeTarget WINDOWS
        let tryParse = tryParseStatement4UnitTest WINDOWS storages >> Option.get
        let coutnerStatement:Statement = "ctu myCounter = createAbCTU(100u, false);" |> tryParse
        let counter = toCounter coutnerStatement
        let timerStatement2:Statement = "ton myTimer = createAbTON(100u, false);" |> tryParse

        let cs2:Statement = "ton mytimerAB = createAbTON(1000u, $tag1 || $tag2);" |> tryParse
        let timer = toTimer cs2


        let statements = [
            "ctu myCounter1 = createAbCTU(100u, false);"
            // "ctr myCtr1 = createAbCTR(100us, $tag1, $tag2)"  : AB does not support CTR
            "ton myTimer2 = createAbTON(100u, false);"
            "ton mytimer3 = createAbTON(1000u, $tag1 || $tag2);"
        ]
        for s in statements do
            (tryParse s).ToText() === s

        let fails = [
            "Counter declaration error"      , "ctu myCtu1 = createAbCTR(100u, $tag1, $tag2);"                  // 'Counter declaration error: ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)'
            "Counter declaration error"      , "ctu myCtu1 = createAbCTU(100u, $tag1, $tag1, $tag1, $tag1);"    // 'Counter declaration error: ctu myCounter = createCTU(100us, $tag1, $tag1, $tag1, $tag1)'
            "Unable to cast"                 , "ctu myCtu2 = createAbCTU(100u, $tagDouble);"                    // 'Unable to cast object of type 'DuTerminal[System.Double]' to type 'IExpression`1[System.Boolean]'.'
            "Unable to cast"                 , "ctu myTon2 = createAbTON(100u, $tag1);"                         // 'Unable to cast object of type 'DuFunction[Engine.Core.ExpressionModule+Timer]' to type 'Expression`1[Engine.Core.ExpressionModule+Counter]'.'
            "Failed to find"                 , "ctu myTon3 = createAbTON(100u, $undefinedTag);"                 // 'Failed to find variable/tag name in $undefinedTag'
            "Resolution Error"               , "ton myTon4 = createAbTON(1u, $tag1);"                           // 'Timer Resolution Error: Preset value should be larger than 20us'
        ]
        for (expectedFailMessage, failText) in fails do
            (fun () ->
                tracefn $"Checking {expectedFailMessage} for {failText}"
                tryParse failText |> ignore
            ) |> ShouldFailWithSubstringT expectedFailMessage




    [<Test>]
    member __.``CTU/TON WINDOWS, XGI parsing test`` () =
        let storages = prepareStorage()

        //let storages = storages.ToArray() |> map Tuple.ofKeyValuePair |> Tuple.toDictionary
        use _ = setRuntimeTarget WINDOWS
        let tryParse = tryParseStatement4UnitTest WINDOWS storages >> Option.get
        let coutnerStatement:Statement = "ctu myCounter = createWinCTU(100u, false, false);" |> tryParse
        let counter = toCounter coutnerStatement
        let timerStatement2:Statement = "ton myTimer = createWinTON(100u, false);" |> tryParse

        let cs2:Statement = "ton mytimerWin = createWinTON(1000u, $tag1 || $tag2);" |> tryParse
        let timer = toTimer cs2


        let statements = [
            "ctu myCounter1 = createWinCTU(100u, false, false);"
            "ctr myCtr1 = createWinCTR(100u, $tag1, $tag2);"
            "ton myTimer2 = createWinTON(100u, false);"
            "ton mytimer3 = createWinTON(1000u, $tag1 || $tag2);"
            //"ton mytimer4 = createWinTON(1000u, $tag1 || $tag2, $tag3)"
        ]
        for s in statements do
            (tryParse s).ToText() === s

        let fails = [
            "Counter declaration error"      , "ctu myCtu1 = createWinCTU(100u, $tag1, $tag1, $tag1, $tag1);"    // 'Counter declaration error: ctu myCounter = createCTU(100u, $tag1, $tag1, $tag1, $tag1)'
            "Unable to cast"                 , "ctu myCtu2 = createWinCTU(100u, $tagDouble, $tag1);"                    // 'Unable to cast object of type 'DuTerminal[System.Double]' to type 'IExpression`1[System.Boolean]'.'
            //// 예외나는데 ShouldFailWithSubstringT 에서 처리가  안되네요  ( 한글 Visual studio 이라 처리안됨 )
            ////        "The index was outside the range", "ctu myTon1 = createWinTON();"                                     // 'The index was outside the range of elements in the list. (Parameter 'n')'
            ////
            "Unable to cast"                 , "ctu myTon2 = createWinTON(100u, $tag1);"                         // 'Unable to cast object of type 'DuFunction[Engine.Core.ExpressionModule+Timer]' to type 'Expression`1[Engine.Core.ExpressionModule+Counter]'.'
            "Failed to find"                 , "ctu myTon3 = createWinTON(100u, $undefinedTag);"                 // 'Failed to find variable/tag name in $undefinedTag'
            "Resolution Error"               , "ton myTon4 = createWinTON(1u, $tag1);"                           // 'Timer Resolution Error: Preset value should be larger than 20us'
        ]
        for (expectedFailMessage, failText) in fails do
            (fun () ->
                tracefn $"Checking {expectedFailMessage} for {failText}"
                tryParse failText |> ignore
            ) |> ShouldFailWithSubstringT expectedFailMessage
    [<Test>]
    member __.``COPY statement parsing test`` () =
        let storages = Storages()
        let tCond = createTag("tagCondition", "%M1.1", false)
        let tTarget = createTag("tag1", "%M1.1", 99us)
        storages.Add(tCond.Name, tCond)
        storages.Add(tTarget.Name, tTarget)
        let text = "copyIf($tagCondition, 100us, $tag1);"
        let copyStatement:Statement = text |> tryParseStatement4UnitTest WINDOWS storages |> Option.get
        copyStatement.ToText() === text

        copyStatement.Do()
        tTarget.Value === 99us

        tCond.Value <- true
        copyStatement.Do()
        tTarget.Value === 100us
        ()


