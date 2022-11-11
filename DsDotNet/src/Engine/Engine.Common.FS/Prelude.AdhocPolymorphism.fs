namespace Engine.Common.FS
//open FSharpPlus

[<AutoOpen>]
module PreludeAdhocPolymorphism =
    (*
    // http://nut-cracker.azurewebsites.net/blog/2011/11/15/typeclasses-for-fsharp/
    // https://kwangyulseo.com/2015/01/21/emulating-haskell-type-classes-in-f/
    // https://stackoverflow.com/questions/7695393/overloading-operator-in-f
    *)

    type FAdhoc_bind = FAdhoc_bind with
        static member ($) (FAdhoc_bind, x:option<_>)     = fun f -> Option.bind   f x
        static member ($) (FAdhoc_bind, x:list<_>)       = fun f -> List.collect  f x
        static member ($) (FAdhoc_bind, x:seq<_>)        = fun f -> Seq.collect   f x
        static member ($) (FAdhoc_bind, x:array<_>)      = fun f -> Array.collect f x
        static member ($) (FAdhoc_bind, x:Result<_, _>)  = fun f -> Result.bind   f x

    type FAdhoc_bindi = FAdhoc_bindi with
        static member ($) (FAdhoc_bindi, x:list<_>)    = fun f -> List.mapi  f x |> List.collect id
        static member ($) (FAdhoc_bindi, x:seq<_>)     = fun f -> Seq.mapi   f x |> Seq.collect id
        static member ($) (FAdhoc_bindi, x:array<_>)   = fun f -> Array.mapi f x |> Array.collect id

    type FAdhoc_choosei = FAdhoc_choosei with
        static member ($) (FAdhoc_choosei, x:list<_>)  = fun f -> List.mapi  f x |> List.choose id
        static member ($) (FAdhoc_choosei, x:seq<_>)   = fun f -> Seq.mapi   f x |> Seq.choose id
        static member ($) (FAdhoc_choosei, x:array<_>) = fun f -> Array.mapi f x |> Array.choose id

    type FAdhoc_chunkBySize = FAdhoc_chunkBySize with
        static member ($) (FAdhoc_chunkBySize, x:list<_>)     = fun f -> List.chunkBySize  f x
        static member ($) (FAdhoc_chunkBySize, x:seq<_>)      = fun f -> Seq.chunkBySize   f x
        static member ($) (FAdhoc_chunkBySize, x:array<_>)    = fun f -> Array.chunkBySize f x

    type FAdhoc_groupBy = FAdhoc_groupBy with
        static member ($) (FAdhoc_groupBy, x:list<_>)     = fun f -> List.groupBy  f x
        static member ($) (FAdhoc_groupBy, x:seq<_>)      = fun f -> Seq.groupBy   f x
        static member ($) (FAdhoc_groupBy, x:array<_>)    = fun f -> Array.groupBy f x

    type FAdhoc_indexed = FAdhoc_indexed with
        static member ($) (FAdhoc_indexed, x:list<_>)  = List.indexed  x
        static member ($) (FAdhoc_indexed, x:seq<_>)   = Seq.indexed   x
        static member ($) (FAdhoc_indexed, x:array<_>) = Array.indexed x

    type FAdhoc_iter = FAdhoc_iter with
        static member ($) (FAdhoc_iter, x:option<_>)   = fun f -> Option.iter f x
        static member ($) (FAdhoc_iter, x:list<_>)     = fun f -> List.iter   f x
        static member ($) (FAdhoc_iter, x:seq<_>)      = fun f -> Seq.iter    f x
        static member ($) (FAdhoc_iter, x:array<_>)    = fun f -> Array.iter  f x
        static member ($) (FAdhoc_iter, x:Result<_, _>)  = fun f -> Result.iter f x

    type FAdhoc_map = FAdhoc_map with
        static member ($) (FAdhoc_map, x:option<_>) = fun f -> Option.map     f x
        static member ($) (FAdhoc_map, x:list<_>)   = fun f -> List.map       f x
        static member ($) (FAdhoc_map, x:seq<_>)    = fun f -> Seq.map        f x
        static member ($) (FAdhoc_map, x:array<_>)  = fun f -> Array.map      f x
        static member ($) (FAdhoc_map, x:Result<_, _>)  = fun f -> Result.map f x

    type FAdhoc_mapi = FAdhoc_mapi with
        static member ($) (FAdhoc_mapi, x:list<_>)   = fun f -> List.mapi  f x
        static member ($) (FAdhoc_mapi, x:seq<_>)    = fun f -> Seq.mapi   f x
        static member ($) (FAdhoc_mapi, x:array<_>)  = fun f -> Array.mapi f x

    type FAdhoc_pairwise = FAdhoc_pairwise with
        static member ($) (FAdhoc_pairwise, x:list<_>)  = List.pairwise  x
        static member ($) (FAdhoc_pairwise, x:seq<_>)   = Seq.pairwise   x
        static member ($) (FAdhoc_pairwise, x:array<_>) = Array.pairwise x

    type FAdhoc_picki = FAdhoc_picki with
        static member ($) (FAdhoc_picki, x:list<_>)  = fun f -> List.mapi  f x |> List.pick id
        static member ($) (FAdhoc_picki, x:seq<_>)   = fun f -> Seq.mapi   f x |> Seq.pick id
        static member ($) (FAdhoc_picki, x:array<_>) = fun f -> Array.mapi f x |> Array.pick id


    type FAdhoc_OrElse = FAdhoc_OrElse with
        static member (?<-) (FAdhoc_OrElse, x:option<'a>, y:option<'a>)  = Option.orElse x y
        static member (?<-) (FAdhoc_OrElse, x:list<'a>  , y:list<'a>)    = if List.isEmpty  x then y else x
        static member (?<-) (FAdhoc_OrElse, x:seq<'a>   , y:seq<'a>)     = if Seq.isEmpty   x then y else x
        static member (?<-) (FAdhoc_OrElse, x:array<'a> , y:array<'a>)   = if Array.isEmpty x then x else y

    let inline bind        f x = FAdhoc_bind        $ x <| f
    let inline bindi       f x = FAdhoc_bindi       $ x <| f
    let inline choosei     f x = FAdhoc_choosei     $ x <| f
    let inline chunkBySize f x = FAdhoc_chunkBySize $ x <| f
    let inline groupBy     f x = FAdhoc_groupBy     $ x <| f

    let inline indexed       x = FAdhoc_indexed     $ x
    let inline iter        f x = FAdhoc_iter        $ x <| f
    let inline map         f x = FAdhoc_map         $ x <| f
    let inline mapi        f x = FAdhoc_mapi        $ x <| f
    let inline pairwise      x = FAdhoc_pairwise    $ x
    let inline picki       f x = FAdhoc_picki       $ x <| f
    let inline ( <|> )     x y = (?<-) FAdhoc_OrElse x y

    let private testme() =
        let a = Some 1 <|> None
        let b = [1..10] <|> []
        let c =[] <|> [1..10] <|> []
        ()





    /// tuple array 를 개별 seq 의 tuple 로 반환 : [a*b] -> [a] * [b]
    //let inline unzip tpls =
    //    (tpls |> map fst, tpls |> map snd)


