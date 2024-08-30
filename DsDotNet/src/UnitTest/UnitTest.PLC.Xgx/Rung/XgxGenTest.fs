namespace T.Rung
open T

open Dual.UnitTest.Common.FS

open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common.FlatExpressionModule

type XgxGenerationTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)
    let newline = "\r\n"
    let and28 = """
        $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
        $x10 && $x11 && $x12 && $x13 && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
        $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28
"""
    let and30 = $"{and28} && $x29 && $x30"

    member x.``Add test`` () =
        let qAddress = if xgx = XGI then "%QX0.1.0" else "P00001"
        let code = $"""
            int16 nn0 = 0s;
            int16 nn1 = 1s;
            int16 nn2 = 2s;
            int16 nn3 = 3s;
            int16 nn4 = 4s;
            int16 nn5 = 5s;
            bool qq = createTag({dq}{qAddress}{dq}, false);

            //$qq = add($nn1, $nn2) > 3s;
            //$qq = ($nn1 + $nn2) * 9s + $nn3 > 3s;
            $qq = true && (($nn1 + $nn2) * 9s + $nn3 > 3s);
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``And Huge28 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and28};"
        code |> x.TestCode (getFuncName()) |> ignore
    member x.``And Huge29 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and28} && $x29;"
        code |> x.TestCode (getFuncName()) |> ignore
    member x.``And Huge30 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and30};"
        code |> x.TestCode (getFuncName()) |> ignore
    member x.``And Huge31 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and30} && $x31;"
        code |> x.TestCode (getFuncName()) |> ignore
    member x.``And Huge32 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and30} && $x31&& $x32;"
        code |> x.TestCode (getFuncName()) |> ignore
    member x.``And Huge33 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and30} && $x31 && $x32 && $x33;"
        code |> x.TestCode (getFuncName()) |> ignore
    member x.``And Huge34 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and30} && $x31 && $x32 && $x33 && $x34;"
        code |> x.TestCode (getFuncName()) |> ignore
    member x.``And Huge35 test`` () =
        let code = generateLargeVariableDeclarations xgx + $"{newline}$x15 = {and30} && $x31 && $x32 && $x33 && $x34 && $x35;"
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``And Huge simple test`` () =
        let code = generateLargeVariableDeclarations xgx + $"""
            $x15 =
                {and30} && $x31 && $x32 && $x33 && $x34 && $x35 && $x36 && $x37 //&& $x38 && $x39
                ;
    """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``And Huge test`` () =
        let code = generateLargeVariableDeclarations xgx + """
            $x16 =
                ($nn1 > $nn2) &&
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                $x10 && $x11 && $x12 && $x13 && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                $x30 && ($nn1 > $nn2) &&
                $x32 && $x33 && $x34 && $x35 && $x36 && $x37 //&& $x38 && $x39
                ;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``And Huge test 3`` () =
        let code = generateLargeVariableDeclarations xgx + """
            $x15 =
                ($x00 || $x01 || $x02 || $x03) && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                ($x10 && $x11 || $x12 && $x13) && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                $x30 && $x31 &&
                ($x32 || $x33 && $x34 || $x35) && $x36 && $x37 && $x38 && $x39 &&
                ($x00 || $x01 || $x02 || $x03) && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                ($x10 && $x11 || $x12 && $x13) && $x14 && $x15 && $x16 && $x17 && $x18 && $x19
                ;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``And Huge test 4`` () =
        let x1_to_x38 = [1..38] |> map (fun i -> sprintf "$x%02d" i) |> String.concat " && "
        let code = generateBitTagVariableDeclarations xgx 0 50 + $"""
            $x49 =
                (   (    false
                        || ({x1_to_x38})
                        || $x39
                        || $x40
                        || false )
                    && true
                    || $x41
                )
                && ! ($x42 && !($x43) && !($x44) && !($x45) || $x46);
            """

        code |> x.TestCode (getFuncName()) |> ignore


    member x.``And Huge test2`` () =
        let code = generateLargeVariableDeclarations xgx + """
            $x16 =
                (($nn1 + $nn2) > $nn3) && (($nn4 - $nn5 + $nn6) > $nn7) &&
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 &&
                $x10 && $x11 && $x12 && $x13 && $x14 && $x15 && $x16 && $x17 && $x18 && $x19 &&
                $x20 && $x21 && $x22 && $x23 && $x24 && $x25 && $x26 && $x27 && $x28 && $x29 &&
                $x30 && ($nn1 > $nn2) &&
                $x32 && $x33 && $x34 && $x35 && $x36 && $x37 //&& $x38 && $x39
                ;
        """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``And Many test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 =
                $x00 && $x01 && $x02 && $x03 && $x04 && $x05 && $x06 && $x07 && $x08 && $x09 && $x10 &&
                $x11 && $x12 && $x13 && $x14
                ;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``AndOr simple test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 4 + """
            $x03 = ($x00 || $x01) && $x02;
