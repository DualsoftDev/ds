module BleedingEdgesModules

let xxx = seq {1..10} |> teeForEach (printfn "%A")
