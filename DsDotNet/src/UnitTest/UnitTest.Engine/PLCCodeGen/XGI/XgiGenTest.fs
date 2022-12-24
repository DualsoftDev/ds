namespace T.PLC.XGI

open System.IO
open System.Reflection

open NUnit.Framework

open T
open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.Common.QGraph
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common.FlatExpressionModule
open System.Text.RegularExpressions


//[<AutoOpen>]
//module XGI =

    type XgiGenerationTest() =
        do Fixtures.SetUpTest()

        let projectDir =
            let src = __SOURCE_DIRECTORY__
            let key = @"UnitTest\UnitTest.Engine"
            let tail = src.IndexOf(key) + key.Length
            src.Substring(0, tail)
        let xmlDir = Path.Combine(projectDir, "XgiXmls")
        let xmlAnswerDir = Path.Combine(xmlDir, "Answers")

        let saveTestResult testFunctionName xml =
            File.WriteAllText($@"{xmlDir}\{testFunctionName}.xml", xml)
            let answerXml = File.ReadAllText($@"{xmlAnswerDir}\{testFunctionName}.xml")
            answerXml === xml

        (* 테스트 수행 후, XG5000 에서 Project > Open project 를 누르고, outputFile 을 지정하여 open 한다.
            XG5000 의 project 탐색 창의 Scan Program / DsLogic / Program 을 double click 하여 생성된 rung 을 육안 검사한다.
         *)
        let outputFile = "C:/a.xml"

        let plcCodeGenerationOption =
            let n (v:IVertex) = v :?> INamed |> name
            /// 테스트 용으로 출력 신호를 따로 생성함.  e.g "Ap" --> "QAp"
            let coilGenerator          = fun (v:IVertex) -> $"O_{n v}"
            let SensorGenerator        = fun (v:IVertex) -> $"I_{n v}"
            let runningGenerator       = fun (v:IVertex) -> $"{n v}_G"
            let finishGenerator        = fun (v:IVertex) -> $"{n v}_F"
            let resetGenerator         = fun (v:IVertex) -> $"{n v}_RST"
            let readyStateGenerator    = fun (v:IVertex) -> $"{n v}_R"
            let HomingStateGenerator   = fun (v:IVertex) -> $"{n v}_H"
            let OriginStateGenerator   = fun (v:IVertex) -> $"{n v}_O"
            let goinglockNameGenerator = fun (v:IVertex) (i:int) -> $"{n v}_GL{id}"

            { createDefaultCodeGenerationOption() with
                CoilTagGenerator            = Some coilGenerator
                SensorTagGenerator          = Some SensorGenerator
                GoingStateNameGenerator     = Some runningGenerator
                StandbyStateNameGenerator   = Some readyStateGenerator
                HomingStateNameGenerator    = Some HomingStateGenerator
                OriginStateNameGenerator    = Some OriginStateGenerator
                ResetLockRelayNameGenerator = Some goinglockNameGenerator
                ResetNameGenerator          = Some resetGenerator
                RelayGenerator              = relayGenerator "RR" 1// XGI 고려한 모델 : Relay 이름 R 대신 RR
                //FinishStateNameGenerator  = Some finishGenerator
            }

        let codeForBits = """
            bool myBit00 = createTag("%IX0.0.0", false);
            bool myBit01 = createTag("%IX0.0.1", false);
            bool myBit02 = createTag("%IX0.0.2", false);
            bool myBit03 = createTag("%IX0.0.3", false);
            bool myBit04 = createTag("%IX0.0.4", false);
            bool myBit05 = createTag("%IX0.0.5", false);
            bool myBit06 = createTag("%IX0.0.6", false);
            bool myBit07 = createTag("%IX0.0.7", false);

            bool myBit10 = createTag("%IX0.0.8", false);
            bool myBit11 = createTag("%IX0.0.9", false);
            bool myBit12 = createTag("%IX0.0.10", false);
            bool myBit13 = createTag("%IX0.0.11", false);
            bool myBit14 = createTag("%IX0.0.12", false);
            bool myBit15 = createTag("%IX0.0.13", false);
            bool myBit16 = createTag("%IX0.0.14", false);
            bool myBit17 = createTag("%IX0.0.15", false);
"""

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
        member __.``XX Or Many test`` () =
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

        [<Test>]
        member __.``Timer test`` () =
            let storages = Storages()
            let code = """
                bool myQBit0 = createTag("%QX0.1.0", false);
                bool myBit0 = createTag("%IX0.0.0", false);
                bool myBit1 = createTag("%IX0.0.1", false);
                bool myBit2 = createTag("%IX0.0.2", false);

                bool myBit7 = createTag("%IX0.0.7", false);
                ton myTon = createTON(2000us, $myQBit0);
                $myBit7 := ($myBit0 || $myBit1) && $myBit2;
"""
            let statements = parseCode storages code
            //storages.Count === 12
            //statements.Length === 2      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

            let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml

        [<Test>]
        member __.``TIMER= Many1 AND RungIn Condition test`` () =
            let storages = Storages()
            let code = codeForBits + """
                ton myTon = createTON(2000us,
                    $myBit00 && $myBit01 && $myBit02 && $myBit03 && $myBit04 && $myBit05 && $myBit06 && $myBit07
                    && $myBit10 && $myBit11 && $myBit12 && $myBit13 && $myBit14 && $myBit15 && $myBit16    );
"""
            let statements = parseCode storages code
            let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml

        [<Test>]
        member __.``TIMER= Many2 AND RungIn Condition test`` () =
            let storages = Storages()
            let code = codeForBits + """
                ton myTon = createTON(2000us,
                    // 산전 limit : 가로로 31개
                    //let coilCellX = 31
                    $myBit00 && $myBit01 && $myBit02 && $myBit03 && $myBit04 && $myBit05 && $myBit06 && $myBit07
                    && $myBit10 && $myBit11 && $myBit12 && $myBit13 && $myBit14 && $myBit15 &&

                    $myBit00 && $myBit01 && $myBit02 && $myBit03 && $myBit04 && $myBit05 && $myBit06 && $myBit07 &&
                    $myBit10 &&
                    $myBit11 &&
                    $myBit12 &&
                    $myBit13 &&
                    //$myBit14 &&
                    //$myBit15 &&

                    $myBit16    );
"""
            let statements = parseCode storages code
            let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml


        [<Test>]
        member __.``TIMER= Many1 OR RungIn Condition test`` () =
            let storages = Storages()
            let code = codeForBits + """
                ton myTon = createTON(2000us,
                    $myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16    );
"""
            let statements = parseCode storages code
            let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml


        [<Test>]
        member __.``TIMER= Many2 OR RungIn Condition test`` () =
            let storages = Storages()
            let code = codeForBits + """
                ton myTon = createTON(2000us,
                    $myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16 ||

                    $myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16 ||

                    $myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16

                    );
"""
            let statements = parseCode storages code
            let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml


        [<Test>]
        member __.``XX TIMER= Many And, OR RungIn Condition test`` () =
            let storages = Storages()
            let code = codeForBits + """
                ton myTon = createTON(2000us,
                    ($myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16)
                    &&
                    ($myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16)
                    &&
                    ($myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16)
                    &&
                    ($myBit00 || $myBit01 || $myBit02 || $myBit03 || $myBit04 || $myBit05 || $myBit06 || $myBit07
                    || $myBit10 || $myBit11 || $myBit12 || $myBit13 || $myBit14 || $myBit15 || $myBit16)

                    );
"""
            let statements = parseCode storages code
            let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
            saveTestResult (get_current_function_name()) xml