"""
        let statements = parseCodeForTarget storages code XGI
        storages.Count === 4
        statements.Length === 1      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``AndOr2 test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x07 =    (($x00 || $x01) && $x02)
                        ||  $x03
                        || ($x04 && $x05 && $x06)
                        ;
            $x15 =    (($x08 && $x09) || $x10)
                        && $x11
                        && ($x12 || $x13 || $x14)
                        ;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``COPY test min int16`` () =
        let code =
            $"""
            bool cond = true;
            int16 src = 1s;
            int16 tgt = 2s;
            copyIf($cond, $src, $tgt);
            """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``COPY test min bool`` () =
        let code =
            $"""
            bool cond = true;
            bool src = true;
            bool tgt = true;
            copyIf($cond, $src, $tgt);
            """
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``COPY test`` () =
        let code =
            let ands (x:string) = [0..63] |> map (fun i -> sprintf "$%s%02d" x i) |> String.concat " && "
            let start = 256     // 임시 변수 사용 공간 회피 시작 주소
            let copyIfs =
                [
                    for i in [0..9] @ [15..17] @ [30..33] do
                        yield sprintf "copyIf($cond%02d, $x%02d, $y%02d);" i i i
                ] |> String.concat "\n"
            let addressPrefix =
                match xgx with
                | XGI -> "MX"
                | XGK -> "M"
                | _ -> failwith "Not supported runtime target"
            generateNamedBitTagVariableDeclarations xgx "x" addressPrefix (start+0) 64 +
            generateNamedBitTagVariableDeclarations xgx "y" addressPrefix (start+64) 64 +
            generateNamedBitTagVariableDeclarations xgx "cond" addressPrefix (start+128) 64 +
            $"""
            $x63 = {ands "x"};
            $y63 = {ands "y"};
            $cond63 = {ands "cond"};

            int16 src = 1s;
            int16 tgt = 2s;
            copyIf($cond50, $src, $tgt);

            copyIf($cond51, true, $y51);
            copyIf($cond52, false, $y52);

            {copyIfs}

"""
        code |> x.TestCode (getFuncName()) |> ignore


    member x.``COPY test2`` () =
        let code = """
bool b1 = false;
bool b2 = false;
copyIf(2 > 3, $b1, $b2);
"""
        code |> x.TestCode (getFuncName()) |> ignore


    member x.``COPY test3`` () =
        let code = """
bool b1 = false;
bool b2 = false;
bool b3 = false;
int nn3 = 0;
//copyIf( !rising($b1) && $b2, (2 + 3), $nn3);
copyIf( rising(!$b1) && $b2, (2 + $nn3), $nn3);
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``OR Block test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 =
                $x01 && ($x02 || ($x03 && ($x04 || $x05 || $x06 || $x07) && $x08 && ($x09 || $x10)))
                ;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``OR Block test2`` () =
        let code = generateBitTagVariableDeclarations xgx 0 32  + """
            $x31 =
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
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``OR Huge test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 32  + """
            $x15 =
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09 ||
                $x10 || $x11 || $x12 || $x13 || $x14 || $x15 || $x16 || $x17 || $x18 || $x19 ||
                $x20 || $x21 || $x22 || $x23 || $x24 || $x25 || $x26 || $x27 || $x28 || $x29 ||
                $x30 || $x31 ||
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09
                ;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``OR Many test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 =
                $x00 || $x01 || $x02 || $x03 || $x04 || $x05 || $x06 || $x07 || $x08 || $x09 ||
                $x10 || $x11 || $x12 || $x13 || $x14
                ;
"""
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``OR simple test`` () =
        let storages = Storages()
        let code = generateBitTagVariableDeclarations xgx 0 3 + """
            $x02 = ($x00 || $x01);
"""
        let statements = parseCodeForTarget storages code XGI
        storages.Count === 3
        statements.Length === 1      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml

    member x.``OR variable length test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x07 =    (($x00 || $x01) && $x02)
                        ||  $x03
                        || ($x04 && $x05 && $x06)
                        ;

            $x15 =
                $x00
                || ( $x10 && $x11 && $x12 && $x13 && $x14)
                || ( $x06 && $x07 && $x08 && $x09 )
                || ( $x03 && $x04 && $x05 )
                || ( $x01 && $x02 )
                || $x00
                ;
            $x15 =
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

            $x15 =
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
        code |> x.TestCode (getFuncName()) |> ignore

    member x.``OR variable length 역삼각형 test`` () =
        let code = generateBitTagVariableDeclarations xgx 0 16 + """
            $x15 =
                $x00
                || ( ( $x01 || $x02 ) && $x03 )
                ;
