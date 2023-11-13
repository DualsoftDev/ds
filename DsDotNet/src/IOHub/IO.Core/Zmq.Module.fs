namespace IO.Core

open System.Threading
open Dual.Common.Core.FS


[<AutoOpen>]
module Zmq =
    type ZmqInfo =
        { IOSpec: IOSpec
          Server: ServerDirectAccess
          Client: Client
          CancellationTokenSource: CancellationTokenSource }

    let private initialize (withClient: bool) (settingJsonPath: string) : ZmqInfo =
        let ioSpec = IOSpec.FromJsonFile settingJsonPath
        let port = ioSpec.ServicePort
        let cts = new CancellationTokenSource()

        let server = new ServerDirectAccess(ioSpec, cts.Token) |> tee (fun x -> x.Run())

        let client =
            if withClient then
                new Client($"tcp://localhost:{port}")
            else
                null

        { IOSpec = ioSpec
          Server = server
          Client = client
          CancellationTokenSource = cts }

    let Initialize (settingJsonPath: string) : ZmqInfo = initialize true settingJsonPath
    let InitializeServer (settingJsonPath: string) : ZmqInfo = initialize false settingJsonPath
