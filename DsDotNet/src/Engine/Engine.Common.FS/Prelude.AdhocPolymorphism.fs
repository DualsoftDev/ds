namespace Engine.Common.FS

open System

#nowarn "1173"  // warning FS1173: 중위 연산자 멤버 '?<-'에 3개의 초기 인수가 있습니다. 2개 인수의 튜플이 필요합니다(예: 정적 멤버 (+) (x,y) = ...).

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
        static member ($) (FAdhoc_bind, x:Async<_>)      = fun f -> Async.bind    f x

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
        static member ($) (FAdhoc_iter, x:option<_>)    = fun f -> Option.iter f x
        static member ($) (FAdhoc_iter, x:list<_>)      = fun f -> List.iter   f x
        static member ($) (FAdhoc_iter, x:seq<_>)       = fun f -> Seq.iter    f x
        static member ($) (FAdhoc_iter, x:array<_>)     = fun f -> Array.iter  f x
        static member ($) (FAdhoc_iter, x:Result<_, _>) = fun f -> Result.iter f x
        static member ($) (FAdhoc_iter, x:Async<_>)     = fun f -> Async.iter  f x

    type FAdhoc_map = FAdhoc_map with
        static member ($) (FAdhoc_map, x:option<_>) = fun f -> Option.map     f x
        static member ($) (FAdhoc_map, x:list<_>)   = fun f -> List.map       f x
        static member ($) (FAdhoc_map, x:seq<_>)    = fun f -> Seq.map        f x
        static member ($) (FAdhoc_map, x:array<_>)  = fun f -> Array.map      f x
        static member ($) (FAdhoc_map, x:Result<_, _>)  = fun f -> Result.map f x
        static member ($) (FAdhoc_map, x:Async<_>)  = fun f -> Async.map f x

    type FAdhoc_mapi = FAdhoc_mapi with
        static member ($) (FAdhoc_mapi, x:list<_>)   = fun f -> List.mapi  f x
        static member ($) (FAdhoc_mapi, x:seq<_>)    = fun f -> Seq.mapi   f x
        static member ($) (FAdhoc_mapi, x:array<_>)  = fun f -> Array.mapi f x

    type FAdhoc_picki = FAdhoc_picki with
        static member ($) (FAdhoc_picki, x:list<_>)  = fun f -> List.mapi  f x |> List.pick id
        static member ($) (FAdhoc_picki, x:seq<_>)   = fun f -> Seq.mapi   f x |> Seq.pick id
        static member ($) (FAdhoc_picki, x:array<_>) = fun f -> Array.mapi f x |> Array.pick id

    type FAdhoc_filter = FAdhoc_filter with
        static member ($) (FAdhoc_filter, x:list<_>)   = fun f -> List.filter  f x
        static member ($) (FAdhoc_filter, x:seq<_>)    = fun f -> Seq.filter   f x
        static member ($) (FAdhoc_filter, x:array<_>)  = fun f -> Array.filter f x


    type FAdhoc_pairwise = FAdhoc_pairwise with
        static member ($) (FAdhoc_pairwise, x:list<_>)  = List.pairwise  x
        static member ($) (FAdhoc_pairwise, x:seq<_>)   = Seq.pairwise   x
        static member ($) (FAdhoc_pairwise, x:array<_>) = Array.pairwise x

    type FAdhoc_sort = FAdhoc_sort with
        static member ($) (FAdhoc_sort, x:list<_>)  = List.sort  x
        static member ($) (FAdhoc_sort, x:seq<_>)   = Seq.sort   x
        static member ($) (FAdhoc_sort, x:array<_>) = Array.sort x

    type FAdhoc_distinct = FAdhoc_distinct with
        static member ($) (FAdhoc_distinct, x:list<_>)  = List.distinct  x
        static member ($) (FAdhoc_distinct, x:seq<_>)   = Seq.distinct   x
        static member ($) (FAdhoc_distinct, x:array<_>) = Array.distinct x

    type FAdhoc_append = FAdhoc_append with
        static member (?<-) (FAdhoc_append, x:list<'a>,  y:list<'a>)  = List.append x y
        static member (?<-) (FAdhoc_append, x:array<'a>, y:array<'a>) = Array.append x y
        static member (?<-) (FAdhoc_append, x:seq<'a>,   y:seq<'a>)   = Seq.append x y

    /// x + [xs] or [xs] + x
    type FAdhoc_Xpend = FAdhoc_Xpend with
        static member (?<-) (FAdhoc_Xpend, x:list<'a>,  y:'a) = List.append x [y]
        static member (?<-) (FAdhoc_Xpend, x:array<'a>, y:'a) = Array.append x [|y|]
        static member (?<-) (FAdhoc_Xpend, x:seq<'a>,   y:'a) = Seq.append x (seq {y})
        static member (?<-) (FAdhoc_Xpend, x:'a, y:list<'a> ) = x::y
        static member (?<-) (FAdhoc_Xpend, x:'a, y:array<'a>) = Array.append [|x|] y
        static member (?<-) (FAdhoc_Xpend, x:'a, y:seq<'a>  ) = Seq.append (seq{x}) y


    type FAdhoc_orElse = FAdhoc_orElse with
        /// 주의: FAdhoc_orElse (Option.orElse) 사용 시 short circuit 기능이 없다.
        static member (?<-) (FAdhoc_orElse, x:option<'a>, y:option<'a>)  = x |> Option.orElse y
        static member (?<-) (FAdhoc_orElse, x:Nullable<'a>, y:Nullable<'a>)  = if x.HasValue then x else y
        static member (?<-) (FAdhoc_orElse, x:'a when ^a : not struct, y:'a when ^a : not struct) = if isNull x then y else x

    let inline bind        f x = FAdhoc_bind        $ x <| f
    let inline (>>=)       x f = FAdhoc_bind        $ x <| f
    let inline bindi       f x = FAdhoc_bindi       $ x <| f
    let inline choosei     f x = FAdhoc_choosei     $ x <| f
    let inline chunkBySize f x = FAdhoc_chunkBySize $ x <| f
    let inline groupBy     f x = FAdhoc_groupBy     $ x <| f

    let inline indexed     x   = FAdhoc_indexed     $ x
    let inline pairwise    x   = FAdhoc_pairwise    $ x
    let inline sort        x   = FAdhoc_sort        $ x
    let inline distinct    x   = FAdhoc_distinct    $ x
    let inline iter        f x = FAdhoc_iter        $ x <| f
    let inline filter      f x = FAdhoc_filter      $ x <| f
    let inline map         f x = FAdhoc_map         $ x <| f
    let inline mapi        f x = FAdhoc_mapi        $ x <| f
    let inline picki       f x = FAdhoc_picki       $ x <| f


    //type FAdhoc_scan = FAdhoc_scan with
    //    static member ($) (FAdhoc_scan, x:list<'T>)   = fun (f:'s -> 'T -> 's) (state:'s) -> List.scan  f state x
    //    static member ($) (FAdhoc_scan, x:seq<'T>)    = fun (f:'s -> 'T -> 's) (state:'s) -> Seq.scan   f state x
    //    static member ($) (FAdhoc_scan, x:array<'T>)  = fun (f:'s -> 'T -> 's) (state:'s) -> Array.scan f state x

    //let inline scan        (f:'s->'t->'s) (state:'s) x = FAdhoc_scan  $ x <|| (f, state)

    (* Operators *)

    /// map
    let inline ( ==> )     x f = FAdhoc_map         $ x <| f
    /// choice on option type
    let inline ( <|> )     x y = (?<-) FAdhoc_orElse x y
    /// append two lists / arrays / sequences
    let inline ( @ )       x y = (?<-) FAdhoc_append x y // Haskell 의 ++ 연산자와 동일
    let inline ( ++ )      x y = (?<-) FAdhoc_append x y // Haskell 의 ++ 연산자와 동일
    /// append element to a list / array / sequence
    let inline ( +++ )     x y = (?<-) FAdhoc_Xpend  x y

    /// get default arguments (= defaultArg).
    let (|?) = defaultArg

    // https://riptutorial.com/fsharp/example/16297/how-to-compose-values-and-functions-using-common-operators

    /// kliesli for option
    (* tf: ('a -> 'b option) -> uf: ('b -> 'c option) -> v: 'a -> 'c option *)
    let inline (>=>) tf uf = fun v -> tf v >>= uf      // == fun v -> (tf v) >>= uf

    (* uf: ('a -> 'b option) -> tf: ('c -> 'a option) -> v: 'c -> 'b option
        v: 'b option <- 'c <- uf: ('b option <- 'a ) <- tf: ('a option <- 'c)
        *)
    let inline (<=<) uf tf = fun v -> tf v >>= uf

    let private testme() =
        (*
        #I @"bin\Debug\net48"
        #r "Engine.Common.FS.dll"
        open Engine.Common.FS
        *)
        let verify c = if not c then failwith "ERROR"
        let some1, some2, some3 = Some 1, Some 2, Some 3
        verify ( [1..3] @ [4..5] = [1..5])
        verify ( [|1..3|] @ [|4..5|] = [|1..5|])

        // sequence 는 '=' 로 직접 비교하면 원하는 결과가 나오지 않는다.
        let seq1 = seq{1..3} @ seq{4..5}
        verify ( System.Linq.Enumerable.SequenceEqual (seq1, seq{1..5}))

        verify ( some1 <|> None = some1 )

        let lift (f: 'a -> 'b) (x: 'a) = f x

        let incrOption x = Some (x + 1)
        verify ( some2 >>= incrOption = some3)
        verify ( bind incrOption some2 = some3)

        let dincr = incrOption >=> incrOption
        verify (dincr 1 = some3)

        let incrList x = [ x + 1 ]
        let lincr = incrList >=> incrList
        verify (lincr 3 = [5])

        let incr x = x + 1
        verify( ([1..5] ==> incr) = [2..6])
        verify( map incr some2 = some3)

        verify( (map incr [1..5]) = [2..6])
        verify( map incr some2 = some3)


        let s1:string = null
        let s2 = "Hello"
        verify( s1 <|> s2 = s2)

        let s3 = "World"
        verify( s2 <|> s3 = s2 )

        verify( "nice" <|> null = "nice")
        verify( null <|> "nice" = "nice")
        verify( Nullable<int>() <|> Nullable<int>(333) = Nullable<int>(333))
        verify( Nullable<int>() <|> Nullable<int>() = Nullable<int>())
        ()





    /// tuple array 를 개별 seq 의 tuple 로 반환 : [a*b] -> [a] * [b]
    //let inline unzip tpls =
    //    (tpls |> map fst, tpls |> map snd)


