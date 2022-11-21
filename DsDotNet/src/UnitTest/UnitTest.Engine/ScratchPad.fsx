open System.Collections.Generic


exception MyException of string
    with static member Create(msg:string) = MyException msg

let ex = MyException.Create("test")
raise ex



let bind   (mapping: 'x->seq<'y>) xs = (Seq.collect mapping) xs
let bind'  (mapping: 'x->seq<'y>) xs = Seq.collect mapping xs
let bind'' (mapping: 'x->seq<'y>)    = Seq.collect mapping
let apply  fs xs = bind (fun f -> Seq.map ((<|) f) xs) fs
let apply' fs xs = bind (fun f -> Seq.map f xs) fs
let x2 = (*) 2
let x3 = (*) 3
let x n = (*) n
let (<*>) = apply
apply [x 2; x 9] [1; 3]
apply' [x 2; x 9] [1; 3]

bind (fun f -> Seq.map f [ 1; 3 ]) [ x 2; x 9 ]

[x2; x3] <*> [1; 3]

((<|) x2) 9

type Range = {
    Start: int
    End: int
    Replace: string
} with
    static member Create(start: int, end_: int, replace: string) =
        { Start = start
          End = end_
          Replace = replace }

let ranges = [
    Range.Create(0, 0, "Start")
    Range.Create(2, 3, "hello")
    Range.Create(5, 6, "world")
    Range.Create(9, 9, "Bye")
]


let replace (xs:char array) (ranges:Range seq) =
    let hash = HashSet<Range>()
    let rec helper (i:int) =
        [
            if i < xs.Length then
                let range = ranges |> Seq.tryFind (fun r -> r.Start <= i && i <= r.End)
                match range with
                | Some r ->
                    if not (hash.Contains(r)) then
                        hash.Add(r) |> ignore
                        yield! r.Replace
                | None ->
                    yield xs[i]
                yield! helper (i+1)
        ]
    helper 0

[|'0'..'9'|]
replace [|'0'..'9'|] ranges

open System.Collections.Generic
let dic =
    [
        1, "One"
        2, "Two"
        5, "Five"
    ] |> dict |> Dictionary

let n = 5
match n with
| DicContains dic v -> v
| _ -> "Not Found"
