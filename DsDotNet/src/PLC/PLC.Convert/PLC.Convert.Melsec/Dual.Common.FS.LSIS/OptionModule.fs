namespace Dual.Common.FS.LSIS

[<RequireQualifiedAccess>]
module Option =
    let ofTuple<'v> (b, v:'v) =
        if b then Some v
        else None

    let toTuple = function
        | Some(v) -> true, v
        | None -> false, null

    let ofResult = function
        | Ok v -> Some v
        | Error _ -> None

    let toResult opt defaultErrorValue =
        match opt with
        | Some v -> Ok v
        | None -> Error defaultErrorValue

    /// 주어진 option 값을 꺼낸다.  None 일 경우를 대비해 default value 를 인자로 준다.
    let flatten defaultValue (opt:Option<'t>) =
        match opt with
        | Some(value) -> value
        | None -> defaultValue

    #if INTERACTIVE
    let a = (true, "a") |> Option.ofTuple
    let b = (false, null)  |> Option.ofTuple<string>
    let n = Int32.TryParse("32") |> Option.ofTuple
    let x = Int32.TryParse("32.7") |> Option.ofTuple
    #endif


    /// 두개의 옵션 인자 a b 가 모두 Some 값일 때에만 그 값들을 꺼내서 f 적용한 값 반환
    let lift f a b =
        match a, b with
        | Some(a), Some(b) -> Some <| f a b
        | _ -> None
    
    #if INTERACTIVE
    Some(1) |> Option.lift (+) (Some(3))
    Some(1) |> Option.lift (+) None
    None |> Option.lift (+) (Some(3))
    
    let a =
      Some(3) |> Option.bind (fun num ->
        Some(2) |> Option.map ((+) num) )
    #endif

