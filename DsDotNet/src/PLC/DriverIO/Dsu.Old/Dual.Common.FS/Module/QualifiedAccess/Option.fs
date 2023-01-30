namespace Old.Dual.Common

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

    let toResult defaultErrorValue = function
        | Some v -> Ok v
        | None -> Error defaultErrorValue

    // reminders...
    let ofObj2        = Option.ofObj
    let ofNullable2   = Option.ofNullable
    let toObj2        = Option.toObj
    let toNullable2   = Option.toNullable
    let defaultValue2 = Option.defaultValue
    let defaultWith2  = Option.defaultWith

    /// F# option to nullable reference
    let toReference = function
        | Some v -> v
        | None -> null

    let toList = function
        | Some v -> [v]
        | None -> []

    /// 주어진 option 값을 꺼낸다.  None 일 경우를 대비해 default value 를 인자로 준다.
    let flatten defaultValue = function
        | Some(value) -> value
        | None -> defaultValue


    //let rec flattenRecursively defaultValue = function
    //    | Some(value) ->
    //        if value.GetType() = typedefof<Option<_>> then
    //            flattenRecursively defaultValue (value :?> Option<_>)
    //        else
    //            value
    //        //match value with
    //        //| :? Option<_> as ov -> //value.GetType() = typedefof<Option<_>> -> flattenRecursively defaultValue value
    //        //    flattenRecursively defaultValue ov
    //    | None -> defaultValue

    #if INTERACTIVE
    let a = (true, "a") |> Option.ofTuple
    let b = (false, null)  |> Option.ofTuple<string>
    let n = Int32.TryParse("32") |> Option.ofTuple
    let x = Int32.TryParse("32.7") |> Option.ofTuple
    #endif

    let lift1 = Option.map

    /// 두개의 옵션 인자 a, b 가 모두 Some 값일 때에만 그 값들을 꺼내서 f 적용한 값 반환
    let lift2 f a b =
        match a, b with
        | Some(aa), Some(bb) -> Some <| f aa bb
        | _ -> None

    /// 세개의 옵션 인자 a, b, c 가 모두 Some 값일 때에만 그 값들을 꺼내서 f 적용한 값 반환
    let lift3 f a b c =
        match a, b, c with
        | Some(aa), Some(bb), Some(cc) -> Some <| f aa bb cc
        | _ -> None
    
    #if INTERACTIVE
    Some(1) |> Option.lift2 (+) (Some(3))
    Some(1) |> Option.lift2 (+) None
    None |> Option.lift2 (+) (Some(3))
    
    let a =
      Some(3) |> Option.bind (fun num ->
        Some(2) |> Option.map ((+) num) )
    #endif

    let cast<'a> = tryCast<'a>

//[<AutoOpen>]
module OptionModule =
    /// F# option to C# reference : reference type 이 아닌 경우, compile error 발생
    let o2r optVal = Option.toReference optVal

    // https://riptutorial.com/fsharp/example/16297/how-to-compose-values-and-functions-using-common-operators

    /// If 't' has Some value then return t otherwise return u
    let (<|>) t u =
        match t with
        | Some _  -> t
        | None    -> u

    /// If 't' and 'u' has Some values then return Some (tv*uv) otherwise return None
    let (<*>) t u =
        match t, u with
        | Some tv, Some tu  -> Some (tv, tu)
        | _                 -> None
