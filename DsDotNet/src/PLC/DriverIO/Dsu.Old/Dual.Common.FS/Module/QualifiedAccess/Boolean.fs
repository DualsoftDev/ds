namespace Old.Dual.Common

open System
open System.Runtime.CompilerServices
open System.Diagnostics

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Boolean =
    type internal U2U = unit -> unit
    let mapValue (trueValue: 't)  (falseValue: 't) b =
        if b then trueValue else falseValue

    /// 주어진 bool 이 true 이면 mapper 를 수행한 Some 값 반환, false 이면 None
    let mapTrue (mapper:unit -> 'a) = mapValue (Some (mapper())) None
    //let map = mapTrue
    /// 주어진 bool 이 false 이면 mapper 를 수행한 Some 값 반환, true 이면 None
    let mapFalse (mapper:unit -> 'a) = mapValue None (Some (mapper()))

    let mapBool (trueMapper: unit -> 't)  (falseMapper: unit -> 't) b =
        if b then trueMapper() else falseMapper()


    let iterTrue (action: U2U) b =
        if b then action()
    let iterFalse (action: U2U) b =
        if not b then action()
    //let iter = iterTrue

    let iterBool (trueAction: U2U)  (falseAction: U2U) b =
        if b then trueAction() else falseAction()



    /// 주어진 bool 이 true 이면 mapper 를 수행한 option 값 반환, false 이면 None
    let bindTrue (mapper:unit -> 'a option) = mapValue (mapper()) None
    //let bind = bindTrue
    /// 주어진 bool 이 false 이면 mapper 를 수행한 option 값 반환, true 이면 None
    let bindFalse (mapper:unit -> 'a option) = mapValue None (mapper())

    let bindBool (trueMapper: unit -> 'a option)  (falseMapper: unit -> 'a option) b =
        if b then trueMapper() else falseMapper()


    [<Extension>] // type BooleanExt =
    type BooleanExt =
        /// Graph 의 vertex v 와 연결된 outgoing edges 를 반환
        [<Extension>] static member MapTrue(b:bool, f)   = b |> mapTrue f
        [<Extension>] static member MapFalse(b:bool, f)  = b |> mapFalse f
        [<Extension>] static member BindTrue(b:bool, f)  = b |> bindTrue f
        [<Extension>] static member BindFalse(b:bool, f) = b |> bindFalse f
        [<Extension>] static member Map(b:bool, f)       = b |> mapTrue f
        [<Extension>] static member Bind(b:bool, f)      = b |> bindTrue f

module private TestMe =
    let t() =
        printfn "True"
        true
    let f() =
        printfn "False"
        false

    let someTrue = fun () -> Some true
    let someFalse = fun () -> Some false
    let none = fun () -> None

    let True = true |> Boolean.mapBool (fun () -> "TRUE") (fun () -> "FALSE")
    let False = false |> Boolean.mapBool (fun () -> "TRUE") (fun () -> "FALSE")

    true |> Boolean.iterBool (fun () -> printfn "OK") (fun () -> failwith "ERROR")
    false |> Boolean.iterBool (fun () -> failwith "ERROR") (fun ()-> printfn "OK") 

    let someOK = true |> Boolean.bindTrue (fun ()-> Some "OK")
    let none1   = false |> Boolean.bindTrue (fun ()-> Some "OK")

    let t0 = true.MapTrue(t) // Some t() = Some true
    assert(t0 = Some true)
    let t1 = true.Map(t) // Some t() = Some true
    assert(t1 = Some true)
    let t2 = true.Map(f) // Some false
    assert(t2 = Some false)
    let f1 = false.Map(t) // None
    assert(f1 = None)
    let f2 = false.Map(f) // None
    assert(f1 = None)

    let testBindTrue() =
        let t3 = true.BindTrue(someTrue)    // Some true
        assert(t3 = Some true)
        let t4 = true.BindTrue(someFalse)   // Some false
        assert(t4 = Some false)
        let f3 = false.BindTrue(someTrue)   // None
        assert(f3 = None)
        let f4 = false.BindTrue(someFalse)  // None
        assert(f4 = None)
        ()

    let testBindFalse() =
        let t3 = true.BindFalse(someTrue)    // None
        assert(t3 = None)
        let t4 = true.BindFalse(someFalse)   // None
        assert(t4 = None)
        let f3 = false.BindFalse(someTrue)   // Some true
        assert(f3 = Some true)
        let f4 = false.BindFalse(someFalse)  // Some false
        assert(f4 = Some false)
        ()
