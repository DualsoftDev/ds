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
module DsTextDataType =
    //data
    let [<Literal>] TextBit    = "bit"
    let [<Literal>] TextBool   = "bool"
    let [<Literal>] TextByte   = "byte"
    let [<Literal>] TextWord   = "word"
    let [<Literal>] TextDword  = "dword"
    let [<Literal>] TextLword  = "lword"
    let [<Literal>] TextString = "string"
    let [<Literal>] TextFloat  = "float"
    let [<Literal>] TextSingle = "single"
    let [<Literal>] TextDouble = "double"


[<AutoOpen>]
module DsTextExport =
    //export Excel
    let [<Literal>] TextAddressDev  = "주소"
    let [<Literal>] TextVariable    = "내부"
    let [<Literal>] TextCommand     = "지시"
    let [<Literal>] TextObserve     = "관찰"
    let [<Literal>] TextButton      = "버튼"
    let [<Literal>] TextEmgBtn      = "비상"
    let [<Literal>] TextAutoBtn     = "자동"
    let [<Literal>] TextResetBtn    = "리셋"
    let [<Literal>] TextStartBtn    = "시작"


[<AutoOpen>]
module DsTextProperty =
    //button
 
    let [<Literal>] TextFlow    = "flow"
    let [<Literal>] TextSystem  = "sys"
    let [<Literal>] TextAddress = "addresses"
    let [<Literal>] TextSafety  = "safety"
    let [<Literal>] TextAlias   = "alias"
    let [<Literal>] TextLayout  = "layouts"

    let [<Literal>] TextVariable = "variable"
    let [<Literal>] TextCommand  = "command"
    let [<Literal>] TextObserve  = "observe"
    let [<Literal>] TextCpus     = "cpus"
    let [<Literal>] TextCpu      = "cpu"

