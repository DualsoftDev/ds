namespace Dual.Common.FS.LSIS

open System.Linq
open System.Collections.Generic
open Microsoft.FSharp.Reflection

[<AutoOpen>]
module CollectionAlgorith =
    // https://stackoverflow.com/questions/4495597/combinations-and-permutations-in-f
    /// 주어진 seq 에서 size 갯수크기의 combination 을 생성한다.
    /// combinations 2 [1..5] |> Array.ofSeq 
    ///  [| [2; 1]; [3; 1]; [4; 1]; [5; 1];
    ///     [3; 2]; [4; 2]; [5; 2];
    ///     [4; 3]; [5; 3];
    ///     [5; 4]|]
    let combinations size samples =
        let rec combinationsHelper acc size set =
            seq {
                match size, set with 
                | n, x::xs -> 
                    if n > 0 then yield! combinationsHelper (x::acc) (n - 1) xs
                    if n >= 0 then yield! combinationsHelper acc n xs 
                | 0, [] -> yield acc
                | _, [] -> () }
        combinationsHelper [] size (samples |> List.ofSeq)


    // https://stackoverflow.com/questions/286427/calculating-permutations-in-f
    /// [1..3] |> permutations |> Array.ofSeq;;
    /// ---> [|[1; 2; 3]; [1; 3; 2]; [2; 1; 3]; [2; 3; 1]; [3; 1; 2]; [3; 2; 1]|]
    let permutations sequ =
        let rec permu list taken =
            seq {
                if Set.count taken = List.length list then
                    yield []
                else
                    for l in list do
                        if not (Set.contains l taken) then 
                            for perm in permu list (Set.add l taken) do
                                yield l::perm }
        permu (sequ |> List.ofSeq) Set.empty

    //// http://euler.synap.co.kr/prob_detail.php?id=24
    //// crashes on [1..10] |> permutations |> Array.ofSeq
    let permutations2 lst =
        let rec permu = function
            | []      -> seq [List.empty]
            | x :: xs -> Seq.collect (insertions x) (permu xs)
        and insertions x = function
            | []             -> [[x]]
            | (y :: ys) as xs -> (x::xs)::(List.map (fun x -> y::x) (insertions x ys))
        permu lst
        |> Seq.sort


    let crossProduct2 l1 l2 =
        seq {
            for el1 in l1 do
                for el2 in l2 do
                    yield el1, el2 }

    /// Seq of Seq 로 주어진 각 항목들에 대해서 cross product 수행
    /// e.g [[1; 2]; [3; ]; [7; 8; 9]]
    /// ==> [   [1; 3; 7]; [1; 3; 8]; [1; 3; 9]
    ///         [2; 3; 7]; [2; 3; 8]; [2; 3; 9] ]
    // https://stackoverflow.com/questions/32263241/cartesian-product-in-c-sharp-using-enumeratos
    let rec crossProduct(sets:'a seq seq) =
        if sets |> Seq.isEmpty then
            [ List.empty ]
        else
            let head = sets |> Seq.head
            let tailCross = crossProduct(sets |> Seq.skip 1)

            [
                for h in head do
                    for ts in tailCross do
                        yield h::ts
            ]

    /// Seq of Seq 로 주어진 각 항목들에 대해서 cross product 수행.  이때, empty sequence 는 무시
    /// e.g [[1; 2]; []; [7; 8; 9]]
    /// ==> [   [1; 7]; [1; 8]; [1; 9]
    ///         [2; 7]; [2; 8]; [2; 9] ]
    let rec crossProductIgnoringEmpty(sets:'a seq seq) =
        if sets |> Seq.isEmpty then
            [ List.empty ]
        else
            let sets = sets |> Seq.filter Seq.any
            let head = sets |> Seq.head
            let tailCross = crossProduct(sets |> Seq.skip 1)

            [
                for h in head do
                    for ts in tailCross do
                        yield h::ts
            ]

    /// <summary>
    /// 주어진 sequence 를 unsort/random shuffle/scramble 한다.
    /// http://www.fssnip.net/16/title/Sequence-Random-Permutation
    /// </summary>
    /// <param name="sqn"></param>
    let scramble (sqn : seq<'T>) =
        let rnd = new System.Random()
        let rec scramble2 (sqn : seq<'T>) =
            /// Removes an element from a sequence.
            let remove n sqn = sqn |> Seq.filter (fun x -> x <> n)

            seq {
                let x = sqn |> Seq.item (rnd.Next(0, sqn |> Seq.length))
                yield x
                let sqn' = remove x sqn
                if not (sqn' |> Seq.isEmpty) then
                    yield! scramble2 sqn'
            }
        scramble2 sqn

    // Example:
    //scramble ['1' .. '9'] |> Seq.toList
    //scramble [1..9] |> Seq.toList
    //scramble ['a' .. 'z'] |> Seq.toList


    /// <summary>
    /// http://www.fssnip.net/2n/title/Sequnsort
    /// </summary>
    /// <param name="xs"></param>
    let unsort xs =
            //let rand = System.Random(Seed=0)
            let rand = System.Random()
            xs
            |> Seq.map (fun x -> rand.Next(),x)
            |> Seq.cache
            |> Seq.sortBy fst
            |> Seq.map snd



    /// <summary>
    /// 주어진 sequence 를 계속 circular population 한다.
    /// http://www.fssnip.net/1N/title/Function-to-generate-circular-infinite-sequence-from-a-list
    /// </summary>
    /// <param name="lst"></param>
    let generateCircularSeq (lst:'a list) =
        let rec next () =
            seq {
                for element in lst do
                    yield element
                yield! next()
            }
        next()

    //for i in [1;2;3;4;5;6;7;8;9;10] |> generateCircularSeq |> Seq.take 12 do
    //    i |> System.Console.WriteLine


    /// <summary>
    /// http://www.fssnip.net/1R/title/Take-every-Nth-element-of-sequence
    /// </summary>
    /// <param name="n"></param>
    /// <param name="seq"></param>
    let everyNth n seq =
        seq |> Seq.mapi (fun i el -> el, i)              // Add index to element
            |> Seq.filter (fun (el, i) -> i % n = n - 1) // Take every nth element
            |> Seq.map fst                               // Drop index from the result


    // C# : EmConsecutive.ToConsecutiveGroups
    // http://vaskir.blogspot.com/2013/09/grouping-consecutive-integers-in-f-and.html
    /// [1; 2; 4; 5; 6; 9] |> groupConsecutive ==> [[1;2]; [4;5;6]; [9]]
    let groupConsecutive xs = 
        List.foldBack (fun x acc -> 
            match acc, x with
            | [], _ -> [[x]]
            | (h :: t) :: rest, x when h - x <= 1 -> (x :: h :: t) :: rest
            | acc, x -> [x] :: acc) xs []