"""
        code |> x.TestCode (getFuncName()) |> ignore










//[<Collection("SerialXgxGenerationTest")>]
type XgiGenerationTest() =
    inherit XgxGenerationTest(XGI)

    [<Test>] member x.``Add test`` () =  base.``Add test`` ()
    [<Test>] member x.``And Huge28 test`` () = base.``And Huge28 test`` ()
    [<Test>] member x.``And Huge29 test`` () = base.``And Huge29 test`` ()
    [<Test>] member x.``And Huge30 test`` () = base.``And Huge30 test`` ()
    [<Test>] member x.``And Huge31 test`` () = base.``And Huge31 test`` ()
    [<Test>] member x.``And Huge32 test`` () = base.``And Huge32 test`` ()
    [<Test>] member x.``And Huge33 test`` () = base.``And Huge33 test`` ()
    [<Test>] member x.``And Huge34 test`` () = base.``And Huge34 test`` ()
    [<Test>] member x.``And Huge35 test`` () = base.``And Huge35 test`` ()
    [<Test>] member x.``And Huge simple test`` () = base.``And Huge simple test`` ()
    [<Test>] member x.``And Huge test`` () = base.``And Huge test`` ()
    [<Test>] member x.``And Huge test 3`` () = base.``And Huge test 3`` ()
    [<Test>] member x.``And Huge test 4`` () = base.``And Huge test 4`` ()
    [<Test>] member x.``And Huge test2`` () = base.``And Huge test2`` ()
    [<Test>] member x.``And Many test`` () = base.``And Many test`` ()
    [<Test>] member x.``AndOr simple test`` () = base.``AndOr simple test`` ()
    [<Test>] member x.``AndOr2 test`` () = base.``AndOr2 test`` ()
    [<Test>] member x.``COPY test min bool`` () = base.``COPY test min bool`` ()
    [<Test>] member x.``COPY test min int16`` () = base.``COPY test min int16`` ()
    [<Test>] member x.``COPY test`` () = base.``COPY test`` ()
    [<Test>] member x.``X COPY test2`` () = base.``COPY test2`` ()
    [<Test>] member x.``COPY test3`` () = base.``COPY test3`` ()
    [<Test>] member x.``OR Block test`` () = base.``OR Block test`` ()
    [<Test>] member x.``OR Block test2`` () = base.``OR Block test2`` ()
    [<Test>] member x.``OR Huge test`` () = base.``OR Huge test`` ()
    [<Test>] member x.``OR Many test`` () = base.``OR Many test`` ()
    [<Test>] member x.``OR simple test`` () = base.``OR simple test`` ()
    [<Test>] member x.``OR variable length test`` () = base.``OR variable length test`` ()
    [<Test>] member x.``OR variable length 역삼각형 test`` () = base.``OR variable length 역삼각형 test`` ()

type XgkGenerationTest() =
    inherit XgxGenerationTest(XGK)

    [<Test>] member x.``Add test`` () =  base.``Add test`` ()
    [<Test>] member x.``And Huge28 test`` () = base.``And Huge28 test`` ()
    [<Test>] member x.``And Huge29 test`` () = base.``And Huge29 test`` ()
    [<Test>] member x.``And Huge30 test`` () = base.``And Huge30 test`` ()
    [<Test>] member x.``And Huge31 test`` () = base.``And Huge31 test`` ()
    [<Test>] member x.``And Huge32 test`` () = base.``And Huge32 test`` ()
    [<Test>] member x.``And Huge33 test`` () = base.``And Huge33 test`` ()
    [<Test>] member x.``And Huge34 test`` () = base.``And Huge34 test`` ()
    [<Test>] member x.``And Huge35 test`` () = base.``And Huge35 test`` ()
    [<Test>] member x.``And Huge simple test`` () = base.``And Huge simple test`` ()
    [<Test>] member x.``And Huge test`` () = base.``And Huge test`` ()
    [<Test>] member x.``And Huge test 3`` () = base.``And Huge test 3`` ()
    [<Test>] member x.``And Huge test 4`` () = base.``And Huge test 4`` ()
    [<Test>] member x.``And Huge test2`` () = base.``And Huge test2`` ()
    [<Test>] member x.``And Many test`` () = base.``And Many test`` ()
    [<Test>] member x.``AndOr simple test`` () = base.``AndOr simple test`` ()
    [<Test>] member x.``AndOr2 test`` () = base.``AndOr2 test`` ()
    [<Test>] member x.``COPY test min bool`` () = base.``COPY test min bool`` ()
    [<Test>] member x.``COPY test min int16`` () = base.``COPY test min int16`` ()
    [<Test>] member x.``COPY test`` () = base.``COPY test`` ()
    [<Test>] member x.``COPY test2`` () = base.``COPY test2`` ()
    [<Test>] member x.``COPY test3`` () = base.``COPY test3`` ()
    [<Test>] member x.``OR Block test`` () = base.``OR Block test`` ()
    [<Test>] member x.``OR Block test2`` () = base.``OR Block test2`` ()
    [<Test>] member x.``OR Huge test`` () = base.``OR Huge test`` ()
    [<Test>] member x.``OR Many test`` () = base.``OR Many test`` ()
    [<Test>] member x.``OR simple test`` () = base.``OR simple test`` ()
    [<Test>] member x.``OR variable length test`` () = base.``OR variable length test`` ()
    [<Test>] member x.``OR variable length 역삼각형 test`` () = base.``OR variable length 역삼각형 test`` ()

