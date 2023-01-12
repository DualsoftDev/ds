namespace Engine.Core

[<AutoOpen>]
module RuntimeGeneratorModule =
    type RuntimeTarget = WINDOWS | XGI | XGK | AB | MELSEC
    let mutable RuntimeTarget = WINDOWS

