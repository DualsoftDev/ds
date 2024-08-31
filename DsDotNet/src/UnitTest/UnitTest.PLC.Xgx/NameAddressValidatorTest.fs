namespace T
open Dual.Common.UnitTest.FS

open NUnit.Framework

open PLC.CodeGen.LS.XgiPrologModule
open PLC.CodeGen.Common.IECAddressModule
open Engine.Core

type NameAddressValidatorTest() =
    inherit XgxTestBaseClass(XGI)

    [<Test>]
    member __.``Validation= Variable name test`` () =
        let validNames = [
            "n"; "m"; "var1"; "nn0"; "mb"; "mx"; "ix"
            "N"; "M"; "VAR1"; "NN0"; "MB"; "MX"; "IX"
            "ix0"; "IX0"; "ib0"; "iw0"; "il0"           // ix 는 직접 변수와 동일한데도 허용???
            "한글_이름"
        ]
        for name in validNames do
            validateVariableName name XGI |> Result.isOk === true

        let invalidNames = [
            "mx0"; "mb0"; "mb999"; "MX0"; "Mx0"; "mX0"    // 직접 변수와 동일한 이름 불허
            "v 0"; "v\t0"; "v\r0";
        ]
        for xName in invalidNames do
            validateVariableName xName XGI |> Result.isError === true


    [<Test>]
    member __.``Validation= XGI Variable address test`` () =
        let validAddresses = [
            "%IX0"; "%ix0"
            "%IB0.1"; "%IL9.1"; "%IX1.1.1"; "%IX0.1.2"
            "%MX0"; "%mx0"; "%MX1"
            "%MW2.1"
        ]
        for addr in validAddresses do
            validateAddress "" addr XGI |> Result.isOk === true

        let invalidAddresses = [
            "%IX0A1"; "%MX0.1"; "%MX9.1"; "%MX1.1.1"; "%M0.1.2"       // MX 영역은 '.' 을 가지지 않는다.
            "%MB1.9"; "%MW1.17"                             // Byte, Word 등의 영역 침범
            "%Y0"; "%y0";                                   // 허용되지 않는 영역
            "M0"; "MX0"; "I0"; "IX0"; "Q0"; "QX0";          // % 누락
            "x%MX0"                                         // % 로 시작하지 않음
            "%M0ZZ"
        ]
        for xAddr in invalidAddresses do
            validateAddress "" xAddr XGI |> Result.isError === true

    [<Test>]
    member __.``Variable address standardize test`` () =
        "%I0"    |> standardizeAddress === "%IX0"
        "%m0"    |> standardizeAddress === "%MX0"
        "%iw10"  |> standardizeAddress === "%IW10"
        "%iw1.2" |> standardizeAddress === "%IW1.2"
        "%i1.2"  |> standardizeAddress === "%IX1.2"

