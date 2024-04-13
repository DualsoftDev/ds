namespace T
open Dual.UnitTest.Common.FS

open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common.FlatExpressionModule

type XgxGenerationTest(xgx:RuntimeTargetType) =
    inherit XgxTestBaseClass(xgx)

    member x.``OR simple test`` () =
        let storages = Storages()
        let code = """
            bool x0 = createTag("%IX0.0.0", false);
            bool x1 = createTag("%IX0.0.1", false);

            bool x7 = createTag("%QX0.1.0", false);

            $x7 := ($x0 || $x1);
"""
        let statements = parseCode storages code
        storages.Count === 3
        statements.Length === 1      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``AndOr simple test`` () =
        let storages = Storages()
        let code = """
            bool x0 = createTag("%IX0.0.0", false);
            bool x1 = createTag("%IX0.0.1", false);
            bool x2 = createTag("%IX0.0.2", false);

            bool x7 = createTag("%QX0.1.0", false);

            $x7 := ($x0 || $x1) && $x2;
"""
        let statements = parseCode storages code
        storages.Count === 4
        statements.Length === 1      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``And Many test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 :=
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 && $x10 &&
                $x11 && $x12 && $x13 && $x14
                ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``And Huge simple test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = generateLargeVariableDeclarations xgx + """
                $x15 :=
                    $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                    $x10 && $x11 && $x12 && $x13 && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                    $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                    $x30 && $x31 &&
                    $x32 && $x33 && $x34 && $x35 && $x36 && $x37 //&& $x38 && $x39
                    ;
"""
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )
    member x.``And Huge test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = generateLargeVariableDeclarations xgx + """
                $x16 :=
                    ($nn1 > $nn2) &&
                    $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                    $x10 && $x11 && $x12 && $x13 && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                    $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                    $x30 && ($nn1 > $nn2) &&
                    $x32 && $x33 && $x34 && $x35 && $x36 && $x37 //&& $x38 && $x39
                    ;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )

    member x.``And Huge test2`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = generateLargeVariableDeclarations xgx + """
                $x16 :=
                    (($nn1 + $nn2) > $nn3) && (($nn4 - $nn5 + $nn6) > $nn7) &&
                    $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                    $x10 && $x11 && $x12 && $x13 && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                    $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                    $x30 && ($nn1 > $nn2) &&
                    $x32 && $x33 && $x34 && $x35 && $x36 && $x37 //&& $x38 && $x39
                    ;
    """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )

    member x.``And Huge test 3`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = generateLargeVariableDeclarations xgx + """
                $x15 :=
                    ($x00 || $x01 || $x02 || $x03) && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                    ($x10 && $x11 || $x12 && $x13) && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                    $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                    $x30 && $x31 &&
                    ($x32 || $x33 && $x34 || $x35) && $x36 && $x37 && $x38 && $x39 &&
                    ($x00 || $x01 || $x02 || $x03) && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                    ($x10 && $x11 || $x12 && $x13) && $x14 && $x15 && $x16 && $x17 && $x18 && $x19
                    ;
