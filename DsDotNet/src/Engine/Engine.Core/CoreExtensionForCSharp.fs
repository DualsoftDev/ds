namespace Engine.Core

open System.Runtime.CompilerServices


/// Only for C# export
type CoreExtensionForCSharp =
    [<Extension>] static member CsGetReferenceSystem(loadedSystem:LoadedSystem) = loadedSystem.ReferenceSystem


