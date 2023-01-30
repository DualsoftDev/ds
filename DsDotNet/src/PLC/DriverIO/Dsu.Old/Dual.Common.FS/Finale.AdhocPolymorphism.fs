namespace Old.Dual.Common


//open FSharpPlus

//[<AutoOpen>]
//module FinaleAdhocPolymorphism =

//    // http://nut-cracker.azurewebsites.net/blog/2011/11/15/typeclasses-for-fsharp/
//    // https://kwangyulseo.com/2015/01/21/emulating-haskell-type-classes-in-f/
    

//    type Fpartition = Fpartition with
//        static member ($) (Fpartition, x:list<_>)     = fun f -> List.partition  f x
//        static member ($) (Fpartition, x:seq<_>)      = fun f -> Seq.partition   f x
//        static member ($) (Fpartition, x:array<_>)    = fun f -> Array.partition f x
//    /// chunkSize:int -> array:'T [] -> 'T [] []
//    let inline partition f x = Fpartition $ x <| f


