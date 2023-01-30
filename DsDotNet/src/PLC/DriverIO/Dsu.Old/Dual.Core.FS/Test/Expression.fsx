#load "0.FullLoading.fsx"

open Dual.Common
open Dual.Core
open Dual.Core.Types
open Dual.Common.ComputationExpressions

let p = ParserAlgo.parseExpression
let testExpression() =
    let result = p "(((A|B|C|D) & (E|F)) |G) & H"


    let rec toladder exp sx sy =
        match exp with
            | Terminal(t) ->
                printfn "%s: %d, %d" (t.ToText()) sx sy 
                1, 1
            | Binary(l, op, r) ->
                let lw, lh = toladder l sx sy
                match op with
                | And ->
                    let rw, rh = toladder r (sx+lw) sy
                    (lw+rw), (max lh rh)
                | Or ->
                    let rw, rh = toladder r sx (sy+lh)
                    (max lw rw), (lh+rh)
                | _ ->
                    failwith "ERROR"
            | _ ->
                failwith "ERROR"


    match result with
    | Ok(exp) -> toladder exp 0 0
    | Error(err) -> failwithf "ERROR: %s" err


let testOptimze() =
    let a = p "A" |> Result.get
    let b = p "A" |> Result.get
    let aa = a
    let c = mkAnd a b

    let d =
        result {
            let! l = p "A & B"
            let! r = p "A & B"
            return l <&&> r
        } |> Result.get
    ()
