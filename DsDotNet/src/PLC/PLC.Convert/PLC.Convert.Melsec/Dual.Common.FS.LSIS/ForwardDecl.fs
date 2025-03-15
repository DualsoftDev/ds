module Dual.Common.FS.LSIS.ForwardDecl

let declare<'a>  = ref Unchecked.defaultof<'a>

module ShowForwardDeclSample =
    let add = declare<int -> int>
    let private showMeSamples() =

        let incr nums = nums |> Seq.map !add
        let doSomething() =
            incr [1; 2; 3; 4]

        add := fun x -> x + 1

        let x = doSomething()
        x

    type private Initializer() =
        static member Initializer() =
            add := fun x -> x + 1
            let x = showMeSamples()
            ()
