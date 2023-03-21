namespace Engine.Common.FS

open System

[<RequireQualifiedAccess>]
module Option =
    // reminders...
    let private ofObj2        = Option.ofObj
    let private ofNullable2   = Option.ofNullable
    let private toObj2        = Option.toObj
    let private toNullable2   = Option.toNullable
    let private defaultValue2 = Option.defaultValue
    let private defaultWith2  = Option.defaultWith

    let ofString (str:string) =
        match str with
        | IsItNullOrEmpty str -> None
        | _ -> Some str
    let toString (optstr:string option) =
        match optstr with
        | Some str -> str
        | _ -> null

    #if INTERACTIVE
    let ofTuple<'v> (b, v:'v) =
        if b then Some v
        else None

    let toTuple = function
        | Some(v) -> true, v
        | None -> false, null

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

    let cast<'a> a =
        if a = null then
            None
        else
            tryConvert<'a> a

    // https://stackoverflow.com/questions/24841185/how-to-deal-with-option-values-generically-in-f
    let isCompatible (x:obj) : isOption:bool * description:string =
        let tOption = typeof<option<obj>>.GetGenericTypeDefinition()
        match x with
        | null -> true, "null"
        | :? DBNull -> true, "dbnull"
        | _ ->
            let typ = x.GetType()
            match x with
            | _ when typ.IsGenericType && typ.GetGenericTypeDefinition() = tOption ->
                match typ.GenericTypeArguments with
                | [|t|] -> true, t.Name
                | _     -> true, "'t"

            | _ -> false, typ.Name

module private TestMe =
    (*
    #I @"..\..\bin\Debug\net48"
    #r "Engine.Common.FS.dll"
    open Engine.Common.FS
    open System
    *)

    let f = Option.isCompatible
    f 4                    = (false, "Int32")    |> verify
    f (Some 4)             = (true,  "Int32")    |> verify
    f (Some 0.3)           = (true,  "Double")   |> verify
    f None                 = (true,  "null")     |> verify
    f null                 = (true,  "null")     |> verify
    f (Some "a")           =  (true, "String")   |> verify
    f (Some DateTime.Now)  = (true,  "DateTime") |> verify
    f DateTime.Now         = (false, "DateTime") |> verify



//[<AutoOpen>]
module OptionModule =
    /// F# option to C# reference : reference type 이 아닌 경우, compile error 발생
    let o2r optVal = Option.toObj optVal

    // https://riptutorial.com/fsharp/example/16297/how-to-compose-values-and-functions-using-common-operators

    /// If 't' and 'u' has Some values then return Some (tv*uv) otherwise return None
    let (<*>) t u =
        match t, u with
        | Some tv, Some tu  -> Some (tv, tu)
        | _                 -> None


