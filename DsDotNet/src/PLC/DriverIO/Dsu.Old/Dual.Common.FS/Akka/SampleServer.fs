namespace Old.Dual.Common.Akka.Sample

open System
open Akka.Actor
open System.Threading
open Old.Dual.Common
open Old.Dual.Common.Akka
open Old.Dual.Common.Akka.AkkaNet

module Sample =
    let private localhost = "192.168.0.2"
    let private samplePort = 21235

    type SampleServerActor(arg1, arg2) =
        inherit UntypedActor()
        do
            logDebug "Constructor with arguments %A, %A" arg1 arg2

        override x.OnReceive message =
            let sender = x.Sender
            match message with
            | :? string as m ->
                logDebug "Server actor got message %s" m
                sprintf "[%s]" m |> sender.Tell
            | _ -> sprintf "[Unknown: %A]" message |> sender.Tell

        static member Create() =
            let systemName = "test-server-system"
            let serverActorName = "test-server-actor"
            let system = createRemoteServerSystem systemName (Some localhost) samplePort
            let serverActor =
                let args:obj [] = [|"consturction arg1"; 2|]    // argument passing sample
                system.ActorOf(Props(typedefof<SampleServerActor>, args), serverActorName)
            logDebug "Server actor created."
            serverActor


    let connectServer() =
        let timespan = (TimeSpan.FromSeconds(10.0))
        async {
            let system = createRemoteClientSystem "testSystem" None
            let! serverActor =
                let actorPath = getActorPath localhost samplePort "test-server-system" "test-server-actor"
                logDebug "ActorPath = %s" actorPath

                asyncGetServerActor system actorPath timespan
            return (system, serverActor)
        } |> Async.RunSynchronously


    let testClient() =
        let _, serverActor = connectServer()
        let q msg = serverActor.Inquire(msg)

        let response = q "hello"
        logDebug "%A" response

        printf "Hit any key to stop server:"
        System.Console.ReadLine()
        |> q
        |> logDebug "Got message=%A"

        System.Console.ReadKey() |> ignore

        logDebug "Succeeded"
