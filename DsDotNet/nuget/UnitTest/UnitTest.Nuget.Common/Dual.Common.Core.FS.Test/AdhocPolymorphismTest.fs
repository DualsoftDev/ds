namespace T

open Dual.Common.Core.FS
open NUnit.Framework
open Dual.Common.UnitTest.FS
open Dual.Common.Base.FS

[<TestFixture>]
type AdhocPolymorphismTest() =
    [<Test>]
    member _.AdhocPolymorphismTest() =
        let z: int list = []
        let xs = [1]

        // A
        allPairs [1; 2] [3; 4] === [(1, 3); (1, 4); (2, 3); (2, 4)]
        allPairs [|1; 2|] [|3; 4|] === [|(1, 3); (1, 4); (2, 3); (2, 4)|]

        // D
        distinctBy (fun x -> x % 2) [1; 2; 3; 4; 5] === [1; 2]

        // E
        [0..10] |> except [0..2..10] === [1..2..10]
        [|0..10|] |> except [|0..2..10|] === [|1..2..10|]

        // F
        find (fun x -> x > 1) [1; 2; 3] === 2
        fold (+) 0 [1..10] === 55
        fold (+) 0 (seq {1..10}) === 55
        foldRight (+) [1..10] 0 === 55
        foldRight (+) ([|1..10|]) 0 === 55
        forall (fun x -> x > 0) [1; 2; 3] === true

        // H
        List.head xs === 1
        head xs === 1
        (fun () -> List.head z |> ignore) |> ShouldFail
        (fun () -> head z |> ignore) |> ShouldFail
        List.tryHead z === None
        head [1; 2; 3; 4] === 1
        head [|1; 2; 3; 4|] === 1
        head (seq {1; 2; 3; 4}) === 1

        // I
        inits [1; 2; 3; 4] === [1; 2; 3]
        inits [|1; 2; 3; 4|] === [|1; 2; 3|]
        inits (seq {1; 2; 3; 4}) |> toArray === [|1; 2; 3|]
        (fun () -> inits [] |> ignore) |> ShouldFail
        inits [1] === z
        (inits (seq {1; 2; 3; 4}) |> toArray) === (seq [1; 2; 3] |> toArray)


        // L
        List.last xs === 1      // ????
        last xs === 1
        last [1; 2; 3; 4] === 4
        last [|1; 2; 3; 4|] === 4
        last (seq {1; 2; 3; 4}) === 4



        // M
        [1; 2; 3; 4; 5] |> minBy (fun x -> -x) === 5
        [|1; 2; 3; 4; 5|] |> minBy (fun x -> x % 3) === 3
        [|1; 2; 3; 4; 5|] |> maxBy (fun x -> x % 3) === 2
        [0..10] |> map float |> List.average === 5.0
        [0..10] |> map float |> average === 5.0
        [0..10] |> minimum === 0
        [0..10] |> maximum === 10

        /// N
        [0..10] |> item 5 === 5

        // P
        pairwise [1; 2; 3; 4] === List.pairwise [1; 2; 3; 4]
        pairwise [|1; 2; 3; 4|] === Array.pairwise [|1; 2; 3; 4|]
        partition (fun x -> x % 2 = 0) [1; 2; 3; 4; 5] === ([2; 4], [1; 3; 5])
        pick (fun x -> if x > 1 then Some(x * 2) else None) [1; 2; 3; 4] === 4
        (fun () -> pick (fun x -> None) [1; 2; 3; 4]) |> ShouldFail

        // R
        reduce (+) [1; 2; 3; 4] === 10
        reduce (+) (seq {1; 2; 3; 4}) === 10
        reduce (*) [|1; 2; 3; 4|] === 24
        reverse [1; 2; 3; 4] === [4; 3; 2; 1]
        reverse [|1; 2; 3; 4|] === [|4; 3; 2; 1|]

        // S
        scan (fun x acc -> x + acc) 0 [1; 2; 3] === [0; 1; 3; 6]
        scanBack (fun x acc -> x + acc) [1; 2; 3] 0 === [6; 5; 3; 0]
        skip 2 [1; 2; 3; 4; 5] === [3; 4; 5]
        skipWhile (fun x -> x < 4) [1; 2; 3; 4; 5] === [4; 5]
        sortBy (fun x -> -x) [3; 1; 4; 1; 5; 9] === [9; 5; 4; 3; 1; 1]
        sortByDescending (fun x -> x % 3) [3; 1; 4; 1; 5; 9] === [5; 1; 4; 1; 3; 9]
        sortDescending [3; 1; 4; 1; 5; 9] === [9; 5; 4; 3; 1; 1]
        sortWith compare [3; 1; 4; 1; 5; 9] === [1; 1; 3; 4; 5; 9]
        splitAt 2 [1; 2; 3; 4] === ([1; 2], [3; 4])
        splitAt 2 [|1; 2; 3; 4|] === ([|1; 2|], [|3; 4|])
        [0..10] |> sum === 55
        [| "aa"; "bbb"; "cc" |] |> Array.sumBy (fun s -> s.Length) === 7
        [| "aa"; "bbb"; "cc" |] |> sumBy (fun s -> s.Length) === 7

        // T
        List.tail xs === z
        tail xs === z
        (fun () -> List.tail z |> ignore) |> ShouldFail
        (fun () -> tail z |> ignore) |> ShouldFail
        tail [1; 2; 3; 4] === [2; 3; 4]
        tail [|1; 2; 3; 4|] === [|2; 3; 4|]
        (fun () -> inits [] |> ignore) |> ShouldFail
        (tail (seq {1; 2; 3; 4}) |> toArray) === (seq [2; 3; 4] |> toArray)

        List.tryLast z === None

        take 3 [1; 2; 3; 4; 5] === [1; 2; 3]
        takeWhile (fun x -> x < 4) [1; 2; 3; 4; 5] === [1; 2; 3]
        tryFind (fun x -> x > 2) [1; 2; 3] === Some 3
        tryFind (fun x -> x > 2) [|1; 2; 3|] === Some 3
        tryHead [1; 2; 3] === Some 1
        tryInits [1; 2; 3; 4] === Some [1; 2; 3]
        tryInits [|1; 2; 3; 4|] === Some [|1; 2; 3|]
        tryLast [|1; 2; 3|] === Some 3
        tryPick (fun x -> if x > 1 then Some (x * 2) else None) (seq {1; 2; 3}) === Some 4
        tryTail [1; 2; 3; 4] === Some [2; 3; 4]
        tryTail [|1; 2; 3; 4|] === Some [|2; 3; 4|]
        tryTail [1] === Some z
        tryTail [] === None

        union [1; 2; 3; 4] [3; 4; 5; 6] === [1..6]
        union [|1; 2; 3; 4|] [|3; 4; 5; 6|] === [|1..6|]
        union (seq {1; 2; 3; 4}) [|3; 4; 5; 6|] |> toArray === [|1..6|]

        // W
        windowed 2 [1; 2; 3; 4] === [[1; 2]; [2; 3]; [3; 4]]
        windowed 2 [|1; 2; 3; 4|] === [|[|1; 2|]; [|2; 3|]; [|3; 4|]|]
        windowed 2 (seq {1; 2; 3; 4}) |> toArray === [|[|1; 2|]; [|2; 3|]; [|3; 4|]|]

        // Z
        zip [1; 2; 3] ['a'; 'b'; 'c'] === [(1, 'a'); (2, 'b'); (3, 'c')]
        zip ["a"; "b"] [3; 4] === [("a", 3); ("b", 4)]

        // Miscellaneous
        [|1..3|] @ [|8..10|] === [|1; 2; 3; 8; 9; 10|]

    [<Test>]
    member _.AdhocPolymorphismTupleTest() =
        (1, 2, 3, 4) |> Tuple.third === 3
        let xs = [box 1; "test"; 3.14]
        Tuple.ofSeq xs |> Tuple.item 1 === "test"
        Tuple.ofSeq xs |> Tuple.toSeq |> toList === xs
        ()



