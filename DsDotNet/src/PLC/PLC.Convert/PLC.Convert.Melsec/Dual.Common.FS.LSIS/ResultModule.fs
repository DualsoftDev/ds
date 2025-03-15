namespace Dual.Common.FS.LSIS

[<RequireQualifiedAccess>]
module Result =
    let isOk = function
        | Ok _ -> true
        | Error _ -> false

    let isError r = not <| isOk r

    let toOption = function
        | Ok r -> Some r
        | Error _ -> None

    let ofOption opt defaultErrorValue =
        match opt with
        | Some v -> Ok v
        | None -> Error defaultErrorValue

    /// 모두 OK 인지 검사.  하나라도 Error 이면 Error
    let allOk r1 r2 =
        match r1 with
        | Ok _ -> r2
        | Error _ -> r1

    /// OK 나올 때까지 수행한 결과
    let anyOk r1 r2 =
        match r1 with
        | Ok _ -> r1
        | Error _ -> r2

    /// Error 나올 때까지 f() 수행
    let stopOnError f r =
        match r with
        | Ok r -> f()
        | Error _ -> r
