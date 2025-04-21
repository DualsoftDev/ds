namespace T.Types
open T

open NUnit.Framework
open Dual.Common.Core.FS
open Engine.Core
open Engine.Parser.FS

type XgxExtendedTypesTest(xgx:HwCPU) =
    inherit XgxTestBaseClass(xgx)

    let span width = width*3



    member x.``Local vars type test`` () =
        let storages = Storages()
        let code = """
            bool    mybool   = false;
            single  mysingle = 0.1f;
            double  mypi = 3.14;
            double  myeuler = 2.718;
            sbyte   mysbyte  = 1y;
            int16   myint16  = 16s;
            uint16  myuint16 = 16us;
            int32   myint32  = 32;
            uint32  myuint32 = 32u;

            double myDoubleSum1 = 0.0;
            double myDoubleSum2 = 0.0;
            double myDoubleSum3 = $mypi + $myeuler;
            double myDoubleSum4 = 3.14 + 2.718;
            $myDoubleSum1 = $myDoubleSum2;
            $myDoubleSum2 = $myDoubleSum1 + $myDoubleSum3;
"""
        let code =
            match xgx with
            | XGK -> code
            | XGI -> code + """
            int64   myint64  = 64L;
            uint64  myuint64 = 64UL;
            char    mychar   = 'a';
            byte    mybyte   = 2uy;
"""
            | _ -> failwith "Not supported plc type"

        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml



type XgiExtendedTypesTest() =
    inherit XgxExtendedTypesTest(XGI)
    [<Test>] member x.``Local vars type test``() = base.``Local vars type test``()

type XgkExtendedTypesTest() =
    inherit XgxExtendedTypesTest(XGK)
    [<Test>] member x.``Local vars type test``() = base.``Local vars type test``()


(*
XGK PLC에서 문자열 데이터 처리 : https://www.google.com/url?sa=t&source=web&rct=j&opi=89978449&url=https://bime.pusan.ac.kr/bbs/bime/3837/440488/download.do&ved=2ahUKEwi8nbibouGFAxXnj68BHaH2CRkQFnoECCoQAQ&usg=AOvVaw3YHmSO7X8wOnQePIgQjjFr


XGK PLC에서 처리할 수 있는 문자열 데이터는 영문자, 숫자, 특수 문자 외 윈도우에서 지원하는 언어입니다. 즉 한글 윈도우를 사용할 경우
한글이 사용 가능하며, 한글 윈도우에서 지원하는 한자도 사용 가능합니다. 키보드에서 기본적으로 지원하는 영문자, 숫자 또는 특수 문자의
경우 1개의 글자는 1Byte를 점유하며, 한글, 한자의 경우 1개 글자가 2Byte를 점유합니다. XGK에서 문자열 처리 명령어는 최대 32 Byte의
문자열 데이터를 처리할 수 있으므로, 영문자, 숫자, 특수 문자로 구성된 문자열의 경우 최대 32개의 문자를 처리할 수 있으며, 한글, 한자의
경우 16개의 문자까지 처리할 수 있습니다.
PLC에 문자 데이터를 저장할 경우 영문자, 숫자, 특수 문자의 경우 아스키 코드로 변환되어 저장 영역의 선두 바이트부터 저장되며, 다른
언어 문자의 경우 해당 언어 윈도우에서 표준으로 사용하는 코드로 변환되어 1개의 문자가 2 Byte에 저장됩니다. 예를 들어 영문 대문자 ‘A’
를 XGK 데이터 메모리 D00000에 저장하면 영문 대문자 ‘A’에 대한 아스키 코드 (h41)을 저장 데이터 메모리의 선두 바이트에 해당하는
D00000 워드의 하위 바이트(0 ~ 7번 비트)에 저장하고, 한글 ‘가’를 D00000에 저장하면 ‘가’에 대한 KSC5601 코드 (hA1B0)를 D0000 1개
워드에 저장합니다. 여기서 주의할 점은 1개의 문자를 PLC에 저장하더라도 32 Byte 메모리를 사용합니다. 즉, 영문 대문자 ‘A’를 D00000에
저장할 경우 ‘A’는 D00000의 하위 바이트에 저장되고, D00000의 상위 바이트(8 ~ F번 비트)부터 D00015의 상위 바이트까지 31개의 바이트
는 모두 NULL(h00)로 채워집니다.
XGK PLC에서 문자열 데이터를 처리할 경우 연산의 접두어로 ‘$’를 사용하며 문자열 상수는 작은 따옴표 ( ‘ ’ )로 표시합니다.
아래의 프로그램 예는 각 비트가 ON되면 영문자 ‘ABCD’, 한글 ‘가나다라’ 한자 ‘日就月將’ 을 D00000부터 저장합니다.
특히, P00000 을 ON 시킨 D00000부터 16워드 이상의 데이터 메모리를 모니터하면서 문자열 데이터를 저장하는 조건을 ON 시키면 데이터
가 저장되지 않는 영역은 0으로 클리어 되는 것을 확인할 수 있습니다
*)