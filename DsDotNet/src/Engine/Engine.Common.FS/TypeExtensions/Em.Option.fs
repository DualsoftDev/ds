namespace Engine.Common.FS

open System.Runtime.CompilerServices

[<Extension>] // type OptionExt =
type OptionExt =
    [<Extension>] static member GetValue<'T>(xo:'T option) = Option.get xo
    [<Extension>] static member Tap<'T>(xo:'T option, f) = Option.iter f xo; xo

    [<Extension>] static member Bind(xo, f)     = Option.bind f xo
    [<Extension>] static member Contains(xo, v) = Option.contains v xo
    [<Extension>] static member Count(xo)       = Option.count xo
    [<Extension>] static member Exists(xo, f)   = Option.exists f xo
    [<Extension>] static member Filter(xo, f)   = Option.filter f xo
    [<Extension>] static member Flatten(xooo)   = Option.flatten xooo
    [<Extension>] static member DefaultValue(xo, defaultValue) = Option.defaultValue defaultValue xo
    [<Extension>] static member DefaultWith(xo, f)     = Option.defaultWith f xo
    [<Extension>] static member Iter(xo, f)            = Option.iter f xo
    [<Extension>] static member Map(xo, f)             = Option.map f xo
    [<Extension>] static member OrElse(xo, coverValue) = Option.orElse coverValue xo
    [<Extension>] static member OrElseWith(xo, f) = Option.orElseWith f xo
    [<Extension>] static member ToArray(xo)       = Option.toArray xo
    [<Extension>] static member ToList(xo)        = Option.toList xo
    [<Extension>] static member ToNullable(xo)    = Option.toNullable xo
    [<Extension>] static member ToObj(xo)         = Option.toObj xo
    [<Extension>] static member ToOption(v)     = Option.ofNullable v
    [<Extension>] static member ToOption(obj)   = Option.ofObj obj

    (* Fail to define Cast for option. *)
    //[<Extension>] static member Cast<'T>(xo):'T option = Option.map (forceCast<'T>) xo
    //[<Extension>] static member Cast<'T>(xo):'T option =
    //    match xo with
    //    | None -> None
    //    | Some o when isType<'T> o -> Some (forceCast<'T> o)
    //    | _ -> failwith "Casting ERROR"

[<Extension>] // type OptionExt =
type ResultExt =
    [<Extension>] static member Bind(xr, f) = Result.bind f xr
    [<Extension>] static member Map(xr, f)  = Result.map f xr
