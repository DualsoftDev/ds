namespace Dsu.PLCConverter.FS

open ActivePattern
open Microsoft.FSharp.Collections
open Dsu.PLCConverter.FS.XgiSpecs
open System.Diagnostics

[<AutoOpen>]
/// 문자열 주소를 기반으로 Symbol 객체를 생성하는 타입
type Symbol (addressString: string) =
    // 주소 문자열에서 MelsecDevice와 인덱스를 추출
    let head, d1, d2 = XGI.Parsing addressString
    // MelsecDevice 정보로 XGI 주소 및 자동 변수 타입을 생성
    let address, varTypeAuto = XGI.MakeXgiAddress(head, d1, d2)
    let symbol, varType = XGI.MakeXgiSymbolName(head, d1, d2), varTypeAuto

    // 심볼의 인덱스, 심볼 이름, 주소, 헤더 타입, 변수 타입을 멤버로 제공
    member x.Index = d1
    member x.Symbol = symbol
    member x.Address = address
    member x.HeadType = XGI.HeadType address
    member x.VarType = varType


[<AutoOpen>]
/// XML에서 사용되는 심볼 태그의 속성 및 관련 함수들을 포함하는 모듈
module XgiSymbol =
    [<DebuggerDisplay("Name({Name},{Address})")>]
    type SymbolInfo (index, name, comment, deviceType, address, kind, typed, state, melsecAddress, (isNibble:bool)) =
        // 주소의 사용 횟수와 이전 주소 값을 저장하는 mutable 필드
        let mutable sameAddCount = 0
        let mutable oldAddress = melsecAddress
        member x.IsNibble = isNibble
        
        /// 인덱스에 자동 변수 타입의 주소일 경우 10000000을 더해 정렬을 위한 뒷번호를 배정
        member x.Index with get() =
            match address with
            | StartsWith "%A" () -> index + 10000000
            | _ -> index

        // 심볼 이름, 주소, 설명 등의 정보를 멤버로 제공
        member x.Name = name: string
        member x.Address with get() =
            match address with
            | StartsWith "%A" () -> ""
            | _ -> address
        member x.Comment with get() = System.Security.SecurityElement.Escape comment
        member x.SameAddress with get() = sameAddCount and set(value) = sameAddCount <- value
        member x.GxAddress with get() = oldAddress and set(value) = oldAddress <- value
        member private x.deviceType = deviceType: string
        member private x._Kind = kind: int
        member private x._Type = typed: string
        member private x._State = state: int
        member x.DeviceType() = deviceType
        member x.Kind() = kind
        member x.Type() = typed
        member x.State() = state

        // Symbol 관련 XML 태그의 속성을 생성하는 내부 함수
        member private x.GetXmlArgs() =
            seq {
                yield sprintf "Name=\"%s\"" x.Name
                yield sprintf "Kind=\"%d\"" (x.Kind())
                yield sprintf "Type=\"%s\"" (x.Type())
                yield sprintf "Comment=\"%s\"" x.Comment
                yield sprintf "Device=\"%s\"" (x.DeviceType())
                yield sprintf "Address=\"%s\"" x.Address
                yield sprintf "State=\"%d\"" (x.State())
            } |> String.concat " "

        // DirectVar XML 태그에 필요한 속성을 생성하는 내부 함수
        member private x.GetDirectXmlArgs() =
            seq {
                yield sprintf "Device=\"%s\"" x.Address
                yield sprintf "Comment=\"%s\"" x.Comment
            } |> String.concat " "

        /// Symbol 관련 XML 태그를 생성하는 함수
        member x.ToText() =
            sprintf "<Symbol %s/>" (x.GetXmlArgs())
            //Address, Trigger, InitValue 등의 속성도 포함 가능 (사용되지 않음)

        /// XML 태그를 생성하며 필요한 children 요소를 포함하는 함수
        member x.GenerateXml() =
            seq {
                yield sprintf "\t<Symbol %s>" (x.GetXmlArgs())
                //yield! ["Addresses"; "Retains"; "InitValues"; "Comments"]
                //    |> Seq.map (fun k -> sprintf "\t\t<Member%s/>" k)
                yield "\t</Symbol>"
            } |> String.concat "\r\n"

        /// DirectVar XML 태그를 생성하는 함수
        member x.GenerateDirectVarXml() =
            seq {
                yield sprintf "\t<DirectVar %s>" (x.GetDirectXmlArgs())
                yield "\t</DirectVar>"
            } |> String.concat "\r\n"

    /// 로컬 심볼 생성 함수
    let createLocalSymbol (s: SymbolInfo) =
        SymbolInfo (
            s.Index,
            s.Name,
            s.Comment,
            s.DeviceType(),
            s.Address,
            (if s.Kind() = (Variable.Kind.VAR_GLOBAL|>int) then (Variable.Kind.VAR_EXTERNAL|>int) else  (s.Kind()|>int)) ,
            s.Type(),
            0,
            s.GxAddress,
            s.IsNibble
        )

    let createNibbleTempLWord(s: SymbolInfo) =
        SymbolInfo (
            s.Index,
            $"{s.Name}_LWORD",
            s.Comment,
            "A",
            s.Address,
            int Variable.Kind.VAR,
            "LWORD",
            0,
            s.GxAddress+"_Nibble",
            s.IsNibble
        )

        /// 문자열 주소로부터 심볼을 생성하는 함수
    let makeSymbol addressString comment (kind: Variable.Kind) =
        let symbol = Symbol(addressString)
        let symbolInfo = SymbolInfo(
            symbol.Index,
            symbol.Symbol,
            comment,
            symbol.HeadType,
            symbol.Address,
            int kind,
            symbol.VarType.ToString(),
            0,
            addressString,
            false
        )
        symbolInfo

    let mutable rowCount = 0    

    /// 비동기적으로 심볼을 생성하는 함수
    let makeSymbolAsync addressString comment (kind: Variable.Kind) =
        async {
            rowCount<-rowCount+1
            if rowCount % 1000 = 0 then
                rowProcessedEvent.Trigger(rowCount) // 주기적으로 이벤트 발생
            return makeSymbol addressString comment kind
        }

    /// 비동기적으로 심볼을 생성하는 함수
    let makeGlobalLabelSymbolAsync name dataType (kind: Variable.Kind) =
        async {
            return SymbolInfo(
                    -1,
                    name,
                    "",
                    "",
                    "",
                    int kind,
                    dataType,
                    0,
                    "",
                    false
                )
        }

    /// 전역 심볼을 가져오는 함수
    let getGlobalSymbols (commentDic: CommentDictionary) (globalLabelDic:GlobalLabelDictionary) =
        totalLinesEvent.Trigger(commentDic.Count) // 주기적으로 이벤트 발생
        rowCount <- 0

        let commentSymbols = 
            commentDic
            |> Seq.filter (fun kv -> 
                let dev = kv.Key
                not (dev.StartsWith("U") || dev.StartsWith("J") || dev.StartsWith("P") || dev.StartsWith("I"))
            )
            |> Seq.map (fun (kv) -> makeSymbolAsync kv.Key kv.Value Variable.Kind.VAR_GLOBAL)
            |> Seq.chunkBySize 100 // 병렬 처리 시 한 번에 처리할 작업량 제한
            |> Seq.map (fun batch -> Async.Parallel batch |> Async.RunSynchronously)
            |> Seq.concat
            |> Seq.toList

        let globalLabelSymbols = 
            globalLabelDic
            |> Seq.map (fun (kv) -> makeGlobalLabelSymbolAsync kv.Key kv.Value Variable.Kind.VAR_GLOBAL)
            |> Seq.chunkBySize 100 // 병렬 처리 시 한 번에 처리할 작업량 제한
            |> Seq.map (fun batch -> Async.Parallel batch |> Async.RunSynchronously)
            |> Seq.concat
            |> Seq.toList

        commentSymbols@globalLabelSymbols


    type ArrayInfo(size:int, arrayType:VarType) = 
        member val ArraySize = size with get, set
        member val ArrayType = arrayType with get, set
        
    let createSymbolInfo (parse:MelsecDevice*int*int) address comment varType symbolName kind (isNibble:bool) (arrayInfo:ArrayInfo option) =
        let sortUIIndex = parse |> fun (_, d1, _) -> d1
        let varTypeStr = 
            match arrayInfo with
            |Some arr when varType = VarType.ARRAY -> 
                $"{varType}[0..{arr.ArraySize}] OF {arr.ArrayType}"
            |_-> varType.ToString()

        SymbolInfo(
            sortUIIndex,
            symbolName,
            comment,
            (if isNibble then "A" else XGI.HeadType address),
            (if isNibble then ""  else address),
            kind,
            varTypeStr,
            0,
            (parse |> fun (h, _, _) -> h.ToText),
            isNibble
            )


