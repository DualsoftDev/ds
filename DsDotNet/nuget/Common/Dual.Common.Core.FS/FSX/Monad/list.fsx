// https://youtu.be/pC4ZIeOmgB0?t=1654

type ListBuilder() =
    member _.Zero() =
        printfn "Zero"
        []
    member _.Yield(x) =
        printfn $"Yield: {x}"
        [x]

    member _.Bind(x, f) =
        printfn $"Bind x: {x}"
        List.collect f x

    member _.Return(x) =
        printfn $"Return x: {x}"
        [x]

    member _.YieldFrom(x) =
        printfn $"YieldFrom: {x}"
        x

    member _.ReturnFrom(x) = x

    //member _.Delay(f) = f()
    member _.Delay(f) =
        printfn $"Starting delay"
        let result = f()
        printfn $"Ending delay: {result}"
        result

    member _.Combine(currentValueFromYield: 'a list, accumulatorFromDelay) =
        printfn $"Combine: currentValueFromYield: {currentValueFromYield}"
        printfn $"Combine: accumulatorFromDelay: {accumulatorFromDelay}"
        currentValueFromYield @ accumulatorFromDelay

    member _.Run(valueOfLastDelay) =
        valueOfLastDelay |> List.toArray


let list = ListBuilder()

list {
    yield 1
    yield 2
    yield! [3; 4]
}


list {
    1
    2
    3
}