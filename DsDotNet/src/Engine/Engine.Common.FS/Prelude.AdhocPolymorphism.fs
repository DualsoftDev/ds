namespace Engine.Common.FS
//open FSharpPlus

[<AutoOpen>]
module PreludeAdhocPolymorphism =
    (*
    // http://nut-cracker.azurewebsites.net/blog/2011/11/15/typeclasses-for-fsharp/
    // https://kwangyulseo.com/2015/01/21/emulating-haskell-type-classes-in-f/
    // https://stackoverflow.com/questions/7695393/overloading-operator-in-f
    *)

    type Fbind = Fbind with
        static member ($) (Fbind, x:option<_>)     = fun f -> Option.bind   f x
        static member ($) (Fbind, x:list<_>)       = fun f -> List.collect  f x
        static member ($) (Fbind, x:seq<_>)        = fun f -> Seq.collect   f x
        static member ($) (Fbind, x:array<_>)      = fun f -> Array.collect f x
        static member ($) (Fbind, x:Result<_, _>)  = fun f -> Result.bind   f x

    type Fbindi = Fbindi with
        static member ($) (Fbindi, x:list<_>)    = fun f -> List.mapi  f x |> List.collect id
        static member ($) (Fbindi, x:seq<_>)     = fun f -> Seq.mapi   f x |> Seq.collect id
        static member ($) (Fbindi, x:array<_>)   = fun f -> Array.mapi f x |> Array.collect id

    type Fchoosei = Fchoosei with
        static member ($) (Fchoosei, x:list<_>)  = fun f -> List.mapi  f x |> List.choose id
        static member ($) (Fchoosei, x:seq<_>)   = fun f -> Seq.mapi   f x |> Seq.choose id
        static member ($) (Fchoosei, x:array<_>) = fun f -> Array.mapi f x |> Array.choose id

    type FchunkBySize = FchunkBySize with
        static member ($) (FchunkBySize, x:list<_>)     = fun f -> List.chunkBySize  f x
        static member ($) (FchunkBySize, x:seq<_>)      = fun f -> Seq.chunkBySize   f x
        static member ($) (FchunkBySize, x:array<_>)    = fun f -> Array.chunkBySize f x

    type FgroupBy = FgroupBy with
        static member ($) (FgroupBy, x:list<_>)     = fun f -> List.groupBy  f x
        static member ($) (FgroupBy, x:seq<_>)      = fun f -> Seq.groupBy   f x
        static member ($) (FgroupBy, x:array<_>)    = fun f -> Array.groupBy f x

    type Findexed = Findexed with
        static member ($) (Findexed, x:list<_>)  = List.indexed  x
        static member ($) (Findexed, x:seq<_>)   = Seq.indexed   x
        static member ($) (Findexed, x:array<_>) = Array.indexed x

    type Iter = Iter with
        static member ($) (Iter, x:option<_>)   = fun f -> Option.iter f x
        static member ($) (Iter, x:list<_>)     = fun f -> List.iter   f x
        static member ($) (Iter, x:seq<_>)      = fun f -> Seq.iter    f x
        static member ($) (Iter, x:array<_>)    = fun f -> Array.iter  f x
        static member ($) (Iter, x:Result<_, _>)  = fun f -> Result.iter f x

    type Fmap = Fmap with
        static member ($) (Fmap, x:option<_>) = fun f -> Option.map     f x
        static member ($) (Fmap, x:list<_>)   = fun f -> List.map       f x
        static member ($) (Fmap, x:seq<_>)    = fun f -> Seq.map        f x
        static member ($) (Fmap, x:array<_>)  = fun f -> Array.map      f x
        static member ($) (Fmap, x:Result<_, _>)  = fun f -> Result.map f x

    type Fmapi = Fmapi with
        static member ($) (Fmapi, x:list<_>)   = fun f -> List.mapi  f x
        static member ($) (Fmapi, x:seq<_>)    = fun f -> Seq.mapi   f x
        static member ($) (Fmapi, x:array<_>)  = fun f -> Array.mapi f x

    type Fpairwise = Fpairwise with
        static member ($) (Fpairwise, x:list<_>)  = List.pairwise  x
        static member ($) (Fpairwise, x:seq<_>)   = Seq.pairwise   x
        static member ($) (Fpairwise, x:array<_>) = Array.pairwise x

    type Fpicki = Fpicki with
        static member ($) (Fpicki, x:list<_>)  = fun f -> List.mapi  f x |> List.pick id
        static member ($) (Fpicki, x:seq<_>)   = fun f -> Seq.mapi   f x |> Seq.pick id
        static member ($) (Fpicki, x:array<_>) = fun f -> Array.mapi f x |> Array.pick id


    type FOrElse = FOrElse with
        static member (?<-) (FOrElse, x:option<'a>, y:option<'a>)  = Option.orElse x y
        static member (?<-) (FOrElse, x:list<'a>  , y:list<'a>)    = if List.isEmpty x then y else x
        static member (?<-) (FOrElse, x:seq<'a>   , y:seq<'a>)     = if Seq.isEmpty x then y else x
        static member (?<-) (FOrElse, x:array<'a> , y:array<'a>)   = if Array.isEmpty x then x else y

    let inline bind f x = Fbind $ x <| f
    let inline bindi f x = Fbindi $ x <| f
    let inline choosei f x = Fchoosei $ x <| f
    let inline chunkBySize f x = FchunkBySize $ x <| f
    let inline groupBy f x = FgroupBy $ x <| f

    let inline indexed x = Findexed $ x
    let inline iter f x = Iter $ x <| f
    let inline map f x = Fmap $ x <| f
    let inline mapi f x = Fmapi $ x <| f
    let inline pairwise x = Fpairwise $ x
    let inline picki f x = Fpicki $ x <| f
    let inline ( <|> ) x y = (?<-) FOrElse x y

    let private testme() =
        let a = Some 1 <|> None
        let b = [1..10] <|> []
        let c =[] <|> [1..10] <|> []
        ()





    /// tuple array 를 개별 seq 의 tuple 로 반환 : [a*b] -> [a] * [b]
    //let inline unzip tpls =
    //    (tpls |> map fst, tpls |> map snd)


