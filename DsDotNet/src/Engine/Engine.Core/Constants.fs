// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open Engine.Common.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsText =
    //edge
    let [<Literal>] TextStartEdge         = ">"
    let [<Literal>] TextStartPush         = ">>"
    let [<Literal>] TextResetEdge         = "|>"
    let [<Literal>] TextResetPush         = "||>"
    let [<Literal>] TextStartReset        = "=>"
    let [<Literal>] TextInterlockWeak     = "<|>"
    let [<Literal>] TextInterlock         = "<||>"
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
        | Interlock          (*  "<||>" *)

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

    /// source 와 target 을 edge operator 에 따라서 확장 생성
    let expandModelingEdge (modeingEdgeInfo:ModelingEdgeInfo<'v>) : ('v * EdgeType * 'v) list =
        let mi = modeingEdgeInfo
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
                    | (* "<||>" *) TextInterlock     -> [(s, RET.Reset ||| RET.Strong, t); (t, RET.Reset ||| RET.Strong, s)]
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
        | Interlock       ->    TextInterlock

    [<Extension>]
    static member ToModelEdge(edgeText:string) =
        match edgeText with
        | TextStartEdge       ->    StartEdge
        | TextStartPush       ->    StartPush
        | TextResetEdge       ->    ResetEdge
        | TextResetPush       ->    ResetPush
        | TextStartReset      ->    StartReset
        | TextInterlockWeak   ->    InterlockWeak
        | TextInterlock       ->    Interlock
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
    //data 타입 지원 항목
    let [<Literal>] FLOAT32 = "float32"
    let [<Literal>] FLOAT64 = "float64"
    let [<Literal>] INT8    = "int8"
    let [<Literal>] UINT8   = "uint8"
    let [<Literal>] INT16   = "int16"
    let [<Literal>] UINT16  = "uint16"
    let [<Literal>] INT32   = "int32"
    let [<Literal>] UINT32  = "uint32"
    let [<Literal>] INT64   = "int64"
    let [<Literal>] UINT64  = "uint64"
    let [<Literal>] STRING  = "string"
    let [<Literal>] CHAR    = "char"
    let [<Literal>] BOOL    = "bool"

    type DataType =
        | DuFLOAT32
        | DuFLOAT64
        | DuINT8   
        | DuUINT8  
        | DuINT16  
        | DuUINT16 
        | DuINT32  
        | DuUINT32 
        | DuINT64  
        | DuUINT64 
        | DuSTRING 
        | DuCHAR   
        | DuBOOL
        member x.ToText() =
            match x with
            | DuFLOAT32 -> FLOAT32   
            | DuFLOAT64 -> FLOAT64   
            | DuINT8    -> INT8      
            | DuUINT8   -> UINT8     
            | DuINT16   -> INT16     
            | DuUINT16  -> UINT16    
            | DuINT32   -> INT32     
            | DuUINT32  -> UINT32    
            | DuINT64   -> INT64     
            | DuUINT64  -> UINT64    
            | DuSTRING  -> STRING    
            | DuCHAR    -> CHAR      
            | DuBOOL    -> BOOL      
    
    let DataToType(txt:string) =
        match txt.ToLower() with
        //system1 | system2   | plc
        | FLOAT32 | "single"           ->  DuFLOAT32
        | FLOAT64 | "double"           ->  DuFLOAT64
        | INT8    | "sbyte"            ->  DuINT8   
        | UINT8   | "byte"    |"byte"  ->  DuUINT8  
        | INT16   | "short"   |"byte"  ->  DuINT16  
        | UINT16  | "ushort"  |"word"  ->  DuUINT16 
        | INT32   | "int"              ->  DuINT32  
        | UINT32  | "uint"    |"dword" ->  DuUINT32 
        | INT64   | "long"             ->  DuINT64  
        | UINT64  | "ulong"   |"lword" ->  DuUINT64 
        | STRING                       ->  DuSTRING 
        | CHAR                         ->  DuCHAR   
        | BOOL    | "boolean" | "bit"  ->  DuBOOL       
        | _ -> failwithf $"'{txt}' DataToType Error check type"


[<AutoOpen>]
module DsTextExport =
    //export Excel
    let [<Literal>] TextXlsAddress           = "외부주소"
    let [<Literal>] TextXlsVariable          = "내부변수"
                                             
    let [<Literal>] TextXlsAutoBTN           = "자동선택"
    let [<Literal>] TextXlsManualBTN         = "수동선택"
    let [<Literal>] TextXlsDriveBTN          = "운전푸쉬"
    let [<Literal>] TextXlsStopBTN           = "정지푸쉬"
    let [<Literal>] TextXlsClearBTN          = "해지푸쉬"
    let [<Literal>] TextXlsEmergencyBTN      = "비상푸쉬"
    let [<Literal>] TextXlsTestBTN           = "시운전푸쉬"
    let [<Literal>] TextXlsHomeBTN           = "복귀푸쉬"
    let [<Literal>] TextXlsReadyBTN          = "준비푸쉬"
                                             
    let [<Literal>] TextXlsAutoModeLamp      = "자동램프"
    let [<Literal>] TextXlsManualModeLamp    = "수동램프"
    let [<Literal>] TextXlsDriveModeLamp     = "운전램프"
    let [<Literal>] TextXlsStopModeLamp      = "정지램프"
    let [<Literal>] TextXlsEmergencyModeLamp = "비상램프"
    let [<Literal>] TextXlsTestModeLamp      = "시운전램프"
    let [<Literal>] TextXlsReadyModeLamp     = "준비램프"

[<AutoOpen>]
module DsTextProperty =
 
    let [<Literal>] TextFlow    = "flow"
    let [<Literal>] TextSystem  = "sys"
    let [<Literal>] TextAddress = "addresses"
    let [<Literal>] TextSafety  = "safety"
    let [<Literal>] TextAlias   = "alias"
    let [<Literal>] TextLayout  = "layouts"
    let [<Literal>] TextJobs    = "jobs"


[<AutoOpen>]
module DsTextFunction =
 
    let [<Literal>] TextMove    = "m"
    let [<Literal>] TextOnDelayTimer = "t"
    let [<Literal>] TextRingCounter = "c"
    let [<Literal>] TextNot = "n"
    let [<Literal>] TextEQ = "="
    let [<Literal>] TextNotEQ = "!="
    let [<Literal>] TextGT = ">"
    let [<Literal>] TextGTE = ">="
    let [<Literal>] TextLT = "<"
    let [<Literal>] TextLTE = "<="
