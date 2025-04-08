namespace XgtProtocol

open Dual.Common.Core.FS

[<AutoOpen>]
module internal LsTagParserCommonModule =
    let WORD, BIT = 16, 1

    /// returns dataBitSize * totalBitOffset
    /// - ("." 으로 시작할 수도 있고, empty 일 수도 있는) Hex string 에서 정보 추출
    /// - empty 이면 WORD type, 0 으로 취급
    let getInfoFromHexaBitString(hexaBit:string) : int * int =
        if hexaBit.IsNullOrEmpty() then
            WORD, 0
        else
            let hexaBit = if hexaBit.StartsWith(".") then hexaBit.Substring(1) else hexaBit
            match (|HexPattern|_|) hexaBit with
            | Some hexaBit -> BIT, hexaBit
            | None -> WORD, 0


[<AutoOpen>]
module LsXgiTagParserModule =
    let private dataSizeDic =
        [|
            "X", 1
            "B", 8
            "W", 16
            "D", 32
            "L", 64
        |] |> Tuple.toReadOnlyDictionary

    let dataRangeDic =
        [|
            "X", 64/1   //%IX0.0.0 ~ %IW0.0.63
            "B", 64/8   //%IB0.0.0 ~ %IW0.0.7
            "W", 64/16  //%IW0.0.0 ~ %IW0.0.3
            "D", 64/32  //%ID0.0.0 ~ %IW0.0.1
            "L", 64/64  //%IL0.0.0 ~ %IW0.0.0
        |] |> Tuple.toReadOnlyDictionary

    let dataUtypeRangeDic =
        [|
            "X", 512/1   //%UX0.0.0 ~ %UW0.0.512
            "B", 512/8   //%UB0.0.0 ~ %UW0.0.64
            "W", 512/16  //%UW0.0.0 ~ %UW0.0.32
            "D", 512/32  //%UD0.0.0 ~ %UW0.0.16
            "L", 512/64  //%UL0.0.0 ~ %UW0.0.8
        |] |> Tuple.toReadOnlyDictionary

    let private checkInRange(needle:int) (startNumber:int) (endNumber:int) =
        startNumber <= needle && needle <= endNumber

    /// returns None | Some ().   <-- Some unit
    let private tryCheckInRange n s e = checkInRange n s e |> Option.ofBool2 ()

    let private checkDataRange(dataSize:int) (index:int) =
        0 <= index && index < dataSize

    /// returns None | Some ().   <-- Some unit
    let private tryCheckDataRange d i = checkDataRange d i |> Option.ofBool2 ()


    /// XGI tag 문자열을 parsing 해서 정보 tuple 반환
    ///
    /// - device type, data size, total bit offset
    ///
    /// - e.g "%IX0" -> Some("I", 1, 0)
    ///
    /// - e.g "%MX1768" -> Some("M", 1, 1768)
    let tryParseXgiTag(tag:string): (string * int * int) option =
        let tag = tag.ToUpper()
        option {
            match tag with
            | RegexPattern @"^%([IQMLKFNRAWUT])(S)?([XBWDL])([\d\.]+)$" [ device; safety; dataType; remaining] ->
                //tracefn $"{device}; {safety}; {dataType}; {remaining}"
                let digits = remaining.SplitBy(".").Choose(Parse.TryInt).ToFSharpList()

                match safety, device, dataType, digits with
                | "", ("I"|"Q"|"M"|"L"|"N"|"K"|"R"|"A"|"W"|"F"), ("X"|"B"|"W"|"D"|"L"), d1::[] ->   // "U" 만 제외됨
                    match dataType with
                    | "X" ->
                        return ( device, BIT, d1 )
                    | _ ->
                        let dataSize = dataSizeDic[dataType]
                        let totalBitOffset = dataSize * d1;
                        return ( device, dataSize, totalBitOffset )

                | "", ("I"|"Q"|"M"|"L"|"N"|"K"|"R"|"A"|"W"|"F"), ("B"|"W"|"D"|"L"), d1::d2::[] ->   // "U" 만 제외됨
                    let dataSize = dataSizeDic[dataType]
                    do! tryCheckDataRange dataSize d2
                    let totalBitOffset = dataSize * d1 + d2;
                    return ( device, BIT, totalBitOffset ) //%IW1122.6 '.' 하나로 구분되면 무조건 BIT

                | (""|"S"), ("I"|"Q"), ("X"|"B"|"W"|"D"|"L"), d1::d2::d3::[]  ->
                    let dataSize = dataSizeDic[dataType]
                    let dataRange = dataRangeDic[dataType]

                    do!
                        option {
                                do! tryCheckDataRange dataRange d3
                                return! something
                        }

                    let totalBitOffset = d1 * 1024 + d2 * 64 + d3 * dataSize;
                    return ( device + safety, dataSize, totalBitOffset )

                | "", "U", ("X"|"B"|"W"|"D"|"L"), d1::d2::d3::[]  ->
                    let dataSize = dataSizeDic[dataType]
                    let dataUtypeRange = dataUtypeRangeDic[dataType]
                    do!
                        option {
                            do! tryCheckDataRange dataUtypeRange d3
                            do! tryCheckInRange d2 0 15
                            do! tryCheckInRange d1 0 7
                            return! something
                        }

                    let totalBitOffset = d1 * (512*16) + d2 * 512 + d3 * dataSize;
                    return ( device, dataSize, totalBitOffset )
                | _ ->
                    ()

            | _ ->
                ()
                //logDebug $"Failed to parse XGI tag : {tag}"
        }



open System.Runtime.CompilerServices
[<Extension>]   // For C#
type LsXgiTagParser =
    /// XGI tag 문자열을 parsing 해서 정보 tuple 반환
    ///
    /// - device type, data size, total bit offset
    ///
    /// - e.g "IX3" -> ("I", 1, 3)
    ///
    /// - e.g "IB1.2.3" -> ("I", 8, 512 * 16 * 1 + 64 * 2 + 3)
    ///
    /// - e.g "P1" -> null
    [<Extension>]
    static member Parse(tag:string): string * int * int =
        tryParseXgiTag tag |? (getNull<string * int * int>())



