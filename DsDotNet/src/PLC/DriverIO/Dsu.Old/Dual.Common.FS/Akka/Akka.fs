namespace Old.Dual.Common.Akka

open System
//open System.Net
//open Akka
open Akka.Actor
open System.Threading
open System.Runtime.CompilerServices
open Common
open Old.Dual.Common


[<Extension>] // type ActorExt =
type ActorExt =
    /// Actor 에게 Ask : 결과를 Task 로 받음
    [<Extension>]
    //static member Ask(actor:ICanTell, message, (Default None timeout:TimeSpan option)) : Tasks.Task<'t> =
    static member Ask(actor:ICanTell, message, timeout) : Tasks.Task<'t> =
        actor.Ask<'t>(message, Nullable<TimeSpan>(timeout))

    /// Actor 에게 Ask.  결과를 't 로 받음.
    [<Extension>]
    static member Inquire(actor:ICanTell, message, timeout) =
        actor.Ask(message, timeout).Result
    [<Extension>]
    static member Inquire(actor:ICanTell, message) =
        actor.Ask(message).Result

    /// Actor dispose 가능한 IDisposable 로 변환
    [<Extension>]
    static member ToDisposable(actor:IActorRef) =
        let cts = new CancellationTokenSource()

        { new IDisposable with
            member x.Dispose() = actor.Tell(PoisonPill.Instance) }


module AkkaNet =
    type IActorRef with
        /// Actor name 반환
        member x.Name = x.Path.Name
        //member x.Info = x.System, x.Path, x.System
        //member x.System = x.System x.

    /// Actor path 반환 : e.g "akka.tcp://test-server-system@192.168.0.2:12345/user/test-server-actor"
    let getActorPath server port systemName actorName =
        sprintf "akka.tcp://%s@%s:%d/user/%s" systemName server port actorName


    /// AKKA hocon (문자열) 생성.  host configuration.
    let private getDefaultHocon (host:string option) port =

        let localIp = host |? getPhysicalIPAdress()
        sprintf """
            akka {
                actor {
                    provider = "Akka.Remote.RemoteActorRefProvider, Akka.Remote"

                    # https://doc.akka.io/docs/akka/2.5/project/migration-guide-2.4.x-2.5.x.html
                    # Set this to off to disable serialization-bindings defined in
                    # additional-serialization-bindings. That should only be needed
                    # for backwards compatibility reasons.
                    #enable-additional-serialization-bindings = off
                }
                remote {
                    enabled-transports = ["akka.remote.dot-netty.tcp"]
                    dot-netty.tcp {
		                port = %d
		                hostname = %s
                    }
                }
            }
        """ port localIp

    let createAkkaSystem systemName hocon =
        let config = Akka.Configuration.ConfigurationFactory.ParseString hocon
        logDebug "SystemName:%s, Hocon=%s" systemName hocon
        ActorSystem.Create(systemName, config)

    /// TCP Server actor system 를 생성한다. : host 가 None 이면 localhost 에 해당하는 IP 주소를 자동생성한다.
    let createRemoteServerSystem systemName serverIP servicePort =
        createAkkaSystem systemName (getDefaultHocon serverIP servicePort)

    /// TCP Client actor system 를 생성한다. : host 가 None 이면 localhost 에 해당하는 IP 주소를 자동생성한다.
    let createRemoteClientSystem systemName serverIP =
        createAkkaSystem systemName (getDefaultHocon serverIP 0)

    /// Actor System 내에 Actor 를 생성한다.
    let createActor<'t> (system:ActorSystem) actorName args =
        system.ActorOf(Props(typedefof<'t>, args), actorName)


    let createChildActor<'t> (context:IUntypedActorContext) childActorName =
        context.ActorOf(Props(typedefof<'t>), childActorName)


    /// Actor path 를 이용해서 actor 를 얻는다.  C# Task 를 반환
    let getServerActorAsync (system:ActorSystem) (actorPath:string) timespan =
        let actorSelection = system.ActorSelection actorPath
        actorSelection.ResolveOne(timespan)


    /// Actor path 를 이용해서 actor 를 얻는다.  F# Async 를 반환
    let asyncGetServerActor system actorPath timespan =
        getServerActorAsync system actorPath timespan |> Async.AwaitTask

    let toDisposable (actor:IActorRef) = actor.ToDisposable()
