namespace XgtProtocol

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices


[<AutoOpen>]
module LsXgkTagParserModule =
    
    /// 표준 XGK tag 문자열을 parsing 해서 정보 tuple 반환
    ///
    /// - device type, data size, total bit offset
    ///
    /// - 표준 tag 이므로, "P1" 등과 같은 약식은 지원되지 않음.  (bit type 인지 word type 인지 알 수 없음)
    ///
    /// - e.g "P0001A" -> Some("P", 1, 26)
    ///
    /// - e.g "P0002" -> Some("P", 16, 32)
    let tryParseXgkTag(tag:string): (string * int * int) option =
        if tag.IsNullOrEmpty() || tag.Length < 2 then       // tag.Length < 5 ???
            None
        else
            let tag = tag.ToUpper()
            let device = tag.Substring(0, 1)
            match tag[0], tag[0..1], tag.Substring(1), tag.Substring(2) with //"ZR 규격 추가"
            // Bit or Word (HEX),
            // PMKF: 4 or 5 digits
            | ('P'|'M'|'K'|'F'), _, RegexPattern @"^(\d{4})([\da-fA-F]?)$" [ Int32Pattern word; hexaBitString], _ ->
                let dataType, bitOffset = getInfoFromHexaBitString hexaBitString
                let totalBitOffset = word * 16 + bitOffset
                Some ( device, dataType, totalBitOffset )

            // Bit or Word (HEX)
            // L: 5 or 6 digits
            | 'L', _, RegexPattern @"^(\d{5})([\da-fA-F]?)$" [ Int32Pattern word; hexaBitString ], _ ->
                let dataType, bitOffset = getInfoFromHexaBitString hexaBitString
                let totalBitOffset = word * 16 + bitOffset
                Some ( device, dataType, totalBitOffset )
                
            // Bit or Word (HEX)
            | ('T'|'C'|'N'|'D'|'R'), _, RegexPattern @"^(\d+)(\.[\da-fA-F]?)?$" [ Int32Pattern word; hexaBitString ], _ ->
                let dataType, bitOffset = getInfoFromHexaBitString hexaBitString
                let totalBitOffset = word * 16 + bitOffset
                Some ( device, dataType, totalBitOffset )


            //Z 인덱스 레지스터, R 파일레지스터 (ZR과 같음)
            | ('Z'|'R'), _, RegexPattern @"^(\d+)$" [ Int32Pattern word ], _ ->
                let totalBitOffset = word * 16
                Some ( device, WORD, totalBitOffset )

            //ZR 파일레지스터 (Z와 다름)
            | _, "ZR", _, RegexPattern @"^(\d+)(\.[\da-fA-F]?)?$" [ Int32Pattern word; hexaBitString ]  ->
                let dataType, bitOffset = getInfoFromHexaBitString hexaBitString
                let totalBitOffset = word * 16 + bitOffset
                Some ( device, dataType, totalBitOffset )

            // Bit or Word (HEX)
            // U2F.31.F  //채널 31max
            | 'U', _, RegexPattern @"^([\da-fA-F]+)\.(\d+)(\.[\da-fA-F])?$" [  hexaFile; Int32Pattern sub; hexaBitString ], _ ->
                let dataType, bitOffset = getInfoFromHexaBitString hexaBitString
                let totalBitOffset = (Convert.ToInt32(hexaFile, 16) * 32 * 16) + (sub * 16) + bitOffset
                Some ( device, dataType, totalBitOffset )

            // S00.00  ~  S255.99  bit  //스텝제어 전용 디바이스 SYY.XX  XX는 bit가 아닌 스텝번호
            | 'S', _, RegexPattern @"^(\d+).(\d+)$" [ Int32Pattern word;  bit ], _ ->
                let totalBitOffset = word * 100 + Convert.ToInt32(bit.TrimStart('.'))  //Bit는 100단위
                Some ( device, BIT, totalBitOffset )

            | _ ->
                logWarn $"Failed to parse XGK tag : {tag}"
                None


    let Xgk5Digit = [ "L"; "N"; "D"; "R" ]
    let Xgk4Digit = [ "P"; "M"; "K"; "F"; "T"; "C" ]

    let getXgkBitText (device:string, offset: int) : string =
        let word = offset / 16
        let bit = offset % 16
        match device.ToUpper() with
        | d when Xgk5Digit |> List.contains d ->
            device + sprintf "%05i.%X" word bit
        | d when Xgk4Digit |> List.contains d ->
            device + sprintf "%04i%X" word bit
        | _ -> failwithf $"XGK device({device})는 지원하지 않습니다."

    let getXgkWordText (device:string, offsetBit: int) : string =
        
        let wordIndex = offsetBit / 16
        match device.ToUpper() with
        | d when Xgk5Digit |> List.contains d ->
            sprintf "%s%05i" d wordIndex
        | d when Xgk4Digit |> List.contains d ->
            sprintf "%s%04i" d wordIndex
        | _ -> failwithf $"XGK device({device})는 지원하지 않습니다."


    let parseAddress(dev:string, offsetBit:int, isBit:bool): string =
        if isBit then   
            getXgkBitText (dev, offsetBit)
        else
            getXgkWordText (dev, offsetBit)

    let tryParseXgkValidText (tag: string) (isBit: bool): string option =
        if String.IsNullOrWhiteSpace(tag) || tag.Length < 2 then
            None
        else
            let fullName =   
                if List.contains tag.[0] ['P'; 'M'; 'K'; 'F'] then
                    let padCnt = if isBit then 5 else 4
                    tag.[0].ToString() + tag.Substring(1).PadLeft(padCnt, '0')
                else
                    tag
            
            match tryParseXgkTag fullName with
            | Some (dev, size, offset) when isBit && size = BIT ->
                getXgkBitText(dev, offset) |> Some
            | Some (dev, size, offset) when not isBit && size = WORD ->
                getXgkWordText(dev, offset) |> Some
            | _ -> None

    let tryParseXgkTagAbbreviated (tag: string) (isBit: bool): (string * int * int) option =
        match tryParseXgkValidText tag isBit with
        | Some standardText -> tryParseXgkTag standardText
        | None -> None
            

