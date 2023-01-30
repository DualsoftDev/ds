namespace Old.Dual.Common
open FSharpPlus

[<AutoOpen>]
module PreludeAdhocPolymorphism =

    // http://nut-cracker.azurewebsites.net/blog/2011/11/15/typeclasses-for-fsharp/
    // https://kwangyulseo.com/2015/01/21/emulating-haskell-type-classes-in-f/
    
    //type Fbind = Fbind with
    //    static member ($) (Fbind, x:option<_>)     = fun f -> Option.bind   f x
    //    static member ($) (Fbind, x:list<_>)       = fun f -> List.collect  f x
    //    static member ($) (Fbind, x:seq<_>)        = fun f -> Seq.collect   f x
    //    static member ($) (Fbind, x:array<_>)      = fun f -> Array.collect f x
    //    static member ($) (Fbind, x:Result<_, _>)  = fun f -> Result.bind   f x
    //let inline bind f x = Fbind $ x <| f

    
    //type Fbindi = Fbindi with
    //    static member ($) (Fbindi, x:list<_>)    = fun f -> List.mapi  f x |> List.collect id
    //    static member ($) (Fbindi, x:seq<_>)     = fun f -> Seq.mapi   f x |> Seq.collect id
    //    static member ($) (Fbindi, x:array<_>)   = fun f -> Array.mapi f x |> Array.collect id
    //let inline bindi f x = Fbindi $ x <| f

    //type Fchoosei = Fchoosei with
    //    static member ($) (Fchoosei, x:list<_>)  = fun f -> List.mapi  f x |> List.choose id
    //    static member ($) (Fchoosei, x:seq<_>)   = fun f -> Seq.mapi   f x |> Seq.choose id
    //    static member ($) (Fchoosei, x:array<_>) = fun f -> Array.mapi f x |> Array.choose id
    //let inline choosei f x = Fchoosei $ x <| f

    type FchunkBySize = FchunkBySize with
        static member ($) (FchunkBySize, x:list<_>)     = fun f -> List.chunkBySize  f x
        static member ($) (FchunkBySize, x:seq<_>)      = fun f -> Seq.chunkBySize   f x
        static member ($) (FchunkBySize, x:array<_>)    = fun f -> Array.chunkBySize f x
    /// chunkSize:int -> array:'T [] -> 'T [] []
    let inline chunkBySize f x = FchunkBySize $ x <| f

    //type FgroupBy = FgroupBy with
    //    static member ($) (FgroupBy, x:list<_>)     = fun f -> List.groupBy  f x
    //    static member ($) (FgroupBy, x:seq<_>)      = fun f -> Seq.groupBy   f x
    //    static member ($) (FgroupBy, x:array<_>)    = fun f -> Array.groupBy f x
    ///// projection:('T -> 'Key) -> collection:'T [] -> ('Key * 'T []) [] when 'Key : equality
    //let inline groupBy f x = FgroupBy $ x <| f


    type Findexed = Findexed with
        static member ($) (Findexed, x:list<_>)  = List.indexed  x
        static member ($) (Findexed, x:seq<_>)   = Seq.indexed   x
        static member ($) (Findexed, x:array<_>) = Array.indexed x
    let inline indexed x = Findexed $ x

    //type Fiter = Fiter with
    //    static member ($) (Fiter, x:option<_>)   = fun f -> Option.iter f x
    //    static member ($) (Fiter, x:list<_>)     = fun f -> List.iter   f x
    //    static member ($) (Fiter, x:seq<_>)      = fun f -> Seq.iter    f x
    //    static member ($) (Fiter, x:array<_>)    = fun f -> Array.iter  f x
    //    //static member ($) (Fiter, x:Result<_, _>)  = fun f -> Result.iter f x
    //let inline iter f x = Fiter $ x <| f

    //type Fmap = Fmap with
    //    static member ($) (Fmap, x:option<_>) = fun f -> Option.map     f x
    //    static member ($) (Fmap, x:list<_>)   = fun f -> List.map       f x
    //    static member ($) (Fmap, x:seq<_>)    = fun f -> Seq.map        f x
    //    static member ($) (Fmap, x:array<_>)  = fun f -> Array.map      f x
    //    static member ($) (Fmap, x:Result<_, _>)  = fun f -> Result.map f x
    //let inline map f x = Fmap $ x <| f

    //type Fmapi = Fmapi with
    //    static member ($) (Fmapi, x:list<_>)   = fun f -> List.mapi  f x
    //    static member ($) (Fmapi, x:seq<_>)    = fun f -> Seq.mapi   f x
    //    static member ($) (Fmapi, x:array<_>)  = fun f -> Array.mapi f x
    //let inline mapi f x = Fmapi $ x <| f       
                                               
    type Fpairwise = Fpairwise with
        static member ($) (Fpairwise, x:list<_>)  = List.pairwise  x
        static member ($) (Fpairwise, x:seq<_>)   = Seq.pairwise   x
        static member ($) (Fpairwise, x:array<_>) = Array.pairwise x
    let inline pairwise x = Fpairwise $ x
                                               
    //type Fpicki = Fpicki with                  
    //    static member ($) (Fpicki, x:list<_>)  = fun f -> List.mapi  f x |> List.pick id
    //    static member ($) (Fpicki, x:seq<_>)   = fun f -> Seq.mapi   f x |> Seq.pick id
    //    static member ($) (Fpicki, x:array<_>) = fun f -> Array.mapi f x |> Array.pick id
    //let inline picki f x = Fpicki $ x <| f

    /// tuple array 를 개별 seq 의 tuple 로 반환 : [a*b] -> [a] * [b]
    let inline unzip tpls =
        (tpls |> map fst, tpls |> map snd)
