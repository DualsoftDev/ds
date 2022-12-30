namespace T

open System.IO

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.Common.QGraph
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common.FlatExpressionModule
open System.Globalization

type XgiGenerationTest() =
    inherit XgiTestClass()

    [<Test>]
    member __.``AndOr simple test`` () =
        let storages = Storages()
        let code = """
            bool myBit0 = createTag("%IX0.0.0", false);
            bool myBit1 = createTag("%IX0.0.1", false);
            bool myBit2 = createTag("%IX0.0.2", false);

            bool myBit7 = createTag("%QX0.1.0", false);

            $myBit7 := ($myBit0 || $myBit1) && $myBit2;
"""
        let statements = parseCode storages code
        storages.Count === 4
        statements.Length === 1      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``And Many test`` () =
        let storages = Storages()
        let code = codeForBits + """
            $myBit17 :=
                $myBit00 &&
                $myBit01 &&
                $myBit02 &&
                $myBit03 &&
                $myBit04 &&
                $myBit05 &&
                $myBit06 &&
                $myBit07 &&
                $myBit10 &&
                $myBit11 &&
                $myBit12 &&
                $myBit13 &&
                $myBit14 &&
                $myBit15 &&
                $myBit16
                ;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``OR Many test`` () =
        let storages = Storages()
        let code = codeForBits + """
            $myBit17 :=
                $myBit00 ||
                $myBit01 ||
                $myBit02 ||
                $myBit03 ||
                $myBit04 ||
                $myBit05 ||
                $myBit06 ||
                $myBit07 ||
                $myBit10 ||
                $myBit11 ||
                $myBit12 ||
                $myBit13 ||
                $myBit14 ||
                $myBit15 ||
                $myBit16
                ;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``AndOr2 test`` () =
        let storages = Storages()
        let code = codeForBits + """
            $myBit07 :=    (($myBit00 || $myBit01) && $myBit02)
                        ||  $myBit03
                        || ($myBit04 && $myBit05 && $myBit06)
                        ;
            $myBit17 :=    (($myBit10 && $myBit11) || $myBit12)
                        && $myBit13
                        && ($myBit14 || $myBit15 || $myBit16)
                        ;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml
        ()

    [<Test>]
    member __.``Atomic Negation test`` () =
        let myTagA = PlcTag("tag0", "%IX0.0.0", false)
        let myTagB = PlcTag("tag1", "%IX0.0.1", false)
        let pulse, negated = false, false
        let flatTerminal = FlatTerminal(myTagA, pulse, negated)
        let negatedFlatTerminal = flatTerminal.Negate()
        match negatedFlatTerminal with
        | FlatTerminal(t, p, n) -> n === true
        | _ -> failwith "ERROR"

        (* ! (A & B) === ! A || ! B) test *)
        let expAnd = FlatNary(And, [FlatTerminal(myTagA, pulse, negated); FlatTerminal(myTagB, pulse, negated)])
        let negatedAnd = expAnd.Negate()
        match negatedAnd with
        | FlatNary(Or, [FlatTerminal(_, _, negated1); FlatTerminal(_, _, negated2)]) ->
            negated1 === true
            negated2 === true
        | _ -> failwith "ERROR"


        (* ! (! A & B) === A || ! B) test *)
        let expAnd = FlatNary(And, [FlatTerminal(myTagA, pulse, true); FlatTerminal(myTagB, pulse, negated)])
        let negatedAnd = expAnd.Negate()
        match negatedAnd with
        | FlatNary(Or, [FlatTerminal(_, _, negated1); FlatTerminal(_, _, negated2)]) ->
            negated1 === false
            negated2 === true
        | _ -> failwith "ERROR"


        (* ! (! A & B) === A || ! B) test *)
        let expAnd = FlatNary(And, [FlatNary(Neg, [FlatTerminal(myTagA, false, false)]); FlatTerminal(myTagB, false, false)])
        let negatedAnd = expAnd.Negate()
        match negatedAnd with
        | FlatNary(Or, [FlatTerminal(_, _, negated1); FlatTerminal(_, _, negated2)]) ->
            negated1 === false
            negated2 === true
        | _ -> failwith "ERROR"

        ()

    [<Test>]
    member __.``Negation1 test`` () =
        let storages = Storages()
        let code = """
            bool myBit00 = createTag("%IX0.0.0", false);
            bool myBit01 = createTag("%IX0.0.1", false);

            $myBit01 := ! $myBit00;
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Negation2 test`` () =
        let storages = Storages()
        let code = """
            bool myBit00 = createTag("%IX0.0.0", false);
            bool myBit01 = createTag("%IX0.0.1", false);
            bool myBit02 = createTag("%IX0.0.2", false);
            bool myBit03 = createTag("%IX0.0.3", false);
            bool myBit04 = createTag("%IX0.0.4", false);
            bool myBit05 = createTag("%IX0.0.5", false);

            $myBit02 := ! ($myBit00 || $myBit01);
            $myBit05 := ! ($myBit03 && $myBit04);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml
        ()


    [<Test>]
    member __.``Negation3 test`` () =
        let storages = Storages()
        let code = """
            bool myBit00 = createTag("%IX0.0.0", false);
            bool myBit01 = createTag("%IX0.0.1", false);
            bool myBit02 = createTag("%IX0.0.2", false);
            bool myBit03 = createTag("%IX0.0.3", false);
            bool myBit04 = createTag("%IX0.0.4", false);
            bool myBit05 = createTag("%IX0.0.5", false);

            $myBit02 := ! (! $myBit00 || $myBit01);
            $myBit05 := ! ($myBit03 && ! $myBit04);
"""
        let statements = parseCode storages code
        let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
        saveTestResult (get_current_function_name()) xml

