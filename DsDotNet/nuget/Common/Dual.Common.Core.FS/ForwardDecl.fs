module Dual.Common.Core.FS.ForwardDecl

let declare<'a>  = ref Unchecked.defaultof<'a>

module private ShowForwardDeclSample =
    let add = declare<int -> int>
    let add3 = declare<int -> int -> int>
    let private showMeSamples() =

        let incr nums = nums |> Seq.map !add
        let doSomething() =
            incr [1; 2; 3; 4]

        add := fun x -> x + 1
        add3 := fun x y -> x + y + 1

        let x = doSomething()
        x

    type private Initializer() =
        static member Initializer() =
            add := fun x -> x + 1
            add3 := fun x y -> x + y + 1
            let x = showMeSamples()
            ()
