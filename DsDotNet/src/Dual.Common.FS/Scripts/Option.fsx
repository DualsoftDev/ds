
type A() =
    class end

type B() =
    inherit A()

type C() =
    class end

module XX =
    let cast<'a> input = System.Convert.ChangeType(input, typeof<'a>) :?> 'a
    let tryCast<'a> input =
        try Some(cast<'a> input)
        with _ -> None

    let castable<'a> input =
        input :? 'a


let b = B() :> A
let bb = XX.tryCast<B> b
let c = C()
let cc = XX.tryCast<B> c



let tryCast<'t> x =
    match x with
    | :? 't as t -> Some t
    | _ -> None
