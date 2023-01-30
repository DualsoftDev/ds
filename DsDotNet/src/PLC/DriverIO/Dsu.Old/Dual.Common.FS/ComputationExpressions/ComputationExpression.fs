namespace Dsu.Common.Utilities.FS

module ComputationExpression =


    /// pp.347
    type Logging<'T> =
        | Log of 'T * list<string>

    type LoggingBuilder() =
        member x.Bind(Log(value, logs1), f) =
            let (Log(newValue, logs2)) = f(value)
            Log(newValue, logs1 @ logs2)
        member x.Return(value) =
            Log(value, [])
        member x.Zero() =
            Log((), [])

    let log = new LoggingBuilder()








    /// https://fsharpforfunandprofit.com/posts/computation-expressions-builder-part1/
    type TraceBuilder() =
        member this.Bind(m, f) = 
            match m with 
            | None -> 
                printfn "Binding with None. Exiting."
            | Some a -> 
                printfn "Binding with Some(%A). Continuing" a
            Option.bind f m

        member this.Return(x) = 
            printfn "Returning a unwrapped %A as an option" x
            Some x

        member this.ReturnFrom(m) = 
            printfn "Returning an option (%A) directly" m
            m

    // make an instance of the workflow 
    let trace = new TraceBuilder()



