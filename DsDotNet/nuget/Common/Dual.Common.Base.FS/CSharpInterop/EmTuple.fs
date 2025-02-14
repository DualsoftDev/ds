namespace Dual.Common.Base.FS

open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module TupleConvertor =
    ()
type EmTuple =
    [<Extension>] static member DoSomething() = ()
