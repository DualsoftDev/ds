open Server.DsBridge

[<EntryPoint>]
let main args =
    let brg = new BridgeCommon.DsBridge()
    brg.StartUp("./config.json") |> ignore
    0