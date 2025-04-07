namespace MelsecProtocol

open Dual.Common.Core.FS

[<AutoOpen>]
module internal MxTagParserCommonModule =

    /// 크기 정의: WORD = 16bit, BIT = 1bit
    let WORD = 16
    let BIT  = 1

    /// 단위 기호 → 비트 크기 매핑
    let sizeMap =
        [|
            "X", BIT
            "B", 8
            "W", WORD
            "D", 32
            "L", 64
        |]
        |> Tuple.toReadOnlyDictionary

    /// 허용된 디바이스 접두어
    let allowedDevices = set ["X"; "Y"; "M"; "D"; "B"; "W"; "R"; "L"; "Z"]
