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
        
        statements 

    [<EntryPoint>]        
    let main argv = 

        let s = DsMemory($"test") 
        s.End.ToText()   |> Console.WriteLine
        s.Reset.ToText() |> Console.WriteLine
        s.Start.ToText() |> Console.WriteLine
        s.Relay.ToText() |> Console.WriteLine

        s.Origin.ToText()     |> Console.WriteLine
        s.Pause .ToText()     |> Console.WriteLine
        s.ErrorTx .ToText()   |> Console.WriteLine
        s.ErrorRx .ToText()   |> Console.WriteLine


        let expr = add [10 ;12 ]
        let t1 = PlcTag.Create("t1", 1)
        let t2 = PlcTag.Create("t2", 1)
        let target = PlcTag.Create("target", 1)

        let expr = mul [  //  2
                          //  expr
                          //  add [t1; t2] 
                            2
                            3
                            add [1; 5] 
                ] 
        let a = expr |> evaluate

        let a = expr.ToJsonText()
        let stmt = Assign (expr, target)
        let a = stmt.ToJsonText()
        let b = a.ToStatement()
        let c = b.ToJsonText()
        

        0
