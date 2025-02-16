namespace DsXgComm

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


    let tryParseXgkValidText (tag: string) (isBit: bool): string option =
        let standardizeTag (device: string) (remaining: string) (wordLength: int) =
            if isBit then
                match remaining with
                | RegexPattern @"^(\d+)([\da-fA-F])$" [ Int32Pattern word; hexaBitString] ->  
                    let paddedWord = word.ToString().PadLeft(wordLength, '0')
                    Some $"{device}{paddedWord}{hexaBitString}"
                | RegexPattern @"^([\da-fA-F])$" [ hexaBitString] ->     
                    Some $"{device}{String('0', wordLength)}{hexaBitString}"
                | _ -> 
                    logWarn($"ERROR standardizing xgk tag: {tag}")
                    None
            else
                match remaining with
                | RegexPattern @"^(\d+)$" [ Int32Pattern word ] ->       
                    let paddedWord = word.ToString().PadLeft(wordLength, '0')
                    Some $"{device}{paddedWord}"
                | _ -> 
                    logWarn($"ERROR standardizing xgk tag: {tag}")
                    None

        option {
            if tag.Length > 0 then
                let device, remaining = tag.[0].ToString(), tag.Substring(1)
                match tag.[0] with
                | 'P' | 'M' | 'K' | 'F' ->
                    let maxLength = if isBit then 6 else 5
                    if tag.Length <= maxLength then
                        return! standardizeTag device remaining 4
                | 'L' ->
                    let maxLength = if isBit then 7 else 6
                    if tag.Length <= maxLength then
                        return! standardizeTag device remaining 5
                | _ -> 
                    return! tryParseXgkTag tag |> Option.map (fun (_) ->  tag)
        }

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

    ///// (약식 표기, BIT Type) XGK tag 문자열을 parsing 해서 StandardText 반환
    ///// 약식에 따른 영향 없는  'P', 'M', 'K', 'F', 'L' 제외한 TAG는 XGK TAG면 그대로 반환
    [<Extension>]
    static member ParseValidText(tag:string, isBit:bool): string =
        match tryParseXgkValidText tag isBit with
        | Some standardText -> standardText 
        | None -> getNull<string>()