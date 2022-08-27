namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Disposables
open System.Threading
open System.Reactive.Linq

open Engine.Common.FS
open Engine.Core
open Engine.OPC

//[<AutoOpen>]
//module CpuModule =
//    let runCpu(cpu:Cpu) =
//    let agent =
//        MailboxProcessor<BitChange[]>.Start(fun inbox ->
//            let rec loop() =
//                async {
//                    let! msg = inbox.Receive()

//                    return! loop()
//                } 
//            loop()
//        )

