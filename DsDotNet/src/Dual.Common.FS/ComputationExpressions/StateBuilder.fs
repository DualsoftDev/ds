// http://matthewmanela.com/blog/functional-stateful-program-in-f/
// http://www.fssnip.net/cL
// https://github.com/fsprojects/FSharpx.Extras/blob/master/src/FSharpx.Extras/ComputationExpressions/Monad.fs
module Dual.Common.StateBuilder

/// <summary>
/// 'a : result type
/// 'st : state type
/// </summary>
type State<'st,'a> =
    | Ok of  'a * 'st
    | Error of ErrorState
and ErrorState = string
and StateBuilder() =
    member __.Return(x) = fun s -> Ok (x, s)
    member __.ReturnFrom(x) = x
    member __.Error msg = fun _ -> Error msg
    member __.Bind(p, rest) =
        fun state ->
                 let result = p state in
                 match result with
                 | Ok (value,state2) -> (rest value) state2
                 | Error msg -> Error msg  
 
    member __.Get () = fun state -> Ok (state, state)
    member __.Put s = fun state -> Ok ((), s)
    member __.Delay f = f ()
    member __.Combine (f, g) = 
        let (>>=) f g = Ok( (fun s -> let s', a = !f s in !(g a) s'), ())
        f >>= (fun () -> g)
    member __.Zero () = Ok((fun s -> s,()), ())     // 맞나?

let state = StateBuilder()






