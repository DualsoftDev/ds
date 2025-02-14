namespace Dual.Common.Base.FS

open System
open System.Runtime.CompilerServices

type EmCollection =
    /// Seq.allPairs 확장
    [<Extension>] static member AllPairs(xs:'x seq, ys:'y seq) = Seq.allPairs xs ys
    /// Array.allPairs 확장
    [<Extension>] static member AllPairs(xs:'x[], ys:'y[]) = Array.allPairs xs ys

    /// Seq.bind 확장
    [<Extension>] static member Bind(xs: 'x seq, f: Func<'x, 'y seq>) = Seq.collect (fun a -> f.Invoke(a)) xs
    /// Array.bind 확장
    [<Extension>] static member Bind(xs: 'x[], f: Func<'x, 'y[]>) = Array.collect (fun a -> f.Invoke(a)) xs
    /// Seq.bindi 확장
    [<Extension>] static member Bindi(xs: 'x seq, f: Func<'x, int, 'y seq>) = Seq.mapi (fun i a -> f.Invoke(a, i)) xs |> Seq.collect id
    /// Array.bind 확장
    [<Extension>] static member Bindi(xs: 'x[], f: Func<'x, int, 'y[]>) = Array.mapi (fun i a -> f.Invoke(a, i)) xs |> Array.collect id

    /// Seq.cache 확장
    [<Extension>] static member Cache(xs: 'x seq) = xs |> Seq.cache

    /// Seq.chunkBySize 확장.  C# IEnumerable.Chunk 존재하나, Array 등에 적용시 결과가 Array 가 아님
    [<Extension>] static member ChunkBySize(xs:'x seq, chunkSize:int) = xs |> Seq.chunkBySize chunkSize
    /// Array.chunkBySize 확장.  C# IEnumerable.Chunk 존재하나, Array 등에 적용시 결과가 Array 가 아님
    [<Extension>] static member ChunkBySize(xs:'x[], chunkSize:int) = xs |> Array.chunkBySize chunkSize

    // Count(), Distinct() 등은 System.Linq 를 그냥 사용할 것.

    /// Seq.except 확장.  C# IEnumerable.Except 존재하나, Array 등에 적용시 결과가 Array 가 아님
    [<Extension>] static member Except(xs: 'x seq, excepts: 'x seq) = xs |> Seq.except excepts
    /// Array.except 확장.  C# IEnumerable.Except 존재하나, Array 등에 적용시 결과가 Array 가 아님
    [<Extension>] static member Except(xs: 'x[], excepts: 'x[]) = xs |> Array.except excepts

    /// Seq.item / nth 확장. IEnumerable<'T>.ElementAt 과 동일
    [<Extension>] static member Item(xs: 'x seq, index: int) = xs |> Seq.item index
    /// Array.item / nth 확장. IEnumerable<'T>.ElementAt 과 유사
    [<Extension>] static member Item(xs: 'x[], index: int) = xs.[index]

    //[<Extension>]
    //static member InnerJoin(outer, inner, outerKeySelector:Func<'o, 'k>, inputKeySelector:Func<'i, 'k>, resultSelector:Func<'r>) =
    //    innerJoin outer inner outerKeySelector inputKeySelector resultSelector

    /// SQL InnerJoin
    [<Extension>]
    static member InnerJoin(outer: seq<'o>, inner: 'i seq,
                            outerKeySelector: Func<'o, 'k>,
                            innerKeySelector: Func<'i, 'k>,
                            resultSelector: Func<'o, 'i, 'r>) : 'r seq =
        // 기존 innerJoin 함수 호출 (F# 함수에 C#의 Func 타입을 래핑하여 전달)
        innerJoin outer inner
                  (fun o -> outerKeySelector.Invoke(o))  // F#의 함수로 변환
                  (fun i -> innerKeySelector.Invoke(i))  // F#의 함수로 변환
                  (fun o i -> resultSelector.Invoke(o, i))  // F#의 함수로 변환
        //|> Seq.toList :> 'r seq  // IEnumerable<'r>로 변환 (C#과의 호환성 유지)

    /// SQL NaturalJoin
    /// C#에서 기존 naturalJoin 사용
    [<Extension>]
    static member NaturalJoin(outer: 'o seq, inner: 'i seq,
                              keySelector: Func<'o, 'i, bool>,
                              resultSelector: Func<'o, 'i, 'r>) : 'r seq =
        // 기존 naturalJoin 함수 호출
        naturalJoin outer inner
                    (fun o i -> keySelector.Invoke(o, i))  // F#의 함수로 변환
                    (fun o i -> resultSelector.Invoke(o, i))  // F#의 함수로 변환



    /// Seq.map 확장.  C# IEnumerable.Select 존재하나, Array 등에 적용시 결과가 Array 가 아님
    [<Extension>] static member Map(xs:'x seq, f:Func<'x, 'y>) = Seq.map (fun a -> f.Invoke(a)) xs
    /// Array.map 확장.  C# IEnumerable.Select 존재하나, Array 등에 적용시 결과가 Array 가 아님
    [<Extension>] static member Map(xs:'x [], f:Func<'x, 'y>) = Array.map (fun a -> f.Invoke(a)) xs


    /// Seq.item / nth 확장. IEnumerable<'T>.ElementAt 과 동일
    [<Extension>] static member Nth(xs: 'x seq, index: int) = xs |> Seq.item index
    /// Array.item / nth 확장. IEnumerable<'T>.ElementAt 과 유사
    [<Extension>] static member Nth(xs: 'x[], index: int) = xs.[index]

    /// Array.partition 확장
    [<Extension>] static member Partition(xs:'x[], predicate:Func<'x, bool>) = xs |> Array.partition (fun a -> predicate.Invoke(a))

    /// Seq.windowed 확장
    [<Extension>] static member Windowed(xs:'x seq, windowSize:int) = xs |> Seq.windowed windowSize
    /// Array.windowed 확장
    [<Extension>] static member Windowed(xs:'x[], windowSize:int) = xs |> Array.windowed windowSize

