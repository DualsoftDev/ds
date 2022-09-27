// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic
open System.Linq

[<AutoOpen>]
module PrologModule =
    let internal verify (message:string) condition =
        if not condition then
            failwith message

