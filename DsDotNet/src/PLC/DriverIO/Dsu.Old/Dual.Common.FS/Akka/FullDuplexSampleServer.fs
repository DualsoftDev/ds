namespace Old.Dual.Common.Akka.Sample

open System
open FSharpPlus
open Akka.Actor
open Old.Dual.Common
open Old.Dual.Common.Akka.AkkaNet

module FullDuplexSampleServerSample =
    type AmBase(id, msg:obj) =
        new () = AmBase(-1, null)
        member val Message = msg with get, set
        member val Id = id with get, set
        override x.ToString() = sprintf "Id=%d, Message=%A" x.Id x.Message
    type AmQuery(id, msg) =
        inherit AmBase(id, msg)
    type AmReply(id, msg) =
        inherit AmBase(id, msg)

    type AmRegisterClient(id, client) =
        inherit AmBase(id, client)
        member x.Client:IActorRef = client
    type AmHeartBit(id) =
        inherit AmBase(id, DateTime.Now)


    let mutable serverIp         = "127.0.0.1"
    let mutable servicePort      = 21235
    let mutable serverSystemName = "test-server-system"
    let mutable serverActorName  = "test-server-actor"

    type FullDuplexSampleServerActor(arg1, arg2) as this =
        inherit UntypedActor()

        let clients = ResizeArray<IActorRef>()
        do
            logDebug "Constructor with arguments %A, %A" arg1 arg2

            let me = this.Self
            async {
                while true do
                    do! Async.Sleep(1000)
                    for c in clients do
                        //let m = AmHeartBit(0)
                        //c.Tell(m, me)
                        AmHeartBit(0) |> c.Tell
            } |> Async.Start

        override x.OnReceive message =
            let sender = x.Sender
            let senderPath = sender.Path.ToString()
            match message with
            | :? string as m ->
                logDebug "Server actor got message %s" m
                sprintf "[%s]" m |> sender.Tell

            | :? Terminated as m ->
                //if not (senderPath.Contains("/temp/")) then
                    logWarn "Got peer (connected client:%s) terminated message." senderPath
                    clients.Remove(sender) |> ignore

            | :? AmQuery as m ->
                logDebug "Server actor got AmQuery message %A" m
                AmReply(m.Id, m.Message |>box) |> sender.Tell

            | :? AmRegisterClient as m ->
                logDebug "Server actor got AmRegisterClient message %A" m
                AmReply(m.Id, m.Message |>box) |> sender.Tell
                clients.Add(m.Client)
                UntypedActor.Context.Watch(m.Client) |> ignore
                
            | _ ->
                sprintf "[Unknown: %A]" message |> sender.Tell

        static member ServerSystemName with get() = serverSystemName and set(v) = serverSystemName <- v
        static member ServerActorName  with get() = serverActorName  and set(v) = serverActorName <- v
        static member ServerIp         with get() = serverIp         and set(v) = serverIp <- v
        static member ServicePort      with get() = servicePort      and set(v) = servicePort <- v

        static member ActorPath =
            let actorPath = getActorPath serverIp servicePort serverSystemName serverActorName
            actorPath
        static member Create() =
            let system = createRemoteServerSystem serverSystemName (Some serverIp) servicePort
            let serverActor =
                let args:obj [] = [|"consturction arg1"; 2|]    // argument passing sample
                system.ActorOf(Props(typedefof<FullDuplexSampleServerActor>, args), serverActorName)
            logDebug "Server actor created."
            serverActor


    type FullDuplexSampleClientActor(clientSystem:obj) =
        inherit UntypedActor()
        do
            logDebug "Constructor with arguments %A" clientSystem
        let clientSystem = clientSystem :?> ActorSystem
        let actorPath = FullDuplexSampleServerActor.ActorPath

        let serverActor =
            lazy
                //let actorPath =
                //    sprintf "akka.tcp://%s@%s:%d/user/%s"
                //        serverSystemName
                //        serverIp
                //        servicePort
                //        serverActorName

                let actorPath = FullDuplexSampleServerActor.ActorPath

                let actorSelection = clientSystem.ActorSelection actorPath
                let timespan = (TimeSpan.FromSeconds(10.0))
                actorSelection.ResolveOne(timespan).Result

        override x.OnReceive message =
            let sender = x.Sender
            match message with
            | :? string as m ->
                logDebug "Server actor got message %s" m
            | :? AmReply as m ->
                logDebug "Client actor got AmReply message %A" m
            | _ ->
                logDebug "[Unknown: %A]" message

        static member Create() =
            let systemName = "test-client-system"
            let clientActorName = "test-client-actor"
            let clientSystem = createRemoteClientSystem systemName None
            let clientActor =
                let args:obj [] = [|clientSystem|]    // argument passing sample
                clientSystem.ActorOf(Props(typedefof<FullDuplexSampleClientActor>, args), clientActorName)
            logDebug "Client actor created."


            ////let actorPath = getActorPath localhost samplePort "test-server-system" "test-server-actor"
            //let actorPath = "akka.tcp://test-server-system@192.168.0.2:21235/user/test-server-actor";
            //let actorSelection = clientSystem.ActorSelection actorPath
            //let timespan = (TimeSpan.FromSeconds(10.0))
            //let xxx = actorSelection.ResolveOne(timespan).Result


            clientSystem, clientActor

