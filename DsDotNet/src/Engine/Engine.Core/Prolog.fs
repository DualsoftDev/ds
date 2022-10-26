// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System

[<AutoOpen>]
module PrologModule =
    /// verify with message
    let internal verifyM (message:string) condition =
        if not condition then
            failwith message
    let internal verifyMessage (message:string) condition =
        verifyM message condition
        condition

[<AllowNullLiteral>]
type Xywh(x:int, y:int, w:Nullable<int>, h:Nullable<int>) =
    member z.X = x
    member z.Y = y
    member z.W = w
    member z.H = h

[<AllowNullLiteral>]
type Addresses(inAddress:string, outAddress:string) =
    member x.In = inAddress
    member x.Out = outAddress
