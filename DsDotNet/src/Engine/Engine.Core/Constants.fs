// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsText =
    let [<Literal>] TextLibrary   = "DS_Library"
    let [<Literal>] TextDSJson    = "dualsoft.json"
    let [<Literal>] TextEmtpyChannel = "EmtpyChannel" //channel 정보 없는 대상 Dev및 API 
    let [<Literal>] TextImageChannel = "SlideImage"   //cctv url 대신에 pptx의 SlideImage 사용할 경우
    let [<Literal>] TextFuncNotUsed= "NotUsed"  //함수 사용안함
    let [<Literal>] TextSkip      = "-"  //주소 스킵 처리
    let [<Literal>] TextAddrEmpty = "_"  //주소 없음 Error 대상
    //edge
    let [<Literal>] TextStartEdge         = ">"
    let [<Literal>] TextStartPush         = ">>"
    let [<Literal>] TextResetEdge         = "|>"
    let [<Literal>] TextResetPush         = "||>"
    let [<Literal>] TextStartReset        = "=>"
    let [<Literal>] TextInterlockWeak     = "<|>"
    let [<Literal>] TextInterlockStrong   = "<||>"
    let [<Literal>] TextStartEdgeRev      = "<"
    let [<Literal>] TextStartPushRev      = "<<"
    let [<Literal>] TextResetEdgeRev      = "<|"
    let [<Literal>] TextResetPushRev      = "<||"
    let [<Literal>] TextStartResetRev     = "<="


    /// 사용자 모델링한 edge type (단 reverse 는 반대로 뒤집어서)
    type ModelingEdgeType =
        | StartEdge          (*  ">"    *)
        | StartPush          (*  ">>"   *)
        | ResetEdge          (*  "|>"   *)
        | ResetPush          (*  "||>"  *)
        | StartReset         (*  "=>"   *)
        | InterlockWeak      (*  "<|>"  *)
        | InterlockStrong    (*  "<||>" *)

    /// Runtime Edge Types
    [<Flags>]
    type EdgeType =
        | None                       = 0b00000000    // Invalid state
        | Start                      = 0b00000001    // Start, Weak
        | Reset                      = 0b00000010    // else start
        | Strong                     = 0b00000100    // else weak
        | AugmentedTransitiveClosure = 0b00001000    // 강한 상호 reset 관계 확장 edge

    type internal MET = ModelingEdgeType
    type internal RET = EdgeType

    type ModelingEdgeInfo<'v>(sources:'v seq, edgeSymbol:string, targets:'v seq) =
        new(source, edgeSymbol, target) = ModelingEdgeInfo([source], edgeSymbol, [target])
        member val Sources = sources.ToFSharpList()
        member val Targets = targets.ToFSharpList()
        member val EdgeSymbol = edgeSymbol
        member x.EdgeRevSymbol =  match edgeSymbol with
                                    | TextStartEdge       ->    TextStartEdgeRev
                                    | TextStartPush       ->    TextStartPushRev
                                    | TextResetEdge       ->    TextResetEdgeRev
                                    | TextResetPush       ->    TextResetPushRev
                                    | TextStartReset      ->    TextStartResetRev
                                    |  TextStartEdgeRev      ->    TextStartEdge 
                                    |  TextStartPushRev      ->    TextStartPush 
                                    |  TextResetEdgeRev      ->    TextResetEdge 
                                    |  TextResetPushRev      ->    TextResetPush 
                                    |  TextStartResetRev     ->   TextStartReset 
                                    |_ -> failwith $"{edgeSymbol} symbol is directionless"

    /// source 와 target 을 edge operator 에 따라서 확장 생성
    let expandModelingEdge (modelingEdgeInfo:ModelingEdgeInfo<'v>) : ('v * EdgeType * 'v) list =
        let mi = modelingEdgeInfo
        let ss, edgeSymbol, ts = mi.Sources, mi.EdgeSymbol, mi.Targets
        [
            for s in ss do
            for t in ts do
                yield!
                    match edgeSymbol with
                    | (* ">"    *) TextStartEdge     -> [(s, RET.Start, t)]
                    | (* ">>"   *) TextStartPush     -> [(s, RET.Start ||| RET.Strong, t)]
                    | (* "|>"   *) TextResetEdge     -> [(s, RET.Reset, t)]
                    | (* "||>"  *) TextResetPush     -> [(s, RET.Reset ||| RET.Strong, t)]

                    | (* "=>"   *) TextStartReset    -> [(s, RET.Start, t); (t, RET.Reset, s)]
                    | (* "<|>"  *) TextInterlockWeak -> [(s, RET.Reset, t); (t, RET.Reset, s)]
                    | (* "<||>" *) TextInterlockStrong     -> [(s, RET.Reset ||| RET.Strong, t); (t, RET.Reset ||| RET.Strong, s)]
                    | (* "<"    *) TextStartEdgeRev  -> [(t, RET.Start, s)]
                    | (* "<<"   *) TextStartPushRev  -> [(t, RET.Start ||| RET.Strong, s)]
                    | (* "<|"   *) TextResetEdgeRev  -> [(t, RET.Reset, s)]
                    | (* "<||"  *) TextResetPushRev  -> [(t, RET.Reset ||| RET.Strong, s)]
                    | (* "<="   *) TextStartResetRev -> [(t, RET.Start, s); (s, RET.Reset, t); ]

                    | _
                        -> failwithf "Unknown causal edge type: %s" edgeSymbol
        ]


[<Extension>]
type ModelingEdgeExt =
    [<Extension>]
    static member ToText(edgeType:EdgeType) =
        let t = edgeType
        let isStrong = t.HasFlag(RET.Strong)
        if t.HasFlag(RET.Reset) then
            if isStrong then "||>" else "|>"
        else
            if isStrong then ">>" else ">"

    [<Extension>]
    static member ToText(edgeType:ModelingEdgeType) =
        match edgeType with
        | StartEdge       ->    TextStartEdge
        | StartPush       ->    TextStartPush
        | ResetEdge       ->    TextResetEdge
        | ResetPush       ->    TextResetPush
        | StartReset      ->    TextStartReset
        | InterlockWeak   ->    TextInterlockWeak
        | InterlockStrong ->    TextInterlockStrong

    [<Extension>]
    static member ToModelEdge(edgeText:string) =
        match edgeText with
        | TextStartEdge       ->    StartEdge
        | TextStartPush       ->    StartPush
        | TextResetEdge       ->    ResetEdge
        | TextResetPush       ->    ResetPush
        | TextStartReset      ->    StartReset
        | TextInterlockWeak   ->    InterlockWeak
        | TextInterlockStrong ->    InterlockStrong
        |_ -> failwithf $"'{edgeText}' is not modelEdgeType"

    /// 뒤집힌 edge 판정.  뒤집혀 있으면 source target 을 반대로 하고 edge 를 다시 뒤집을 것.
    [<Extension>]
    static member IsReversedEdge(edgeText:string) =
        match edgeText with
        | TextStartEdgeRev
        | TextStartPushRev
        | TextResetEdgeRev
        | TextResetPushRev
        | TextStartResetRev -> true
        | _ -> false


[<AutoOpen>]
module DsDataType =
    //data 타입 지원 항목 : 알파벳 순 정렬 (Alt+Shift+L, Alt+Shift+S)
    let [<Literal>] BOOL    = "Boolean"
    let [<Literal>] CHAR    = "Char"
    let [<Literal>] FLOAT32 = "Single"
    let [<Literal>] FLOAT64 = "Double"
    let [<Literal>] INT16   = "Int16"
    let [<Literal>] INT32   = "Int32"
    let [<Literal>] INT64   = "Int64"
    let [<Literal>] INT8    = "SByte"
    let [<Literal>] STRING  = "String"
    let [<Literal>] UINT16  = "UInt16"
    let [<Literal>] UINT32  = "UInt32"
    let [<Literal>] UINT64  = "UInt64"
    let [<Literal>] UINT8   = "Byte"

    let typeDefaultValue (typ:System.Type) =
        match typ.Name with
        | BOOL      -> box false
        | CHAR      -> box ' '
        | FLOAT32   -> box 0.0f
        | FLOAT64   -> box 0.0
        | INT16     -> box 0s
        | INT32     -> box 0
        | INT64     -> box 0L
        | INT8      -> box 0y
        | STRING    -> box ""
        | UINT16    -> box 0us
        | UINT32    -> box 0u
        | UINT64    -> box 0UL
        | UINT8     -> box 0uy
        | _  -> failwithlog "ERROR"

    type DataType =
        | DuBOOL
        | DuCHAR
        | DuFLOAT32
        | DuFLOAT64
        | DuINT16
        | DuINT32
        | DuINT64
        | DuINT8
        | DuSTRING
        | DuUINT16
        | DuUINT32
        | DuUINT64
        | DuUINT8

        member x.ToText() =
            match x with
            | DuBOOL    -> BOOL
            | DuCHAR    -> CHAR
            | DuFLOAT32 -> FLOAT32
            | DuFLOAT64 -> FLOAT64
            | DuINT16   -> INT16
            | DuINT32   -> INT32
            | DuINT64   -> INT64
            | DuINT8    -> INT8
            | DuSTRING  -> STRING
            | DuUINT16  -> UINT16
            | DuUINT32  -> UINT32
            | DuUINT64  -> UINT64
            | DuUINT8   -> UINT8


        member x.ToTextLower() = x.ToText().ToLower()
        member x.ToBlockSizeNText() = 
            match x with
            | DuUINT16  -> 16, "Word"
            | DuUINT32  -> 32, "DWord"
            | DuUINT64  -> 64, "LWord"
            | DuUINT8   -> 8 , "Byte"
            | _ -> failwithf $"'{x}' not support ToBlockSize"

        member x.ToType() =
            match x with
            | DuBOOL    -> typedefof<bool>
            | DuCHAR    -> typedefof<char>
            | DuFLOAT32 -> typedefof<single>
            | DuFLOAT64 -> typedefof<double>
            | DuINT16   -> typedefof<int16>
            | DuINT32   -> typedefof<int32>
            | DuINT64   -> typedefof<int64>
            | DuINT8    -> typedefof<int8>
            | DuSTRING  -> typedefof<string>
            | DuUINT16  -> typedefof<uint16>
            | DuUINT32  -> typedefof<uint32>
            | DuUINT64  -> typedefof<uint64>
            | DuUINT8   -> typedefof<uint8>

        member x.ToValue(value:string) =
            match x with
            | DuBOOL    -> value |> Convert.ToBoolean |> box       
            | DuCHAR    -> value |> Convert.ToChar    |> box 
            | DuFLOAT32 -> value |> Convert.ToSingle  |> box 
            | DuFLOAT64 -> value |> Convert.ToDouble  |> box 
            | DuINT16   -> value |> Convert.ToInt16   |> box 
            | DuINT32   -> value |> Convert.ToInt32   |> box 
            | DuINT64   -> value |> Convert.ToInt64   |> box 
            | DuINT8    -> value |> Convert.ToSByte   |> box 
            | DuSTRING  -> value                      |> box 
            | DuUINT16  -> value |> Convert.ToUInt16  |> box 
            | DuUINT32  -> value |> Convert.ToUInt32  |> box 
            | DuUINT64  -> value |> Convert.ToUInt64  |> box 
            | DuUINT8   -> value |> Convert.ToByte    |> box 

        member x.DefaultValue() = typeDefaultValue (x.ToType())

    type IOType = | In | Out | Memory | NotUsed

    type SlotDataType = int *IOType * DataType

    let getBlockType(blockSlottype:string) =
        match blockSlottype.ToLower() with
        | "byte"  -> DuUINT8
        | "word"  -> DuUINT16
        | "dword" -> DuUINT32
        | "lword" -> DuUINT64
        | _ -> failwithf $"'size bit {blockSlottype}' not support getBlockType"


    let textToDataType(typeName:string) =
        match typeName.ToLower() with
        //system1   | system2   | plc
        | "boolean" | "bool"    | "bit"  ->  DuBOOL
        | "char"                         ->  DuCHAR
        | "float32" | "single"           ->  DuFLOAT32
        | "float64" | "double"           ->  DuFLOAT64
        | "int16"   | "short"            ->  DuINT16
        | "int32"   | "int"              ->  DuINT32
        | "int64"   | "long"             ->  DuINT64
        | "int8"    | "sbyte"            ->  DuINT8
        | "string"                       ->  DuSTRING
        | "uint16"  | "ushort"  |"word"  ->  DuUINT16
        | "uint32"  | "uint"    |"dword" ->  DuUINT32
        | "uint64"  | "ulong"   |"lword" ->  DuUINT64
        | "uint8"   | "byte"    |"byte"  ->  DuUINT8
        | _ -> failwithf $"'{typeName}' DataToType Error check type"


[<AutoOpen>]
module DsTextExport =
    //export Excel
    let [<Literal>] TextXlsAddress           = "외부주소"
    let [<Literal>] TextXlsVariable          = "내부변수"
    let [<Literal>] TextXlsOperator          = "내부연산"
    let [<Literal>] TextXlsCommand           = "내부명령"
    let [<Literal>] TextXlsAutoBTN           = "자동셀렉트"
    let [<Literal>] TextXlsManualBTN         = "수동셀렉트"
    let [<Literal>] TextXlsDriveBTN          = "운전푸쉬버튼"
    let [<Literal>] TextXlsPauseBTN          = "정지푸쉬버튼"
    let [<Literal>] TextXlsClearBTN          = "해지푸쉬버튼"
    let [<Literal>] TextXlsEmergencyBTN      = "비상푸쉬버튼"
    let [<Literal>] TextXlsTestBTN           = "시운전푸쉬버튼"
    let [<Literal>] TextXlsHomeBTN           = "복귀푸쉬버튼"
    let [<Literal>] TextXlsReadyBTN          = "준비푸쉬버튼"

    let [<Literal>] TextXlsAutoLamp          = "자동모드램프"
    let [<Literal>] TextXlsManualLamp        = "수동모드램프"
    let [<Literal>] TextXlsDriveLamp         = "운전모드램프"
    let [<Literal>] TextXlsErrorLamp         = "이상모드램프"
    let [<Literal>] TextXlsEmergencyLamp     = "비상모드램프"
    let [<Literal>] TextXlsTestLamp          = "시운전모드램프"
    let [<Literal>] TextXlsReadyLamp         = "준비모드램프"
    let [<Literal>] TextXlsIdleLamp          = "대기모드램프"
    let [<Literal>] TextXlsHomingLamp        = "원위치중램프"
    let [<Literal>] TextXlsConditionReady    = "준비조건"

[<AutoOpen>]
module DsTextProperty =

    let [<Literal>] TextFlow    = "flow"
    let [<Literal>] TextSystem  = "sys"
    let [<Literal>] TextAddress = "addresses"
    let [<Literal>] TextSafety  = "safety"
    let [<Literal>] TextAlias   = "alias"
    let [<Literal>] TextLayout  = "layouts"
    let [<Literal>] TextJobs    = "jobs"
    let [<Literal>] TextDevice  = "device"


[<AutoOpen>]
module DsTextFunction =

    let [<Literal>] TextMove    = "m"
    let [<Literal>] TextOnDelayTimer = "t"
    let [<Literal>] TextRingCounter = "c"
    let [<Literal>] TextNot = "n"
    let [<Literal>] TextEQ = "=="
    let [<Literal>] TextNotEQ = "!="
    let [<Literal>] TextGT = ">"
    let [<Literal>] TextGTE = ">="
    let [<Literal>] TextLT = "<"
    let [<Literal>] TextLTE = "<="
