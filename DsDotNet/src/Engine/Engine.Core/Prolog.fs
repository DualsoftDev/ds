// Copyright (c) Dualsoft  All Rights Reserved.
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
    do
        if x < 0 || y < 0 then failwithf $"Xywh position x, y can't have negative values [x:{x}, y:{y}]"
        if w.HasValue && w.Value <= 0 then failwithf $"Xywh Width w must be a positive value [Width:{w}]"
        if h.HasValue && h.Value <= 0 then failwithf $"Xywh Height h must be a positive value [Height:{h}]"

    member z.X = x
    member z.Y = y
    member z.W = w
    member z.H = h

