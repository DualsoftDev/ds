namespace T.Comparison
open T

open NUnit.Framework

open Dual.UnitTest.Common.FS
open Dual.Common.Core.FS
open Engine.Parser.FS
open Engine.Core
open PLC.CodeGen.Common.FlatExpressionModule


type XgxNegationTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let baseCode =
            match xgx with
            | XGI -> """
                bool ix = createTag("%IX0.0.0", false);
                bool qx = createTag("%QX0.1.0", false);
                """
            | XGK -> """
                bool ix = createTag("P00000", false);
                bool qx = createTag("P00001", false);
                """
            | _ -> failwith "Not supported plc type"

    member __.``Atomic Negation test`` () =
        let myTagA = createTag("tag0", "%IX0.0.0", false)
        let myTagB = createTag("tag1", "%IX0.0.1", false)
        let pulse, negated = None, false
        let flatTerminal = FlatTerminal(myTagA, pulse, negated)
        let negatedFlatTerminal = flatTerminal.Negate()
        match negatedFlatTerminal with
        | FlatTerminal(t, p, n) -> n === true
        | _ -> failwithlog "ERROR"

        (* ! (A & B) === ! A || ! B) test *)
        let expAnd = FlatNary(And, [FlatTerminal(myTagA, pulse, negated); FlatTerminal(myTagB, pulse, negated)])
        let negatedAnd = expAnd.Negate()
        match negatedAnd with
        | FlatNary(Or, [FlatTerminal(_, _, negated1); FlatTerminal(_, _, negated2)]) ->
            negated1 === true
            negated2 === true
        | _ -> failwithlog "ERROR"


        (* ! (! A & B) === A || ! B) test *)
        let expAnd = FlatNary(And, [FlatTerminal(myTagA, pulse, true); FlatTerminal(myTagB, pulse, negated)])
        let negatedAnd = expAnd.Negate()
        match negatedAnd with
        | FlatNary(Or, [FlatTerminal(_, _, negated1); FlatTerminal(_, _, negated2)]) ->
            negated1 === false
            negated2 === true
        | _ -> failwithlog "ERROR"


        (* ! (! A & B) === A || ! B) test *)
        let expAnd = FlatNary(And, [FlatNary(Neg, [FlatTerminal(myTagA, None, false)]); FlatTerminal(myTagB, None, false)])
        let negatedAnd = expAnd.Negate()
        match negatedAnd with
        | FlatNary(Or, [FlatTerminal(_, _, negated1); FlatTerminal(_, _, negated2)]) ->
            negated1 === false
            negated2 === true
        | _ -> failwithlog "ERROR"

        ()

    member x.``Negation1 test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 2 + """
            $x01 = ! $x00;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Negation2 test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 6 + """
            $x02 = ! ($x00 || $x01);
            $x05 = ! ($x03 && $x04);
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml
        ()


    member x.``Negation3 test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 6 + """
            $x02 = ! (! $x00 || $x01);
            $x05 = ! ($x03 && ! $x04);
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``Negation x 1 test`` () =
        let storages = Storages()
        let code = baseCode + "$qx = !$ix;"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Negation x 2 test`` () =
        let storages = Storages()
        let code = baseCode + "$qx = !!$ix;"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Negation x 3 test`` () =
        let storages = Storages()
        let code = baseCode + "$qx = !!!$ix;"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Negation x 4 test`` () =
        let storages = Storages()
        let code = baseCode + "$qx = !!!!$ix;"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Negation x 5 test`` () =
        let storages = Storages()
        let code = baseCode + "$qx = !!!!!$ix;"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Negation x n test`` () =
        let storages = Storages()
        let testCode =
            """$qx =       !$ix
                    &&    !!$ix
                    &&   !!!$ix
                    &&  !!!!$ix
                    && !!!!!$ix;"""

        let code = baseCode + testCode

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``Negation, comparision test1`` () =
        let storages = Storages()
        let code = baseCode + """$qx =
        ( !(2 == 3) && !($ix != true) && !!!(3.14 > 5.0) )
            || ( !(true && false) && !!(true || $ix)  ) ;"""

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


//[<Collection("SerialXgxExpEqualityTest")>]
type XgiNegationTest() =
    inherit XgxNegationTest(XGI)
    [<Test>] member __.``Atomic Negation test`` () = base.``Atomic Negation test`` ()
    [<Test>] member x.``Negation1 test`` () = base.``Negation1 test`` ()
    [<Test>] member x.``Negation2 test`` () = base.``Negation2 test`` ()
    [<Test>] member x.``Negation3 test`` () = base.``Negation3 test`` ()
    [<Test>] member x.``Negation x 1 test`` () = base.``Negation x 1 test`` ()
    [<Test>] member x.``Negation x 2 test`` () = base.``Negation x 2 test`` ()
    [<Test>] member x.``Negation x 3 test`` () = base.``Negation x 3 test`` ()
    [<Test>] member x.``Negation x 4 test`` () = base.``Negation x 4 test`` ()
    [<Test>] member x.``Negation x 5 test`` () = base.``Negation x 5 test`` ()
    [<Test>] member x.``Negation x n test`` () = base.``Negation x n test`` ()
    [<Test>] member x.``Negation, comparision test1`` () = base.``Negation, comparision test1`` ()



type XgkNegationTest() =
    inherit XgxNegationTest(XGK)
    [<Test>] member __.``Atomic Negation test`` () = base.``Atomic Negation test`` ()
    [<Test>] member x.``Negation1 test`` () = base.``Negation1 test`` ()
    [<Test>] member x.``Negation2 test`` () = base.``Negation2 test`` ()
    [<Test>] member x.``Negation3 test`` () = base.``Negation3 test`` ()
    [<Test>] member x.``Negation x 1 test`` () = base.``Negation x 1 test`` ()
    [<Test>] member x.``Negation x 2 test`` () = base.``Negation x 2 test`` ()
    [<Test>] member x.``Negation x 3 test`` () = base.``Negation x 3 test`` ()
    [<Test>] member x.``Negation x 4 test`` () = base.``Negation x 4 test`` ()
    [<Test>] member x.``Negation x 5 test`` () = base.``Negation x 5 test`` ()
    [<Test>] member x.``Negation x n test`` () = base.``Negation x n test`` ()
    [<Test>] member x.``Negation, comparision test1`` () = base.``Negation, comparision test1`` ()
