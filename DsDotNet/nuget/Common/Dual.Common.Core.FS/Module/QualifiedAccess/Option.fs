namespace Dual.Common.Core.FS

open System
open System.Runtime.CompilerServices

[<RequireQualifiedAccess>]
module Option =
    // reminders...
    let private ofObj2        = Option.ofObj
    let private ofNullable2   = Option.ofNullable
    let private toObj2        = Option.toObj
    let private toNullable2   = Option.toNullable
    let private defaultValue2 = Option.defaultValue
    let private defaultWith2  = Option.defaultWith

    /// Option type f 를 Option type 인자 x 에 적용
    ///
    /// See (<*>) operator
    let apply f x =
        match f, x with
        | Some f, Some x -> Some (f x)
        | _ -> None

    (*
     * 현재까지 Option.cast 는 제대로 구현 불가.
     * Em.Option.fs 의 확장 method 는 동작 확인

     * hard coding 으로  다음과 같이 외부에서 사용할 때는 문제가 없다.

        match optValue with
        | Some (:? 'T as v) -> Some v
        | _ -> None

        확장 method : optValue.Cast<'T>()
     *)

    //let convert<'a> a =
    //    if a = null then
    //        None
    //    else
    //        tryConvert<'a> a

    //let cast<'a> (opt:#obj option): 'a option =
    //    match opt with
    //    | Some a when (box a :? 'a) -> Some (box a :?> 'a)
    //    | _ -> None




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

    /// None 값이거나, Some 인 경우 coverValue 이어야 true
    let isNoneOr(xo, coverValue) = match xo with | None -> true | Some x -> x = coverValue
    /// None 값이거나, Some 인 경우 f 를 만족해야 true
    let isNoneOrWith f xo = match xo with | None -> true | Some x -> f x


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

    /// boolValue 가 true 이면 Some targetValueWhenTrue else None
    let ofBool (b:bool) : bool option =
        if b then Some true else None

    /// boolValue 가 true 이면 Some targetValueWhenTrue else None
    let ofBool2 targetValueWhenTrue (boolValue:bool) =
        if boolValue then Some targetValueWhenTrue else None

    /// xs 가 empty 이면 None else (Some xs)
    let ofArray (xs:'a [])   = if isItNull(xs) || xs |> Array.isEmpty then None else Some xs
    /// xs 가 empty 이면 None else (Some xs)
    let ofList  (xs:'a list) = if isItNull(xs) || xs |> List.isEmpty  then None else Some xs
    /// xs 가 empty 이면 None else (Some xs)
    let ofSeq   (xs:'a seq)  = if isItNull(xs) || xs |> Seq.isEmpty   then None else Some xs

    let ofString (str:string) =
        match str with
        | IsItNullOrEmpty str -> None
        | _ -> Some str


    /// C# 의 nullable type 에 대한 F# option type 을 nullable type 의 reference 로 반환
    ///
    /// struct 나 record type 은 compile error 발생 -> toNullable 사용
    let toReference (optionValue: Option<'T>) : 'T =
        match optionValue with
        | None -> null
        | Some value -> value

    let toString (optstr:string option) =
        match optstr with
        | Some str -> str
        | _ -> null


    /// Some f() 값.  수행 중 exception 발생하면 None 값
    let tryMap (f:unit -> 'a) : 'a option = try Some <| f() with _ -> None
    /// f() 값.  수행 중 exception 발생하면 None 값
    let tryBind (f:unit -> 'a option) : 'a option = try f() with _ -> None


    // https://riptutorial.com/fsharp/example/16297/how-to-compose-values-and-functions-using-common-operators
    /// If 't' and 'u' has Some values then return Some (tv*uv) otherwise return None
    let zip x1 x2 =
        match x1, x2 with
        | Some x1, Some x2 -> Some (x1, x2)
        | _ -> None






type OptionExt2 =
    /// Match function for Option type.  C# 에서 쉽게 사용.
    [<Extension>]
    static member Match(x:Option<'T>, someFunc, noneFun) =
        match x with
        | Some v -> someFunc v
        | None -> noneFun ()


[<AutoOpen>]
module OptionModule =
    /// Some ():  Some unit value
    let something = Some ()
    /// F# option to C# reference : reference type 이 아닌 경우, compile error 발생
    let o2r optVal = Option.toObj optVal

    /// Option.apply: Option type f 를 Option type 인자 x 에 적용
    let (<*>) = Option.apply

    /// Option.apply 적용 하되, 결과값 option 은 무시
    let (|*>) fOpt xOpt = (<*>) fOpt xOpt |> ignore

module private TestMe =
    (*
    #I @"..\..\bin\Debug\net48"
    #r "Dual.Common.Core.FS.dll"
    open Dual.Common.Core.FS
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



    #if INTERACTIVE
    Some(1) |> Option.lift2 (+) (Some(3))
    Some(1) |> Option.lift2 (+) None
    None |> Option.lift2 (+) (Some(3))

    let a =
      Some(3) |> Option.bind (fun num ->
        Some(2) |> Option.map ((+) num) )
    #endif