[<Extension>]   // For C#
type LsXgkTagParser =
    /// 표준 XGK tag 문자열을 parsing 해서 정보 tuple 반환
    ///
    /// - device type, data size, total bit offset
    ///
    /// - 표준 tag 이므로, "P1" 등과 같은 약식은 지원되지 않음.  (bit type 인지 word type 인지 알 수 없음)
    ///
    /// - e.g "P0001A" -> ("P", 1, 26)
    ///
    /// - e.g "P0002" -> ("P", 16, 32)
    ///
    /// - e.g "P1" -> null
    [<Extension>]
    static member Parse(tag:string): string * int * int =
        tryParseXgkTag tag |? (getNull<string * int * int>())
    
    ///// 약식 tag 이므로, "P1" 등도 지원.  bit type 인지 word type 인지 isBit 에 지정
    [<Extension>]
    static member Parse(tag:string, isBit:bool): string * int * int =
        match tryParseXgkValidText tag isBit with
        | Some standardText -> tryParseXgkTag standardText |? (getNull<string * int * int>())
        | None -> getNull<string * int * int>()
        
    [<Extension>]
    static member ParseAddress(dev:string, offsetBit:int, isBit:bool): string =
        parseAddress (dev, offsetBit, isBit)

    ///// (약식 표기, BIT Type) XGK tag 문자열을 parsing 해서 StandardText 반환
    ///// 약식에 따른 영향 없는  'P', 'M', 'K', 'F', 'L' 제외한 TAG는 XGK TAG면 그대로 반환
    [<Extension>]
    static member ParseValidText(tag:string, isBit:bool): string =
        match tryParseXgkValidText tag isBit with
        | Some standardText -> standardText 
        | None -> getNull<string>()
