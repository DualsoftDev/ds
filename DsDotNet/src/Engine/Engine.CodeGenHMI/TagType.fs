// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.CodeGenHMI

open System

[<AutoOpen>]
module TagTypeModule =

    [<Flags>]
    type TagType =
        | NONE      = 0b0000000000000001
        | Q         = 0b0000000000000010
        | I         = 0b0000000000000100
        | M         = 0b0000000000001000
        | START     = 0b0000000000010000
        | RESET     = 0b0000000000100000
        | END       = 0b0000000001000000
        | GOING     = 0b0000000010000000
        | READY     = 0b0000000100000000
        | AUTO      = 0b0000001000000000
        | FLOW      = 0b0000010000000000
        | EXTERNAL  = 0b0000100000000000
        | PLAN      = 0b0001000000000000
        | ETC       = 0b0010000000000000
        | TX        = 0b0100000000000000
        | RX        = 0b1000000000000000
