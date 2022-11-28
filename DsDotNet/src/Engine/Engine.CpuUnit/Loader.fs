namespace Engine.Cpu

open System.Collections.Concurrent
open Engine.Cpu.FunctionImpl
open System.IO
open System.Linq
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module CpuLoader =

    ///CPU에 text 규격으로 code 불러 로딩하기
    let LoadStatements(system:DsSystem) = 
        let statements =  ConvertSystem(system)
        statements.Iter(fun f -> 
                f.ToText()  |> Console.WriteLine
                f.Do() |>ignore
                f.ToText()  |> Console.WriteLine
                ) 

        statements 

    [<EntryPoint>]        
    let main argv = 

        let s = SegmentTag<byte>.Create($"test") 
        s.End.ToText()   |> Console.WriteLine
        s.Reset.ToText() |> Console.WriteLine
        s.Start.ToText() |> Console.WriteLine
        s.Relay.ToText() |> Console.WriteLine

        s.Origin.ToText()     |> Console.WriteLine
        s.Pause .ToText()     |> Console.WriteLine
        s.ErrorTx .ToText()   |> Console.WriteLine
        s.ErrorRx .ToText()   |> Console.WriteLine

        0
