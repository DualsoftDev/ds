// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Linq
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

    /// Runtime Edge Types
    [<Flags>]
    type EdgeType =
        | None                       = 0b00000000    // Invalid state
        | Start                      = 0b00000001    // Start, Weak
        | Reset                      = 0b00000010    // else start
        | Strong                     = 0b00000100    // else weak
        | AugmentedTransitiveClosure = 0b00001000    // 강한 상호 reset 관계 확장 edge


    [<Flags>]
    type ModelingEdgeType =
        | None                       = 0b00000000    // Invalid state
        | Start                      = 0b00000001    // Start, Weak
        | Reset                      = 0b00000010    // else start
        | Strong                     = 0b00000100    // else weak

        | EditorInterlock            = 0b00100000    // 강한 상호 reset 저장 확장 edge
        | EditorStartReset           = 0b01000000    // 약 시작 약 리셋 저장 확장 edge
        | EditorSpare                = 0b10000000    // 추후 사용예약

    type internal MET = ModelingEdgeType
    type internal RET = EdgeType

    let mutable private isFirst = true
    let internal checkEnumSanity() =
        if isFirst then
            isFirst <- false
            let c x = LanguagePrimitives.EnumToValue x
            assert(c RET.None    = c MET.None)
            assert(c RET.Start   = c MET.Start)
            assert(c RET.Reset   = c MET.Reset)
            assert(c RET.Strong  = c MET.Strong)


    type ModelingEdgeInfo<'v>(source:'v, edgeSymbol:string, target:'v) =
        member val Source = source
        member val Target = target
        member val EdgeSymbol = edgeSymbol

    /// source 와 target 을 edge operator 에 따라서 확장 생성
    let expandModelingEdge (modeingEdgeInfo:ModelingEdgeInfo<'v>) : ('v * EdgeType * 'v) list =
        let mi = modeingEdgeInfo
        let s, edgeSymbol, t = mi.Source, mi.EdgeSymbol, mi.Target
        match edgeSymbol with
        | (* ">"    *) TextStartEdge     -> [s, RET.Start, t]
        | (* ">>"   *) TextStartPush     -> [s, RET.Start ||| RET.Strong, t]
        | (* "|>"   *) TextResetEdge     -> [s, RET.Reset, t]
        | (* "||>"  *) TextResetPush     -> [s, RET.Reset ||| RET.Strong, t]

        | (* "=>"   *) TextStartReset    -> [(s, RET.Start, t); (t, RET.Reset, s)]
        | (* "<|>"  *) TextInterlockWeak -> [(s, RET.Reset, t); (t, RET.Reset, s)]
        | (* "<||>" *) TextInterlock     -> [(s, RET.Reset ||| RET.Strong, t); (t, RET.Reset ||| RET.Strong, s)]
        | (* "<"    *) TextStartEdgeRev  -> [t, RET.Start, s]
        | (* "<<"   *) TextStartPushRev  -> [t, RET.Start ||| RET.Strong, s]
        | (* "<|"   *) TextResetEdgeRev  -> [t, RET.Reset, s]
        | (* "<||"  *) TextResetPushRev  -> [t, RET.Reset ||| RET.Strong, s]
        | (* "<="   *) TextStartResetRev -> [(t, RET.Start, s); (s, RET.Reset, t); ]

        | _
            -> failwithf "Unknown causal edge type: %s" edgeSymbol


[<Extension>]
type ModelingEdgeExt =
    [<Extension>] static member IsStart(edgeType:ModelingEdgeType) = edgeType.HasFlag(MET.Reset)|> not
    [<Extension>] static member IsReset(edgeType:ModelingEdgeType) = edgeType.HasFlag(MET.Reset)
    [<Extension>]
    static member ToRuntimeEdge(edgeType:ModelingEdgeType) =
        let et = edgeType
        if et = MET.Start then RET.Start
        elif et = MET.Reset then RET.Reset
        elif et = (MET.Start ||| MET.Strong) then (RET.Start ||| RET.Strong)
        elif et = (MET.Reset ||| MET.Strong) then (RET.Reset ||| RET.Strong)
        else
            failwith "invalid edge type"

    [<Extension>]
    static member ToModelingEdge(edgeType:EdgeType) =
        let et = edgeType
        if et = RET.Start then MET.Start
        elif et = RET.Reset then MET.Reset
        elif et = (RET.Start ||| RET.Strong) then (MET.Start ||| MET.Strong)
        elif et = (RET.Reset ||| RET.Strong) then (MET.Reset ||| MET.Strong)
        else
            failwith "invalid edge type"

    [<Extension>]
    static member ToText(edgeType:EdgeType) =
        checkEnumSanity()
        let t = edgeType
        let isStrong = t.HasFlag(RET.Strong)
        if t.HasFlag(RET.Reset) then
            if isStrong then "||>" else "|>"
        else
            if isStrong then ">>" else ">"

    [<Extension>]
    static member ToText(edgeType:ModelingEdgeType) =
        let t = edgeType
        if t = MET.EditorInterlock then  "<||>"  //EditorInterlock Text 출력우선
        elif t = MET.EditorStartReset then  "=>" //EditorStartReset Reversed 없음
        else
            let isStrong = t.HasFlag(MET.Strong)
            if t.HasFlag(MET.Reset) then
                if isStrong then "||>" else "|>"
            else
                if isStrong then ">>" else ">"

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
    let [<Literal>] TextEmergencyBTN = "emg_in"
    let [<Literal>] TextAutoBTN      = "auto_in"
    let [<Literal>] TextResetBTN     = "reset_in"
    let [<Literal>] TextStartBTN     = "start_in"

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

