namespace IO.Core

open System.Threading
open Dual.Common.Core.FS
open System


[<AutoOpen>]
module Zmq =
    type ZmqInfo(iospec: IOSpec, server: ServerDirectAccess, client: Client, cts: CancellationTokenSource) =
        // 필드 정의
        member x.IOSpec = iospec
        member x.Server = server
        member x.Client = client
        member val CancellationTokenSource = cts with get, set

        // IDisposable 구현
        interface IDisposable with
            member x.Dispose() = x.Dispose()

        member x.Dispose() =
            if isNull x.CancellationTokenSource then
                failwithlog "ZmqInfo is already disposed"

            logInfo "Disposing IO hub service."

            x.CancellationTokenSource.Cancel()
            dispose server
            dispose client
            x.CancellationTokenSource <- null


    let private initialize (withClient: bool) (settingJsonPath: string) : ZmqInfo =
        let ioSpec = IOSpec.FromJsonFile settingJsonPath
        let port = ioSpec.ServicePort
        let cts = new CancellationTokenSource()

        logInfo $"Starting IO hub service: port={port}"
        let server = new ServerDirectAccess(ioSpec, cts.Token) |> tee (fun x -> x.Run() |> ignore)

        let client =
            if withClient then
                new Client($"tcp://localhost:{port}")
            else
                null

        new ZmqInfo(ioSpec, server, client, cts)

    let Initialize (settingJsonPath: string) : ZmqInfo = initialize true settingJsonPath
    let InitializeServer (settingJsonPath: string) : ZmqInfo = initialize false settingJsonPath
