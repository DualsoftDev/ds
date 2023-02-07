namespace Engine.Common.FS


[<RequireQualifiedAccess>]
module Result =
    let orElse y x =
        match x with
        | Ok _ -> x
        | _ -> y
    let orElseWith f x =
        match x with
        | Ok _ -> x
        | _ -> f()

    let isOk     = function | Ok _ -> true   | _ -> false
    let isError  = function | Ok _ -> false  | _ -> true

    let toOption = function | Ok v -> Some v | _ -> None
    let toList   = function | Ok v -> [v]    | _ -> []

