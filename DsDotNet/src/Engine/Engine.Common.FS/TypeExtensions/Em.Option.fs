namespace Engine.Common.FS

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices

[<Extension>] // type OptionExt =
type OptionExt =
    [<Extension>] static member Map(x, f) = x |> Option.map f
    [<Extension>] static member Bind(x, f) = x |> Option.bind f
    [<Extension>] static member Contains(x, y) = x |> Option.contains y
    [<Extension>] static member DefaultValue(x, y) = x |> Option.defaultValue y
    [<Extension>] static member GetValue<'T>(x:'T option) = x |> Option.get
    [<Extension>] static member Tap<'T>(x:'T option, f) = x |> Option.iter f; x


