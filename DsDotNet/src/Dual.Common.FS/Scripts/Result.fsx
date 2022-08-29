

let o1 = Ok "10"


Ok 10
    |> Result.bind (function
        | n when n % 2 = 0 -> Ok (n.ToString())
        | _ -> Error "Odd")


Error 10
    |> Result.bind (function
        | n when n % 2 = 0 -> Ok (n.ToString())
        | _ -> Error "Odd")


let unzip tpls =
    (tpls |> Array.map fst, tpls |> Array.map snd)


