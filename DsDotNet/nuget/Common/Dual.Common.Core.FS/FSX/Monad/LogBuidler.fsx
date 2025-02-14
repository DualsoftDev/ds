// https://www.youtube.com/watch?v=Z8G9eXnuYSk
let log p = printfn "vatue is %A" p

type LoggingBuilder() =
    member this.Bind(x, f) =
        log x
        f x
    member this.Return(x) =
        x

let logger = new LoggingBuilder()

logger {
    let! x = 3
    let! y = 2
    let! z=x+y
    return ()
}




let insertion (value, lambda) =
    log value
    lambda value

insertion(3, fun x ->
insertion(4, fun y ->
insertion(x+y, fun z ->
    ()
)))



let divideAsync num den =
    async { return num / den }
let divideByTwoAsync value = divideAsync value 2

divideByTwoAsync 5 |> Async.RunSynchronously







// https://www.youtube.com/watch?v=wtnZABezP34

let intersect (a: seq<'T>) (b: seq<'T>) : seq<'T> =
    let comparer = LanguagePrimitives. FastGenericComparer
    let aEnumerator = a.GetEnumerator()
    let bEnumerator = b.GetEnumerator()

    let rec loop (aHasValue: bool, bHasValue: bool ) =

        if aHasValue && bHasValue then
            let compareResult = comparer. Compare (aEnumerator. Current, bEnumerator. Current)

            if compareResult = 0 then
                Some (aEnumerator. Current, (aEnumerator.MoveNext(), bEnumerator.MoveNext()))

            elif compareResult < 0 then
                loop (aEnumerator. MoveNext (), true)
            else
                loop (true, bEnumerator. MoveNext ())

        else
            None

    (aEnumerator.MoveNext(), bEnumerator.MoveNext())
    |> Seq.unfold loop


    let a = [1; 2; 4; 5; 8]
    let b = [2; 3; 4; 8; 9]
    intersect a b



let intersect2 (a: seq<'T>) (b: seq<'T>) : seq<'T> =
    let comparer = LanguagePrimitives. FastGenericComparer
    let aEnumerator = a.GetEnumerator()
    let bEnumerator = b.GetEnumerator()
    seq {
        let mutable hasValue = aEnumerator.MoveNext() && bEnumerator.MoveNext()
        while hasValue do
            let compareResult = comparer.Compare (aEnumerator.Current, bEnumerator.Current)
            if compareResult = 0 then
                yield aEnumerator.Current
                hasValue <- aEnumerator.MoveNext() && bEnumerator.MoveNext()
            elif compareResult < 0 then
                hasValue <- aEnumerator.MoveNext()
            else
                hasValue <- bEnumerator.MoveNext()
    }

intersect2 a b



