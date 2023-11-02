namespace IO.Core

open System.Threading
open ZmqServerModule
open IO.Core.ZmqClient
open Dual.Common.Core.FS


[<AutoOpen>]
module Zmq =
    type ZmqInfo = {
        IOSpec:IOSpec
        Server:Server
        Client:Client
        CancellationTokenSource:CancellationTokenSource
    }
    
    let Initialize(settingJsonPath:string) : ZmqInfo =
        let ioSpec = IOSpec.FromJsonFile settingJsonPath
        let port = ioSpec.ServicePort
        let cts = new CancellationTokenSource()

        let server = new Server(ioSpec, cts.Token) |> tee (fun x -> x.Run())
        let client = new Client($"tcp://localhost:{port}")
        { IOSpec = ioSpec; Server = server; Client = client; CancellationTokenSource = cts }

