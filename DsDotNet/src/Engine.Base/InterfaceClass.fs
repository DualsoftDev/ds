// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

[<AutoOpen>]
module InterfaceClass =

    [<AbstractClass>]
    type Named(name)  =
        interface INamed with
            member _.Name: string = name

        member x.ToText() = $"{name}[{x.GetType().Name}]"
