namespace Dual.Common.Core.FS

open System

#nowarn "1173"  // warning FS1173: 중위 연산자 멤버 '?<-'에 3개의 초기 인수가 있습니다. 2개 인수의 튜플이 필요합니다(예: 정적 멤버 (+) (x,y) = ...).
#nowarn "0064"  // warning FS0064: 이 구문을 사용하면 코드가 해당 형식 주석에 표시된 것보다 일반적이지 않게 됩니다. ...

[<AutoOpen>]
module PreludeAdhocPolymorphism =
    (* Pre implementations *)
    module private Async =
        let inline bind f a = async {
            let! x = a
            return! f x
        }
        let inline map f a = async {
            let! x = a
            return f x
        }
        let inline iter f a = async {
            let! x = a
            f x
        }
    module private Result =
        let orElse y x =
            match x with
            | Ok _ -> x
            | _ -> y
        let orElseWith f x =
            match x with
            | Ok _ -> x
            | _ -> f()
    module private List =
        let orElse ys xs = if List.isEmpty xs then ys else xs
        let orElseWith f xs = if List.isEmpty xs then f() else xs
    module private Seq =
        let orElse ys xs = if Seq.isEmpty xs then ys else xs
        let orElseWith f xs = if Seq.isEmpty xs then f() else xs
    module private Array =
        let orElse ys xs = if Array.isEmpty xs then ys else xs
        let orElseWith f xs = if Array.isEmpty xs then f() else xs

    (*
    // http://nut-cracker.azurewebsites.net/blog/2011/11/15/typeclasses-for-fsharp/
    // https://kwangyulseo.com/2015/01/21/emulating-haskell-type-classes-in-f/
    // https://stackoverflow.com/questions/7695393/overloading-operator-in-f
    *)
    type FAdhoc_allPairs = FAdhoc_allPairs with
        static member ($) (FAdhoc_allPairs, xs:list<_>)  = fun ys -> List.allPairs  xs ys
        static member ($) (FAdhoc_allPairs, xs:array<_>) = fun ys -> Array.allPairs xs ys
        static member ($) (FAdhoc_allPairs, xs:seq<_>)   = fun ys -> Seq.allPairs   xs ys

    type FAdhoc_append = FAdhoc_append with
        static member (?<-) (FAdhoc_append, xs:list<'a>,  ys:list<'a>)  = List.append  xs ys
        static member (?<-) (FAdhoc_append, xs:array<'a>, ys:array<'a>) = Array.append xs ys
        static member (?<-) (FAdhoc_append, xs:seq<'a>,   ys:seq<'a>)   = Seq.append   xs ys

    //type FAdhoc_average = FAdhoc_average with
    //    static member inline ($) (FAdhoc_average, xs:list<'x>):  'x when ^x: (static member (+): ^x * ^x -> ^x) and ^x: (static member DivideByInt: ^x * int -> ^x) and ^x: (static member Zero: ^x) = List.average  xs
    //    static member inline ($) (FAdhoc_average, xs:array<'x>): 'x when ^x: (static member (+): ^x * ^x -> ^x) and ^x: (static member DivideByInt: ^x * int -> ^x) and ^x: (static member Zero: ^x) = Array.average xs
    //    static member inline ($) (FAdhoc_average, xs:seq<'x>):   'x when ^x: (static member (+): ^x * ^x -> ^x) and ^x: (static member DivideByInt: ^x * int -> ^x) and ^x: (static member Zero: ^x) = Seq.average   xs


    type FAdhoc_bind = FAdhoc_bind with
        static member ($) (FAdhoc_bind, xs:list<_>)      = fun f -> List.collect  f xs
        static member ($) (FAdhoc_bind, xs:seq<_>)       = fun f -> Seq.collect   f xs
        static member ($) (FAdhoc_bind, xs:array<_>)     = fun f -> Array.collect f xs
        static member ($) (FAdhoc_bind, x:option<_>)     = fun f -> Option.bind   f x
        static member ($) (FAdhoc_bind, x:Result<_, _>)  = fun f -> Result.bind   f x
        static member ($) (FAdhoc_bind, x:Async<_>)      = fun f -> Async.bind    f x

    type FAdhoc_bindi = FAdhoc_bindi with
        static member ($) (FAdhoc_bindi, xs:list<_>)    = fun f -> List.mapi  f xs |> List.collect id
        static member ($) (FAdhoc_bindi, xs:seq<_>)     = fun f -> Seq.mapi   f xs |> Seq.collect id
        static member ($) (FAdhoc_bindi, xs:array<_>)   = fun f -> Array.mapi f xs |> Array.collect id

    type FAdhoc_choose = FAdhoc_choose with
        static member ($) (FAdhoc_choose, xs:list<_>)       = fun f -> List.choose  f xs
        static member ($) (FAdhoc_choose, xs:seq<_>)        = fun f -> Seq.choose   f xs
        static member ($) (FAdhoc_choose, xs:array<_>)      = fun f -> Array.choose f xs

    type FAdhoc_choosei = FAdhoc_choosei with
        static member ($) (FAdhoc_choosei, xs:list<_>)  = fun f -> List.mapi  f xs |> List.choose id
        static member ($) (FAdhoc_choosei, xs:seq<_>)   = fun f -> Seq.mapi   f xs |> Seq.choose id
        static member ($) (FAdhoc_choosei, xs:array<_>) = fun f -> Array.mapi f xs |> Array.choose id

    type FAdhoc_chunkBySize = FAdhoc_chunkBySize with
        static member ($) (FAdhoc_chunkBySize, xs:list<_>)     = fun f -> List.chunkBySize  f xs
        static member ($) (FAdhoc_chunkBySize, xs:seq<_>)      = fun f -> Seq.chunkBySize   f xs
        static member ($) (FAdhoc_chunkBySize, xs:array<_>)    = fun f -> Array.chunkBySize f xs

    type FAdhoc_contains = FAdhoc_contains with
        static member ($) (FAdhoc_contains, xs: list<'T>)  = fun y -> List.contains  y xs
        static member ($) (FAdhoc_contains, xs: seq<'T>)   = fun y -> Seq.contains   y xs
        static member ($) (FAdhoc_contains, xs: array<'T>) = fun y -> Array.contains y xs
        static member ($) (FAdhoc_contains, x: option<'T>) = fun y ->
            match x with
            | Some z -> z = y
            | None -> false
        static member ($) (FAdhoc_contains, x: Result<'T, _>) = fun y ->
            match x with
            | Ok z -> z = y
            | Error _ -> false


    type FAdhoc_distinct = FAdhoc_distinct with
        static member ($) (FAdhoc_distinct, xs:list<_>)  = List.distinct  xs
        static member ($) (FAdhoc_distinct, xs:seq<_>)   = Seq.distinct   xs
        static member ($) (FAdhoc_distinct, xs:array<_>) = Array.distinct xs

    type FAdhoc_distinctBy = FAdhoc_distinctBy with
        static member ($) (FAdhoc_distinctBy, xs:list<_>)  = fun keySelector -> List.distinctBy  keySelector xs
        static member ($) (FAdhoc_distinctBy, xs:seq<_>)   = fun keySelector -> Seq.distinctBy   keySelector xs
        static member ($) (FAdhoc_distinctBy, xs:array<_>) = fun keySelector -> Array.distinctBy keySelector xs

    type FAdhoc_except = FAdhoc_except with
        static member (?<-) (FAdhoc_except, xs:list<'x>,  ys:list<'x>) : list<'x>  when 'x : equality = List.except  ys xs
        static member (?<-) (FAdhoc_except, xs:array<'x>, ys:array<'x>): array<'x> when 'x : equality = Array.except ys xs
        static member (?<-) (FAdhoc_except, xs:seq<'x>,   ys:seq<'x>)  : seq<'x>   when 'x : equality = Seq.except   ys xs

    type FAdhoc_filter = FAdhoc_filter with
        static member ($) (FAdhoc_filter, xs:list<_>)   = fun f -> List.filter  f xs
        static member ($) (FAdhoc_filter, xs:seq<_>)    = fun f -> Seq.filter   f xs
        static member ($) (FAdhoc_filter, xs:array<_>)  = fun f -> Array.filter f xs

    type FAdhoc_fold = FAdhoc_fold with
        static member ($) (FAdhoc_fold, xs:list<_>)   = fun f z0 -> List.fold   f z0 xs
        static member ($) (FAdhoc_fold, xs:seq<_>)    = fun f z0 -> Seq.fold    f z0 xs
        static member ($) (FAdhoc_fold, xs:array<_>)  = fun f z0 -> Array.fold  f z0 xs
        static member ($) (FAdhoc_fold, xs:option<_>) = fun f z0 -> Option.fold f z0 xs

    type FAdhoc_foldRight = FAdhoc_foldRight with
        static member ($) (FAdhoc_foldRight, xs:list<_>)  = fun f z0 -> List.foldBack f xs z0
        static member ($) (FAdhoc_foldRight, xs:array<_>) = fun f z0 -> Array.foldBack f xs z0
        //static member ($) (FAdhoc_foldRight, x:seq<_>) =
        //    // Seq.foldBack이 직접적으로 제공되지 않으므로 List로 변환 후 처리
        //    fun f z0 -> Seq.toList x |> List.foldBack f z0
        static member ($) (FAdhoc_foldRight, xs:option<_>) = fun f z0 ->
            match xs with
            | Some v -> f v z0
            | None -> z0

    type FAdhoc_groupBy = FAdhoc_groupBy with
        static member ($) (FAdhoc_groupBy, xs:list<_>)     = fun f -> List.groupBy  f xs
        static member ($) (FAdhoc_groupBy, xs:seq<_>)      = fun f -> Seq.groupBy   f xs
        static member ($) (FAdhoc_groupBy, xs:array<_>)    = fun f -> Array.groupBy f xs


    type FAdhoc_indexed = FAdhoc_indexed with
        static member ($) (FAdhoc_indexed, xs:list<_>)  = List.indexed  xs
        static member ($) (FAdhoc_indexed, xs:seq<_>)   = Seq.indexed   xs
        static member ($) (FAdhoc_indexed, xs:array<_>) = Array.indexed xs

    type FAdhoc_inits = FAdhoc_inits with
        static member ($) (FAdhoc_inits, xs:list<_>)  = if List.isEmpty  xs then failwith "error" else List.take (List.length xs - 1) xs
        static member ($) (FAdhoc_inits, xs:array<_>) = if Array.isEmpty xs then failwith "error" else xs.[0..(Array.length xs - 2)]
        static member ($) (FAdhoc_inits, xs:seq<_>)   = if Seq.isEmpty   xs then failwith "error" else Seq.take (Seq.length xs - 1) xs

    type FAdhoc_item = FAdhoc_item with
        static member ($) (FAdhoc_item, xs:list<_>)  = fun i -> List.item  i xs
        static member ($) (FAdhoc_item, xs:array<_>) = fun i -> Array.item i xs
        static member ($) (FAdhoc_item, xs:seq<_>)   = fun i -> Seq.item   i xs

    type FAdhoc_iter = FAdhoc_iter with
        static member ($) (FAdhoc_iter, xs:list<_>)     = fun f -> List.iter   f xs
        static member ($) (FAdhoc_iter, xs:seq<_>)      = fun f -> Seq.iter    f xs
        static member ($) (FAdhoc_iter, xs:array<_>)    = fun f -> Array.iter  f xs
        static member ($) (FAdhoc_iter, x:option<_>)    = fun f -> Option.iter f x
        static member ($) (FAdhoc_iter, x:Result<_, _>) = fun f -> Result.iter f x
        static member ($) (FAdhoc_iter, x:Async<_>)     = fun f -> Async.iter  f x


    type FAdhoc_scan = FAdhoc_scan with
        static member ($) (FAdhoc_scan, xs:list<_>)  = fun f z0 -> List.scan  f z0 xs
        static member ($) (FAdhoc_scan, xs:seq<_>)   = fun f z0 -> Seq.scan   f z0 xs
        static member ($) (FAdhoc_scan, xs:array<_>) = fun f z0 -> Array.scan f z0 xs

    type FAdhoc_scanBack = FAdhoc_scanBack with
        static member ($) (FAdhoc_scanBack, x:list<_>)  = fun f z0 -> List.scanBack f x z0
        static member ($) (FAdhoc_scanBack, x:array<_>) = fun f z0 -> Array.scanBack f x z0
        //static member ($) (FAdhoc_scanBack, x:seq<_>) =
        //    fun f z0 ->
        //        let listX = Seq.toList x
        //        List.scanBack f listX z0



    type FAdhoc_map = FAdhoc_map with
        static member inline ($) (FAdhoc_map, xs:list<_>)  = fun f -> List.map       f xs
        static member inline ($) (FAdhoc_map, xs:seq<_>)   = fun f -> Seq.map        f xs
        static member inline ($) (FAdhoc_map, xs:array<_>) = fun f -> Array.map      f xs
        static member inline ($) (FAdhoc_map, x:option<_>) = fun f -> Option.map     f x
        static member inline ($) (FAdhoc_map, x:Result<_, _>)  = fun f -> Result.map f x
        static member inline ($) (FAdhoc_map, x:Async<_>)  = fun f -> Async.map f x

    type FAdhoc_mapi = FAdhoc_mapi with
        static member ($) (FAdhoc_mapi, xs:list<_>)   = fun f -> List.mapi  f xs
        static member ($) (FAdhoc_mapi, xs:seq<_>)    = fun f -> Seq.mapi   f xs
        static member ($) (FAdhoc_mapi, xs:array<_>)  = fun f -> Array.mapi f xs


    (* Polymorphic 적용 불가:  type 매개변수 <'T> 가 필요한 곳 *)
    //type FAdhoc_ofType = FAdhoc_ofType with
    //    static member inline ($) (FAdhoc_ofType, xs:list<_>)  = fun () -> List.choose  (fun x -> match box x with | :? 'T as t -> Some t | _ -> None) xs
    //    static member inline ($) (FAdhoc_ofType, xs:array<_>) = fun () -> Array.choose (fun x -> match box x with | :? 'T as t -> Some t | _ -> None) xs
    //    static member inline ($) (FAdhoc_ofType, xs:seq<_>)   = fun () -> Seq.choose   (fun x -> match box x with | :? 'T as t -> Some t | _ -> None) xs
    //    //static member inline ($) (FAdhoc_ofType, x:option<_>) = fun () -> match box x with | :? 'T as t -> Some t | _ -> None

    ///// 범용 ofType 함수.  type 'T 에 맞는 항목만 추출
    //let inline ofType<'T> (xs: #seq<_>) = FAdhoc_ofType $ xs <| () : seq<'T>



    type FAdhoc_orElse = FAdhoc_orElse with
        /// 주의: FAdhoc_orElse (Option.orElse) 사용 시 short circuit 기능이 없다.
        static member (?<-) (FAdhoc_orElse, y:option<'a>,   x:option<'a>)   = Option.orElse y x
        static member (?<-) (FAdhoc_orElse, y:Result<'c, 'd>, x:Result<'c, 'd>) = Result.orElse y x
        //static member (?<-) (FAdhoc_orElse, y:Nullable<'a>, x:Nullable<'a>) = if x.HasValue then x else y
        //static member (?<-) (FAdhoc_orElse, y:'a when ^a : not struct, x:'a when ^a : not struct) = if isNull x then y else x
        static member (?<-) (FAdhoc_orElse, ys:list<_> , xs:list<_> )       = List.orElse  ys xs
        static member (?<-) (FAdhoc_orElse, ys:seq<_>  , xs:seq<_>  )       = Seq.orElse   ys xs
        static member (?<-) (FAdhoc_orElse, ys:array<_>, xs:array<_>)       = Array.orElse ys xs

    type FAdhoc_orElseWith = FAdhoc_orElseWith with
        static member ($) (FAdhoc_orElseWith, xs:list<_>)   = fun f -> List.orElseWith      f xs
        static member ($) (FAdhoc_orElseWith, xs:seq<_>)    = fun f -> Seq.orElseWith       f xs
        static member ($) (FAdhoc_orElseWith, xs:array<_>)  = fun f -> Array.orElseWith     f xs
        static member ($) (FAdhoc_orElseWith, x:option<_>)  = fun f -> Option.orElseWith    f x
        static member ($) (FAdhoc_orElseWith, x:Result<_, _>)  = fun f -> Result.orElseWith f x


    type FAdhoc_pairwise = FAdhoc_pairwise with
        static member ($) (FAdhoc_pairwise, xs:list<_>)  = List.pairwise  xs
        static member ($) (FAdhoc_pairwise, xs:seq<_>)   = Seq.pairwise   xs
        static member ($) (FAdhoc_pairwise, xs:array<_>) = Array.pairwise xs

    type FAdhoc_partition = FAdhoc_partition with
        static member ($) (FAdhoc_partition, xs:list<_>)  = fun predicate -> List.partition  predicate xs
        static member ($) (FAdhoc_partition, xs:array<_>) = fun predicate -> Array.partition predicate xs
        //static member ($) (FAdhoc_partition, xs:seq<_>)   = fun predicate -> Seq.partition   predicate xs


    type FAdhoc_reverse = FAdhoc_reverse with
        static member ($) (FAdhoc_reverse, xs:list<_>)  = List.rev  xs
        static member ($) (FAdhoc_reverse, xs:array<_>) = Array.rev xs
        //static member ($) (FAdhoc_reverse, xs:seq<_>)   = Seq.toArray xs |> Array.rev |> Array.toSeq


    type FAdhoc_sortWith = FAdhoc_sortWith with
        static member ($) (FAdhoc_sortWith, xs:list<_>)  = fun comparer -> List.sortWith  comparer xs
        static member ($) (FAdhoc_sortWith, xs:array<_>) = fun comparer -> Array.sortWith comparer xs
        static member ($) (FAdhoc_sortWith, xs:seq<_>)   = fun comparer -> Seq.sortWith   comparer xs



    type FAdhoc_sort = FAdhoc_sort with
        static member ($) (FAdhoc_sort, xs:list<_>)  = List.sort  xs
        static member ($) (FAdhoc_sort, xs:seq<_>)   = Seq.sort   xs
        static member ($) (FAdhoc_sort, xs:array<_>) = Array.sort xs

    type FAdhoc_sortBy = FAdhoc_sortBy with
        static member ($) (FAdhoc_sortBy, xs:list<_>)  = fun keySelector -> List.sortBy  keySelector xs
        static member ($) (FAdhoc_sortBy, xs:seq<_>)   = fun keySelector -> Seq.sortBy   keySelector xs
        static member ($) (FAdhoc_sortBy, xs:array<_>) = fun keySelector -> Array.sortBy keySelector xs

    type FAdhoc_sortDescending = FAdhoc_sortDescending with
        static member ($) (FAdhoc_sortDescending, xs:list<_>)  = List.sortDescending  xs
        static member ($) (FAdhoc_sortDescending, xs:seq<_>)   = Seq.sortDescending   xs
        static member ($) (FAdhoc_sortDescending, xs:array<_>) = Array.sortDescending xs

    type FAdhoc_sortByDescending = FAdhoc_sortByDescending with
        static member ($) (FAdhoc_sortByDescending, xs:list<_>)  = fun keySelector -> List.sortByDescending  keySelector xs
        static member ($) (FAdhoc_sortByDescending, xs:seq<_>)   = fun keySelector -> Seq.sortByDescending   keySelector xs
        static member ($) (FAdhoc_sortByDescending, xs:array<_>) = fun keySelector -> Array.sortByDescending keySelector xs



    type FAdhoc_skip = FAdhoc_skip with
        static member ($) (FAdhoc_skip, xs:list<_>)       = fun n -> List.skip  n xs
        static member ($) (FAdhoc_skip, xs:seq<_>)        = fun n -> Seq.skip   n xs
        static member ($) (FAdhoc_skip, xs:array<_>)      = fun n -> Array.skip n xs

    type FAdhoc_skipWhile = FAdhoc_skipWhile with
        static member ($) (FAdhoc_skipWhile, xs:list<_>)  = fun predicate -> List.skipWhile  predicate xs
        static member ($) (FAdhoc_skipWhile, xs:seq<_>)   = fun predicate -> Seq.skipWhile   predicate xs
        static member ($) (FAdhoc_skipWhile, xs:array<_>) = fun predicate -> Array.skipWhile predicate xs

    type FAdhoc_splitAt = FAdhoc_splitAt with
        static member ($) (FAdhoc_splitAt, xs:list<_>)   = fun n -> List.splitAt  n xs
        static member ($) (FAdhoc_splitAt, xs:array<_>)  = fun n -> Array.splitAt n xs
        //static member ($) (FAdhoc_splitAt, xs:seq<_>)  = fun n -> Seq.splitAt   n xs

    type FAdhoc_tail = FAdhoc_tail with
        static member ($) (FAdhoc_tail, xs:list<_>)  = List.tail xs
        static member ($) (FAdhoc_tail, xs:array<_>) = xs[1..]
        static member ($) (FAdhoc_tail, xs:seq<_>)   = Seq.skip 1 xs

    type FAdhoc_take = FAdhoc_take with
        static member ($) (FAdhoc_take, xs:list<_>)       = fun n -> List.take  n xs
        static member ($) (FAdhoc_take, xs:seq<_>)        = fun n -> Seq.take   n xs
        static member ($) (FAdhoc_take, xs:array<_>)      = fun n -> Array.take n xs

    type FAdhoc_takeWhile = FAdhoc_takeWhile with
        static member ($) (FAdhoc_takeWhile, xs:list<_>)  = fun predicate -> List.takeWhile  predicate xs
        static member ($) (FAdhoc_takeWhile, xs:seq<_>)   = fun predicate -> Seq.takeWhile   predicate xs
        static member ($) (FAdhoc_takeWhile, xs:array<_>) = fun predicate -> Array.takeWhile predicate xs

    type FAdhoc_tryInits = FAdhoc_tryInits with
        static member ($) (FAdhoc_tryInits, xs:list<_>)  =
            if List.isEmpty xs then None
            else Some (List.take (List.length xs - 1) xs)

        static member ($) (FAdhoc_tryInits, xs:array<_>) =
            if Array.isEmpty xs then None
            else Some (xs.[0..(Array.length xs - 2)])

        static member ($) (FAdhoc_tryInits, xs:seq<_>) =
            if Seq.isEmpty xs then None
            else Some (Seq.take (Seq.length xs - 1) xs)


    type FAdhoc_tryTail = FAdhoc_tryTail with
        static member ($) (FAdhoc_tryTail, xs:list<_>)  =
            if List.isEmpty xs then None
            else Some (List.tail xs)

        static member ($) (FAdhoc_tryTail, xs:array<_>) =
            if Array.isEmpty xs then None
            else Some (xs.[1..])

        static member ($) (FAdhoc_tryTail, xs:seq<_>) =
            if Seq.isEmpty xs then None
            else Some (Seq.skip 1 xs)


    //type FAdhoc_tryPicki = FAdhoc_tryPicki with
    //    static member ($) (FAdhoc_tryPicki, xs:list<_>)  = fun f -> List.mapi  f xs |> List.pick id
    //    static member ($) (FAdhoc_tryPicki, xs:seq<_>)   = fun f -> Seq.mapi   f xs |> Seq.pick id
    //    static member ($) (FAdhoc_tryPicki, xs:array<_>) = fun f -> Array.mapi f xs |> Array.pick id


    type FAdhoc_union = FAdhoc_union with
        static member inline ($) (FAdhoc_union, xs:seq<'T>)   = fun ys -> Seq.append   xs ys |> Seq.distinct
        static member inline ($) (FAdhoc_union, xs:list<'T>)  = fun ys -> List.append  xs ys |> List.distinct
        static member inline ($) (FAdhoc_union, xs:array<'T>) = fun ys -> Array.append xs ys |> Array.distinct


    type FAdhoc_windowed = FAdhoc_windowed with
        static member ($) (FAdhoc_windowed, xs:list<_>)  = fun size -> List.windowed  size xs
        static member ($) (FAdhoc_windowed, xs:array<_>) = fun size -> Array.windowed size xs
        static member ($) (FAdhoc_windowed, xs:seq<_>)   = fun size -> Seq.windowed   size xs

    /// x + [xs] or [xs] + x
    type FAdhoc_Xpend = FAdhoc_Xpend with
        static member (?<-) (FAdhoc_Xpend, xs:list<'a>,  y:'a) = List.append  xs [y]
        static member (?<-) (FAdhoc_Xpend, xs:array<'a>, y:'a) = Array.append xs [|y|]
        static member (?<-) (FAdhoc_Xpend, xs:seq<'a>,   y:'a) = Seq.append   xs (seq {y})
        static member (?<-) (FAdhoc_Xpend, x:'a, ys:list<'a> ) = x::ys
        static member (?<-) (FAdhoc_Xpend, x:'a, ys:array<'a>) = Array.append [|x|] ys
        static member (?<-) (FAdhoc_Xpend, x:'a, ys:seq<'a>  ) = Seq.append (seq{x}) ys





    type FAdhoc_zip = FAdhoc_zip with
        static member ($) (FAdhoc_zip, xs:list<_>)  = fun ys -> List.zip  xs ys
        static member ($) (FAdhoc_zip, xs:array<_>) = fun ys -> Array.zip xs ys
        static member ($) (FAdhoc_zip, xs:seq<_>)   = fun ys -> Seq.zip   xs ys



    /// 범용 bind ({Array, List, Seq}.bind)
    let inline (>>=)       x f = FAdhoc_bind        $ x <| f
    /// 범용 map ({Array, List, Seq}.map).  Haskell 의 <$>
    let inline (>>-)       x f = FAdhoc_map         $ x <| f
    /// 범용 iter ({Array, List, Seq, Option}.iter)
    let inline (>>:)       x f = FAdhoc_iter        $ x <| f

    /// 범용 average ({Array, List, Seq}.average)
    let inline average xs = Seq.average xs
    /// 범용 averageBy 함수
    let inline averageBy xs = Seq.averageBy xs

    /// 범용 allPairs ({Array, List, Seq}.allPairs)
    let inline allPairs xs ys = FAdhoc_allPairs $ xs <| ys

    /// 범용 bind ({Array, List, Seq}.bind).  (>>=)
    let inline bind        f x = FAdhoc_bind        $ x <| f
    /// 범용 bindi ({Array, List, Seq}.bindi) => (int * T') seq
    let inline bindi       f x = FAdhoc_bindi       $ x <| f
    /// 범용 bind/collect ({Array, List, Seq}.bind).  .  (>>=)
    let inline collect     f x = FAdhoc_bind        $ x <| f
    /// 범용 choose ({Array, List, Seq}.bind)
    let inline choose     f x = FAdhoc_choose     $ x <| f
    /// 범용 choosei ({Array, List, Seq}.choosei) => (int * T') seq
    let inline choosei     f x = FAdhoc_choosei     $ x <| f
    /// 범용 chunkBySize ({Array, List, Seq}.chunkBySize)
    let inline chunkBySize f x = FAdhoc_chunkBySize $ x <| f
    /// 범용 bindi/collecti ({Array, List, Seq}.bindi) => (int * T') seq
    let inline collecti    f x = FAdhoc_bindi       $ x <| f
    /// 주어진 seq (or option) xs 에 item x 가 포함되어 있는지 여부를 확인
    let inline contains x xs = FAdhoc_contains $ xs <| x
    /// 범용 distinct ({Array, List, Seq}.distinct)
    let inline distinct    x   = FAdhoc_distinct    $ x
    /// 범용 distinctBy ({Array, List, Seq}.distinctBy)
    let inline distinctBy keySelector x = FAdhoc_distinctBy $ x <| keySelector

    /// 범용 except ({Array, List, Seq}.except)
    let inline except ys xs = (?<-) FAdhoc_except xs ys


    /// 범용 exists ({Array, List, Seq}.exists)
    let inline exists predicate xs = Seq.exists predicate xs

    /// 범용 filter ({Array, List, Seq}.filter).  (f:'x -> bool), xs:['x]
    let inline filter      f xs = FAdhoc_filter      $ xs <| f
    /// 범용 find ({Array, List, Seq}.find)
    let inline find predicate xs = Seq.find predicate xs
    /// 범용 fold
    let inline fold f z0 xs = FAdhoc_fold $ xs <| f <| z0
    /// 범용 foldRight (or foldBack)
    let inline foldRight f xs z0 = FAdhoc_foldRight $ xs <| f <| z0
    /// 범용 foldRight (or foldBack)
    let inline foldBack  f xs z0 = FAdhoc_foldRight $ xs <| f <| z0
    /// 범용 forall ({Array, List, Seq}.forall)
    let inline forall predicate xs = Seq.forall predicate xs

    /// 범용 groupBy ({Array, List, Seq}.groupBy)
    let inline groupBy     f x = FAdhoc_groupBy     $ x <| f

    /// 범용 head ({Array, List, Seq}.head)
    let inline head xs = Seq.head xs
    /// 범용 item / nth 함수
    let inline item index xs = FAdhoc_item $ xs <| index
    /// 범용 indexed ({Array, List, Seq}.indexed) => (int * T') seq
    let inline indexed     x   = FAdhoc_indexed     $ x
    /// 범용 init ({Array, List, Seq}.init).  Last 제외한 모든 항목
    let inline inits xs = FAdhoc_inits $ xs
    /// 범용 iter ({Array, List, Seq}.iter).  (>>:)
    let inline iter        f xs = FAdhoc_iter        $ xs <| f
    /// 범용 iteri ({Array, List, Seq}.iteri)
    let inline iteri       f xs = Seq.iteri f xs
    /// 범용 last ({Array, List, Seq}.last)
    let inline last xs = Seq.last xs
    /// 범용 map ({Array, List, Seq}.map).  (>>-)
    let inline map         f x = FAdhoc_map         $ x <| f
    /// 범용 mapi ({Array, List, Seq}.mapi).  xs |> mapi (fun i v -> ..)
    let inline mapi        f xs = FAdhoc_mapi       $ xs <| f

    /// 범용 minimum 함수.  (min 은 2 개 중 최소 구하는 함수 이름으로 사용되므로 minimum 으로 작명)
    let inline minimum xs = Seq.min xs

    /// 범용 minBy 함수.
    let inline minBy f xs = Seq.minBy f xs

    /// 범용 max 함수  (max 은 2 개 중 최소 구하는 함수 이름으로 사용되므로 maximum 으로 작명)
    let inline maximum xs = Seq.max xs

    /// 범용 maxBy 함수
    let inline maxBy f xs = Seq.maxBy f xs


    /// 범용 item / nth 함수
    [<Obsolete("Use item instead.")>]
    let inline nth index xs = FAdhoc_item $ xs <| index

    /// 대상 값이 None, [] 등이면 주어진 함수 수행 값을 선택한다.   Option, Result, Collection 등에 적용
    let inline orElseWith  f x = FAdhoc_orElseWith  $ x <| f
    /// 대상 값이 None, [] 등이면 주어진 후보 값을 선택한다.
    let inline orElse      y x = (?<-) FAdhoc_orElse y x

    /// 범용 pairwise ({Array, List, Seq}.pairwise)
    let inline pairwise    x   = FAdhoc_pairwise    $ x
    /// 범용 partition ({Array, List}.partition)
    let inline partition predicate xs = FAdhoc_partition $ xs <| predicate

    /// 범용 pick ({Array, List, Seq}.pick)
    let inline pick f xs = Seq.pick f xs

    let inline splitAt n xs = FAdhoc_splitAt $ xs <| n

    /// 범용 sumBy 함수
    let inline sum xs = Seq.sum xs

    /// 범용 sumBy 함수
    let inline sumBy f xs = Seq.sumBy f xs


    /// 범용 reduce ({Array, List, Seq}.reduce)
    let inline reduce f xs = Seq.reduce f xs

    /// 범용 reverse ({Array, List}.reverse)
    let inline reverse xs = FAdhoc_reverse $ xs


    /// 범용 sort ({Array, List, Seq}.sort)
    let inline sort        x   = FAdhoc_sort        $ x
    /// 범용 sortBy ({Array, List, Seq}.sortBy)
    let inline sortBy keySelector x = FAdhoc_sortBy $ x <| keySelector
    /// 범용 sortDescending ({Array, List, Seq}.sortDescending)
    let inline sortDescending x = FAdhoc_sortDescending $ x
    /// 범용 sortByDescending ({Array, List, Seq}.sortByDescending)
    let inline sortByDescending keySelector x = FAdhoc_sortByDescending $ x <| keySelector
    /// 범용 sortWith ({Array, List, Seq}.sortWith)
    let inline sortWith comparer xs = FAdhoc_sortWith $ xs <| comparer
    /// 범용 scan.  (f:'z->'t->'z) (z0:'z)
    let inline scan f z0 xs = FAdhoc_scan $ xs <| f <| z0
    /// 범용 scanBack.  (f:'x->'z->'z) x (z0:'z)
    let inline scanBack f xs z0 = FAdhoc_scanBack $ xs <| f <| z0

    /// 범용 tail ({Array, List, Seq}.tail).  Head 제외한 모든 항목
    let inline tail xs = FAdhoc_tail $ xs
    /// 범용 take ({Array, List, Seq}.take)
    let inline take n xs = FAdhoc_take $ xs <| n
    /// 범용 skip ({Array, List, Seq}.skip)
    let inline skip n xs = FAdhoc_skip $ xs <| n
    /// 범용 takeWhile ({Array, List, Seq}.takeWhile)
    let inline takeWhile predicate xs = FAdhoc_takeWhile $ xs <| predicate
    /// 범용 skipWhile ({Array, List, Seq}.skipWhile)
    let inline skipWhile predicate xs = FAdhoc_skipWhile $ xs <| predicate


    /// 범용 tryFind ({Array, List, Seq}.tryFind)
    let inline tryFind f xs = Seq.tryFind f xs
    /// 범용 tryHead ({Array, List, Seq}.tryHead)
    let inline tryHead xs = Seq.tryHead xs
    /// 범용 tryInits
    let inline tryInits xs = FAdhoc_tryInits $ xs
    /// 범용 tryLast ({Array, List, Seq}.tryLast)
    let inline tryLast xs = Seq.tryLast xs
    /// 범용 tryPick ({Array, List, Seq}.tryPick)
    let inline tryPick f xs = Seq.tryPick f xs
    /// 범용 tryInits
    let inline tryTail xs = FAdhoc_tryTail $ xs

    /// 범용 union 함수
    let inline union xs ys = FAdhoc_union $ xs <| ys

    /// 범용 zip ({Array, List, Seq}.zip)
    let inline zip xs ys = FAdhoc_zip $ xs <| ys

    /// 범용 windowed ({Array, List, Seq}.windowed)
    let inline windowed size xs = FAdhoc_windowed $ xs <| size


    (* Operators *)

    /// General OrElse.  choice on option/collection type.
    ///
    /// - None <|> Some 1 === Some 1
    ///
    /// - [] <|> [1;2] === [1; 2]
    let inline ( <|> )     x y = (?<-) FAdhoc_orElse y x

    /// append two lists / arrays / sequences, Haskell 의 ++ 연산자와 동일
    let inline ( @ )       x y = (?<-) FAdhoc_append x y
    /// append two lists / arrays / sequences, Haskell 의 ++ 연산자와 동일
    let inline ( ++ )      x y = (?<-) FAdhoc_append x y

    /// append (not prepand) element to a list / array / sequence
    ///
    /// - 3 +++ [1] === [3; 1]
    let inline ( +++ )     x y = (?<-) FAdhoc_Xpend  x y

    /// Option.defaultValue 혹은 defaultArg 와 동일 기능 (argument 순서가 뒤바뀜)
    ///
    /// - a |? coverValue === a |> Option.defaultValue coverValue
    let (|?) = defaultArg

    /// Option.defaultWith 와 동일 기능 (argument 순서가 뒤바뀜)
    ///
    /// e.g Some 1 |?? (fun() -> 20)
    let (|??) opt f = Option.defaultWith f opt

    // https://riptutorial.com/fsharp/example/16297/how-to-compose-values-and-functions-using-common-operators

    /// Kliesli fish for option
    (* tf: ('a -> 'b option) -> uf: ('b -> 'c option) -> v: 'a -> 'c option *)
    let inline (>=>) tf uf = fun v -> tf v >>= uf      // == fun v -> (tf v) >>= uf

    /// reverse Kliesli fish for option
    (* uf: ('a -> 'b option) -> tf: ('c -> 'a option) -> v: 'c -> 'b option
        v: 'b option <- 'c <- uf: ('b option <- 'a ) <- tf: ('a option <- 'c)
        *)
    let inline (<=<) uf tf = fun v -> tf v >>= uf


module private PreludeAdhocPolymorphismTestSample =
    let private testme() =
        (*
        #r @"F:\Git\ds\DsDotNet\src\Engine\Dual.Common.Core.FS\bin\Debug\net48\Dual.Common.Core.FS.dll"
        open Dual.Common.Core.FS
        open System

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
        verify( ([1..5] >>- incr) = [2..6])
        verify( map incr some2 = some3)

        verify( (map incr [1..5]) = [2..6])
        verify( map incr some2 = some3)


        //let s1:string = null
        //let s2 = "Hello"
        //verify( String.orElse s2 s1 = s2)

        //let s3 = "World"
        //verify( String.orElse s3 s2 = s2 )

        //verify( "nice" <|> null = "nice")
        //verify( null <|> "nice" = "nice")
        //verify( Nullable<int>() <|> Nullable<int>(333) = Nullable<int>(333))
        //verify( Nullable<int>() <|> Nullable<int>() = Nullable<int>())

        verify( orElse [3] [1] = [1])
        verify( orElse [3] [] = [3])
        verify( orElse [] [] = [])
        verify( [1] <|> [3] = [1])
        verify( []  <|> [3] = [3])
        verify( []  <|> []  = [])

        verify( orElse (Some 3) None  = Some 3)
        verify( orElse (Some 3) (Some 5) = Some 5)
        verify( orElse (Some 3) None     = Some 3)
        verify( Some 3 <|> None   = Some 3)
        verify( Some 5 <|> Some 3 = Some 5)
        verify( None <|> Some 3   = Some 3)
        verify( Option.orElse (Some 3) (Some 5) = Some 5)
        verify( Option.orElse (Some 3) None = Some 3)

        verify( orElse (Ok 3) (Ok 11) = Ok 11)
        verify( orElse (Ok 3) (Error "X") = Ok 3)
        verify( Ok 11 <|> (Ok 3) = Ok 11)
        verify( Error "X" <|> Ok 3 = Ok 3)


        verify( orElseWith (fun () -> [1]) [3] = [3])
        verify( orElseWith (fun () -> [1]) [] = [1])

        verify( Option.orElseWith (fun () -> Some 1) (Some 3) = (Some 3))
        verify( Option.orElseWith (fun () -> Some 1) None = (Some 1))

        ()





    /// tuple array 를 개별 seq 의 tuple 로 반환 : [a*b] -> [a] * [b]
    //let inline unzip tpls =
    //    (tpls |> map fst, tpls |> map snd)


