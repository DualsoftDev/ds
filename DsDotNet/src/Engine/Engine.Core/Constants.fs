// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System

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
    type EdgeType =
    // runtime edge
    | Default                    = 0b00000000    // Start, Weak
    | Reset                      = 0b00000001    // else start
    | Strong                     = 0b00000010    // else weak
    | AugmentedTransitiveClosure = 0b00000100    // 강한 상호 reset 관계 확장 edge


    // runtime edge 는 Reversed / Bindrectional 을 포함하지 않는다.
    | Reversed                   = 0b00001000    // direction reversed : <, <|, <||, etc
    | Bidirectional              = 0b00010000    // 양방향.  <||>, =>, ...

    | EditorInterlock            = 0b00100000    // 강한 상호 reset 저장 확장 edge
    | EditorStartReset           = 0b01000000    // 약 시작 약 리셋 저장 확장 edge
    | EditorSpare                = 0b10000000    // 추후 사용예약l


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