type Outer = { Id: int; Name: string }
type Inner = { OuterId: int; Value: string }
[<TestFixture>]
type GroupJoinTest() =
    let outerSeq = [ { Id = 1; Name = "Outer1" }; { Id = 2; Name = "Outer2" }; { Id = 3; Name = "Outer3" } ]
    let innerSeq = [ { OuterId = 1; Value = "Inner1" }; { OuterId = 1; Value = "Inner2" }; { OuterId = 2; Value = "Inner3" } ]
    [<Test>]
    member _.GroupJoinTest() =
        let result =
            groupJoin outerSeq innerSeq
                (_.Id)      // --> (fun outer -> outer.Id)
                (_.OuterId) // --> (fun inner -> inner.OuterId)
                (fun outer innerGroup -> outer.Name, innerGroup)
            |> map ( fun(k, vs) -> k, vs.ToFSharpList())
            |> toList

        result |> Seq.iter (fun (name, group) -> forceTrace "%s: %A" name group)
        // 결과 비교를 위한 코드
        let expectedResult =
            [
                "Outer1", [{ OuterId = 1; Value = "Inner1" }; { OuterId = 1; Value = "Inner2" }];
                "Outer2", [{ OuterId = 2; Value = "Inner3" }]
                "Outer3", []
            ]
        result === expectedResult


    [<Test>]
    member _.InnerJoinTest() =


        // InnerJoin 사용 예시
        let innerJoinResult = innerJoin outerSeq innerSeq
                                     (fun outer -> outer.Id)
                                     (fun inner -> inner.OuterId)
                                     (fun outer inner -> outer.Name, inner.Value)

        // NaturalJoin 사용 예시
        let naturalJoinResult = naturalJoin outerSeq innerSeq
                                       (fun outer inner -> outer.Id = inner.OuterId)
                                       (fun outer inner -> outer.Name, inner.Value)

        // 결과 출력
        innerJoinResult |> iter (fun (name, value) -> printfn "%s: %s" name value)
        naturalJoinResult |> iter (fun (name, value) -> printfn "%s: %s" name value)

        // 기대 결과
        let expectedInnerJoin = [ ("Outer1", "Inner1"); ("Outer1", "Inner2"); ("Outer2", "Inner3") ]
        let expectedNaturalJoin = expectedInnerJoin

        innerJoinResult |> SeqEq expectedInnerJoin
        naturalJoinResult |> SeqEq expectedNaturalJoin
