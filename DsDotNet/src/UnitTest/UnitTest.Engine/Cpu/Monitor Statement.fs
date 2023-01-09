namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Common.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

type Spec05_MonitorStatement() =
    do Fixtures.SetUpTest()

    [<Test>]
    member __.``M1 Origin Monitor`` () =
       
        Eq  1 1 

    [<Test>] member __.``M2 Pause Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M3 Call Error TX Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M4 Call Error RX Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M5 Real Error RX Monitor`` () =  Eq 1 1 //test ahn
    [<Test>] member __.``M6 Real Error RX Monitor`` () =  Eq 1 1 //test ahn
    
       
          