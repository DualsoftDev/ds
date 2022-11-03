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
    
    [<Flags>]
    type ModelEdgeType =
        | StartEdge          (*  ">"    *)
        | StartPush          (*  ">>"   *)
        | ResetEdge          (*  "|>"   *)
        | ResetPush          (*  "||>"  *)
        | StartReset         (*  "=>"   *)
        | InterlockWeak      (*  "<|>"  *)
        | Interlock          (*  "<||>" *)
        | StartEdgeRev       (*  "<"    *)
        | StartPushRev       (*  "<<"   *)
        | ResetEdgeRev       (*  "<|"   *)
        | ResetPushRev       (*  "<||"  *)
        | StartResetRev      (*  "<="   *)
        
    /// Runtime Edge Types
    [<Flags>]
    type EdgeType =
        | Default                    = 0b00000000    // Start, Weak
        | Reset                      = 0b00000001    // else start
        | Strong                     = 0b00000010    // else weak
        | AugmentedTransitiveClosure = 0b00000100    // 강한 상호 reset 관계 확장 edge


    type internal MET = ModelEdgeType
    type internal RET = EdgeType

    /// source 와 target 을 edge operator 에 따라서 확장 생성
    let expandModelingEdge (source:'v) (edgeSymbol:string) (target:'v) : ('v * EdgeType * 'v) list =
        let s, t = source, target
        match edgeSymbol with
        | (* ">"    *) TextStartEdge     -> [s, RET.Default, t]
        | (* ">>"   *) TextStartPush     -> [s, RET.Default ||| RET.Strong, t]
        | (* "|>"   *) TextResetEdge     -> [s, RET.Reset, t]
        | (* "||>"  *) TextResetPush     -> [s, RET.Reset ||| RET.Strong, t]

        | (* "=>"   *) TextStartReset    -> [(s, RET.Default, t); (t, RET.Reset, s)]
        | (* "<|>"  *) TextInterlockWeak -> [(s, RET.Reset, t); (t, RET.Reset, s)]
        | (* "<||>" *) TextInterlock     -> [(s, RET.Reset ||| RET.Strong, t); (t, RET.Reset ||| RET.Strong, s)]
        | (* "<"    *) TextStartEdgeRev  -> [t, RET.Default, s]
        | (* "<<"   *) TextStartPushRev  -> [t, RET.Default ||| RET.Strong, s]
        | (* "<|"   *) TextResetEdgeRev  -> [t, RET.Reset, s]
        | (* "<||"  *) TextResetPushRev  -> [t, RET.Reset ||| RET.Strong, s]
        | (* "<="   *) TextStartResetRev -> [(t, RET.Default, s); (s, RET.Reset, t); ]

        | _
            -> failwithf "Unknown edge symbol: %s" edgeSymbol



[<Extension>]
type ModelingEdgeExt =
    
    [<Extension>]
    static member ToText(edgeType:EdgeType) =
        //checkEnumSanity()
        let t = edgeType
        if t.HasFlag(RET.Reset) then
            if t.HasFlag(RET.Strong) then "||>" else "|>"
        else
            if t.HasFlag(RET.Strong) then ">>" else ">"

    [<Extension>]
    static member ToText(edgeType:ModelEdgeType) =
        match edgeType with
        | StartEdge       ->    TextStartEdge     
        | StartPush       ->    TextStartPush     
        | ResetEdge       ->    TextResetEdge     
        | ResetPush       ->    TextResetPush     
        | StartReset      ->    TextStartReset    
        | InterlockWeak   ->    TextInterlockWeak 
        | Interlock       ->    TextInterlock     
        | StartEdgeRev    ->    TextStartEdgeRev  
        | StartPushRev    ->    TextStartPushRev  
        | ResetEdgeRev    ->    TextResetEdgeRev  
        | ResetPushRev    ->    TextResetPushRev  
        | StartResetRev   ->    TextStartResetRev 

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
        | TextStartEdgeRev    ->    StartEdgeRev  
        | TextStartPushRev    ->    StartPushRev  
        | TextResetEdgeRev    ->    ResetEdgeRev  
        | TextResetPushRev    ->    ResetPushRev  
        | TextStartResetRev   ->    StartResetRev 
        |_ -> failwithf $"'{edgeText}' is not modelEdgeType"


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

