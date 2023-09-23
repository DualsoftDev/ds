// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System

[<AutoOpen>]
module PrologModule =
    /// Verifies a condition and throws an exception if not met.
    let verifyM (message: string) condition =
        if not condition then
            failwith message

    /// Verifies a condition and returns the condition's state.
    let verifyMessage (message: string) condition =
        verifyM message condition
        condition

[<AllowNullLiteral>]
type Xywh(x: int, y: int, w: Nullable<int>, h: Nullable<int>) =
    /// X coordinate
    member z.X = x
    /// Y coordinate
    member z.Y = y
    /// Width
    member z.W = w
    /// Height
    member z.H = h

[<AllowNullLiteral>]
type Addresses(inAddress: string, outAddress: string) =
    /// Input address
    member x.In = inAddress
    /// Output address
    member x.Out = outAddress