"""
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )

    member x.``And Huge test 4`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code =
                let vars =
                    [
                        yield! ["_OFF"; "_ON"; "R"]
                        for i in [1..50] do
                            yield $"c{i}"
                    ] |> distinct

                let counter = counterGenerator 0
                let varDecls = vars |> map (fun v -> $"""bool {v} = createTag("%%MX{counter()}", false);""") |> String.concat "\n"
                let c1_to_c38 = [1..38] |> map (fun i -> $"""$c{i}""") |> String.concat " && "
                varDecls + $"""
                $R := 
                    (   (    $_OFF
                          || ({c1_to_c38})
                          || $c39
                          || $c40
                          || $_OFF )
                        && $_ON
                        || $c41
                    )
                    && ! ($c42 && !($c43) && !($c44) && !($c45) || $c46);
                """
            let statements = parseCode storages code
            let f = getFuncName()
            let xml = x.generateXmlForTest f storages (map withNoComment statements)
            x.saveTestResult f xml
        )

    member x.``OR Many test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 :=
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09 ||
                $x10 || $x11 || $x12 || $x13 || $x14
                ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``OR Huge test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 32  + """
            $x15 :=
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09 ||
                $x10 || $x11 || $x12 || $x13 || $x14 || $x15 || $x16 || $x17 || $x18 || $x19 ||
                $x20 || $x21 || $x22 || $x23 || $x24 || $x25 || $x26 || $x27 || $x28 || $x29 ||
                $x30 || $x31 ||
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09
                ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``OR variable length 역삼각형 test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 :=
                $x00
                || ( ( $x01 || $x02 ) && $x03 )
                ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``OR Block test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 :=
                $x01 && ($x02 || ($x03 && ($x04 || $x05 || $x06 || $x07) && $x08 && ($x09 || $x10))) 
                ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``OR Block test2`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 32  + """
            $x31 :=
                $x02
                && ($x03 || $x04 || $x05)
                && ($x06 || $x07)
                && $x08
                && ($x09
                      || ($x10 && ( $x11
                                    || $x12
                                    || ($x13 && $x14 && $x15)
                                    || !($x16 && $x17 && $x18))
                               && $x19) )
                ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``OR variable length test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x07 :=    (($x00 || $x01) && $x02)
                        ||  $x03
                        || ($x04 && $x05 && $x06)
                        ;

            $x15 :=
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x00
                ;
            $x15 :=
                $x00
                || ( $x01 && $x02 )
                || ( $x03 && $x04 && $x05 )
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x00
                ;

            $x15 :=
                (
                    $x00
                    || ( $x10 && $x11 && $x12 && $x13 && $x14)
                    || ( $x06 && $x07 && $x08 && $x09 )
                    || ( $x03 && $x04 && $x05 )
                    || ( $x01 && $x02 )
                    || $x00
                ) &&
                (
                    $x00
                    || ( $x01 && $x02 )
                    || ( $x03 && $x04 && $x05 )
                    || ( $x06 && $x07 && $x08 && $x09 )
                    || ( $x10 && $x11 && $x12 && $x13 && $x14)
                    || ( $x06 && $x07 && $x08 && $x09 )
                    || ( $x03 && $x04 && $x05 )
                    || ( $x01 && $x02 )
                    || $x00
                )
                ;

"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml


    member x.``AndOr2 test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x07 :=    (($x00 || $x01) && $x02)
                        ||  $x03
                        || ($x04 && $x05 && $x06)
                        ;
            $x15 :=    (($x08 && $x09) || $x10)
                        && $x11
                        && ($x12 || $x13 || $x14)
                        ;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml
        ()

    member __.``Atomic Negation test`` () =
        let myTagA = createTag("tag0", "%IX0.0.0", false)
        let myTagB = createTag("tag1", "%IX0.0.1", false)
        let pulse, negated = false, false
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
        let expAnd = FlatNary(And, [FlatNary(Neg, [FlatTerminal(myTagA, false, false)]); FlatTerminal(myTagB, false, false)])
        let negatedAnd = expAnd.Negate()
        match negatedAnd with
        | FlatNary(Or, [FlatTerminal(_, _, negated1); FlatTerminal(_, _, negated2)]) ->
            negated1 === false
            negated2 === true
        | _ -> failwithlog "ERROR"

        ()

    member x.``Negation1 test`` () =
        let storages = Storages()
        let code = """
            bool x00 = createTag("%IX0.0.0", false);
            bool x01 = createTag("%IX0.0.1", false);

            $x01 := ! $x00;
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Negation2 test`` () =
        let storages = Storages()
        let code = """
            bool x00 = createTag("%IX0.0.0", false);
            bool x01 = createTag("%IX0.0.1", false);
            bool x02 = createTag("%IX0.0.2", false);
            bool x03 = createTag("%IX0.0.3", false);
            bool x04 = createTag("%IX0.0.4", false);
            bool x05 = createTag("%IX0.0.5", false);

            $x02 := ! ($x00 || $x01);
            $x05 := ! ($x03 && $x04);
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml
        ()


    member x.``Negation3 test`` () =
        let storages = Storages()
        let code = """
            bool x00 = createTag("%IX0.0.0", false);
            bool x01 = createTag("%IX0.0.1", false);
            bool x02 = createTag("%IX0.0.2", false);
            bool x03 = createTag("%IX0.0.3", false);
            bool x04 = createTag("%IX0.0.4", false);
            bool x05 = createTag("%IX0.0.5", false);

            $x02 := ! (! $x00 || $x01);
            $x05 := ! ($x03 && ! $x04);
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``Add test`` () =
        let storages = Storages()

        let code = """
            int16 nn0 = 0s;
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;
            int16 nn4 = 4s;
            int16 nn5 = 5s;
            bool qq = createTag("%QX0.1.0", false);

            //$qq := add($nn1, $nn2) > 3s;
            //$qq := ($nn1 + $nn2) * 9s + $nn3 > 3s;
            $qq := true && (($nn1 + $nn2) * 9s + $nn3 > 3s);
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml



    member x.``COPY test`` () =
        let storages = Storages()

        let code = """
            bool cond = false;
            int16 src = 1s;
            int16 tgt = 2s;

            copyIf($cond, $src, $tgt);
"""
        let statements = parseCode storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml





//[<Collection("SerialXgxGenerationTest")>]
type XgiGenerationTest() =
    inherit XgxGenerationTest(XGI)

    [<Test>] member x.``OR simple test`` () = base.``OR simple test`` ()
    [<Test>] member x.``AndOr simple test`` () = base.``AndOr simple test`` ()
    [<Test>] member x.``And Many test`` () = base.``And Many test`` ()
    [<Test>] member x.``And Huge simple test`` () = base.``And Huge simple test`` ()
    [<Test>] member x.``And Huge test`` () = base.``And Huge test`` ()
    [<Test>] member x.``And Huge test2`` () = base.``And Huge test2`` ()
    [<Test>] member x.``And Huge test 3`` () = base.``And Huge test 3`` ()
    [<Test>] member x.``And Huge test 4`` () = base.``And Huge test 4`` ()
    [<Test>] member x.``OR Many test`` () = base.``OR Many test`` ()
    [<Test>] member x.``OR Huge test`` () = base.``OR Huge test`` ()
    [<Test>] member x.``OR variable length 역삼각형 test`` () = base.``OR variable length 역삼각형 test`` ()
    [<Test>] member x.``OR Block test`` () = base.``OR Block test`` ()
    [<Test>] member x.``OR Block test2`` () = base.``OR Block test2`` ()
    [<Test>] member x.``OR variable length test`` () = base.``OR variable length test`` ()
    [<Test>] member x.``AndOr2 test`` () = base.``AndOr2 test`` ()
    [<Test>] member __.``Atomic Negation test`` () = base.``Atomic Negation test`` ()
    [<Test>] member x.``Negation1 test`` () = base.``Negation1 test`` ()
    [<Test>] member x.``Negation2 test`` () = base.``Negation2 test`` ()
    [<Test>] member x.``Negation3 test`` () = base.``Negation3 test`` ()
    [<Test>] member x.``Add test`` () =  base.``Add test`` ()
    [<Test>] member x.``COPY test`` () = base.``COPY test`` ()

