namespace T

open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common.FlatExpressionModule

[<Collection("SerialXgiGenerationTest")>]
type XgiGenerationTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``OR simple test`` () =
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

        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``AndOr simple test`` () =
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

        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``And Many test`` () =
        let storages = Storages()
        let code = codeForBits + """
            $x15 :=
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 && $x10 &&
                $x11 && $x12 && $x13 && $x14
                ;
"""
        let statements = parseCode storages code
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member x.``And Huge simple test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = codeForBitsHuge + """
                $x15 :=
                    $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                    $x10 && $x11 && $x12 && $x13 && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                    $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                    $x30 && $x31 &&
                    $x32 && $x33 && $x34 && $x35 && $x36 && $x37 //&& $x38 && $x39
                    ;
"""
            let statements = parseCode storages code
            let xml = XgiFixtures.generateXml storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml
        )
    [<Test>]
    member x.``And Huge test`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = codeForBitsHuge + """
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
            let xml = XgiFixtures.generateXml storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml
        )

    [<Test>]
    member x.``And Huge test2`` () =
        lock x.Locker (fun () ->
            autoVariableCounter <- 0
            let storages = Storages()
            let code = codeForBitsHuge + """
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
            let xml = XgiFixtures.generateXml storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml
        )
    [<Test>]
    member __.``OR Many test`` () =
        let storages = Storages()
        let code = codeForBits + """
            $x15 :=
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09 ||
                $x10 || $x11 || $x12 || $x13 || $x14
                ;
"""
        let statements = parseCode storages code
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``OR Huge test`` () =
        let storages = Storages()
        let code = codeForBits31 + """
            $x15 :=
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09 ||
                $x10 || $x11 || $x12 || $x13 || $x14 || $x15 || $x16 || $x17 || $x18 || $x19 ||
                $x20 || $x21 || $x22 || $x23 || $x24 || $x25 || $x26 || $x27 || $x28 || $x29 ||
                $x30 || $x31 ||
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09
                ;
"""
        let statements = parseCode storages code
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``OR variable length 역삼각형 test`` () =
        let storages = Storages()
        let code = codeForBits + """
            $x15 :=
                $x00
                || ( ( $x01 || $x02 ) && $x03 )
                ;
"""
        let statements = parseCode storages code
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``OR variable length test`` () =
        let storages = Storages()
        let code = codeForBits + """
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
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``AndOr2 test`` () =
        let storages = Storages()
        let code = codeForBits + """
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
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml
        ()

    [<Test>]
    member __.``Atomic Negation test`` () =
        let myTagA = Tag("tag0", "%IX0.0.0", false)
        let myTagB = Tag("tag1", "%IX0.0.1", false)
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

    [<Test>]
    member __.``Negation1 test`` () =
        let storages = Storages()
        let code = """
            bool x00 = createTag("%IX0.0.0", false);
            bool x01 = createTag("%IX0.0.1", false);

            $x01 := ! $x00;
"""
        let statements = parseCode storages code
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Negation2 test`` () =
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
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml
        ()


    [<Test>]
    member __.``Negation3 test`` () =
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
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Add test`` () =
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
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml



    [<Test>]
    member __.``COPY test`` () =
        let storages = Storages()

        let code = """
            bool cond = false;
            int16 src = 1s;
            int16 tgt = 2s;

            copyIf($cond, $src, $tgt);
"""
        let statements = parseCode storages code
        let xml = XgiFixtures.generateXml storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

