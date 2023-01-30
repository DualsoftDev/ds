namespace Old.Dual.Common

[<RequireQualifiedAccess>]
module Result =
    let isOk = function
        | Ok _ -> true
        | Error _ -> false

    let isError r = not <| isOk r

    /// Result 를 Optin type 으로 변경
    let toOption = function
        | Ok r -> Some r
        | Error _ -> None

    /// Option type 을 Result type 으로 변경
    let ofOption opt defaultErrorValue =
        match opt with
        | Some v -> Ok v
        | None -> Error defaultErrorValue

    /// Result 로부터 OK value 를 반환.  Error case 에는 exception
    let get = function
        | Ok r -> r
        | Error err -> failwithf "Try to get fail result: %A" err
