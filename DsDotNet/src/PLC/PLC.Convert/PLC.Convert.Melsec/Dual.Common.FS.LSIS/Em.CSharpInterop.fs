module Dual.Common.FS.LSIS.Interop

open System

/// C# Nullable to F# Option : value type 에만 적용됨
let NullableToOption (n:Nullable<_>) =
   if n.HasValue
   then Some n.Value
   else None

/// F# option to C# Nullable
let OptionToNullable (o:option<_>) =
    match o with
    | Some v -> Nullable<_>(v)
    | None -> Nullable<_>()

/// F# option to nullable reference
let OptionToReference (o:option<_>) =
    match o with
    | Some v -> v
    | None -> null


/// C# Nullable to F# Option
let n2o nullable = NullableToOption nullable

/// F# option to C# Nullable : value type 이 아닌 경우, compile error 발생
let o2n optVal = OptionToNullable optVal

/// F# option to C# reference : reference type 이 아닌 경우, compile error 발생
let o2r optVal = OptionToReference optVal
