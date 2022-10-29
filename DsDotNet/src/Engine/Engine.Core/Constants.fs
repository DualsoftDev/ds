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
