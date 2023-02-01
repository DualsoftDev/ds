namespace T

open NUnit.Framework

open PLC.CodeGen.LSXGI.XgiPrologModule

type NameAddressValidatorTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``Validation= Variable name test`` () =
        let validNames = [
            "n"; "m"; "var1"; "nn0"; "mb"; "mx"; "ix"
            "N"; "M"; "VAR1"; "NN0"; "MB"; "MX"; "IX"
            "ix0"; "IX0"; "ib0"; "iw0"; "il0"           // ix 는 직접 변수와 동일한데도 허용???
            "한글_이름"
        ]
        for name in validNames do
            validateVariableName name |> Result.isOk === true

        let invalidNames = [
            "mx0"; "mb0"; "mb999"; "MX0"; "Mx0"; "mX0"    // 직접 변수와 동일한 이름 불허
            "v 0"; "v\t0"; "v\r0";
        ]
        for xName in invalidNames do
            validateVariableName xName |> Result.isError === true


    [<Test>]
    member __.``Validation= Variable address test`` () =
        let validAddresses = [
            "%I0"; "%i0"; "%IX0"; "%ix0"
            "%IX0.1"; "%IX9.1"; "%IX1.1.1"; "%I0.1.2"
            "%M0"; "%m0"; "%MX0"; "%mx0"; "%MX1"
        ]
        for addr in validAddresses do
            validateAddress addr |> Result.isOk === true

        let invalidAddresses = [
            "%MX0.1"; "%MX9.1"; "%MX1.1.1"; "%M0.1.2"       // M 영역은 '.' 을 가지지 않는다.
            "%Y0"; "%y0";                                   // 허용되지 않는 영역
            "M0"; "MX0"; "I0"; "IX0"; "Q0"; "QX0";          // % 누락
            "x%MX0"                                         // % 로 시작하지 않음
            "%M0ZZ"
        ]
        for xAddr in invalidAddresses do
            validateAddress xAddr |> Result.isError === true
