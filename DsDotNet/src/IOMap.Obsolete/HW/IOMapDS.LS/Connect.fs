namespace IOMap.LS

open XGCommLib
open System
open System.Threading

[<AutoOpen>]
module Connect =

    type XGTConnection(connStr) =
      
        let (|?) = defaultArg
        let tryCnt = 3
        let rec tryFunction budget (f: 'T -> int) (arg: 'T) (name:string) : bool =
            if budget = 0 then
                failwithf $"All attempts failed: {name}"
                false
            else
                let result = f arg
                if result <> 1 then
                    Console.WriteLine $"Retrying {name} with remaining {budget}"
                    Thread.Sleep 1000
                    tryFunction (budget - 1) f arg name
                else
                    true

        member val CommObject:CommObject20 = null with get, set
        member val Factory:CommObjectFactory20 = null with get, set
        member val Delayms:int = 10 with get, set

        member x.Connect () : bool =
            x.Factory <-
                // DO NOT WORK : let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
                let t = Type.GetTypeFromCLSID(Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")) // CommObjectFactory
                Activator.CreateInstance(t) :?> CommObjectFactory20
            x.CommObject <- x.Factory.GetMLDPCommObject20(connStr)
            let isCn = tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            Thread.Sleep(1)
            isCn

        member x.CheckConnect() : bool=
            let mutable isCn = false
            if x.CommObject.IsConnected() <> 1 then
                isCn <- tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            isCn

   
        member x.Disconnect() =
            x.CommObject.Disconnect()

        member x.ReConnect() =
            x.Disconnect() |> ignore
            x.Connect()

        member x.CreateScanDevice(device:string) : DeviceInfo =
            assert(device.Length > 0)
            let di = x.Factory.CreateDevice()
            di.ucDeviceType <- Convert.ToByte(device.ToCharArray()[0])
            di.ucDataType   <- Convert.ToByte('B')
            di.lSize   <- 8 
            di.lOffset <- 0
            di

        member x.WriteRandomDevice(buf: byte array)  =
            if x.CommObject.WriteRandomDevice(buf) <> 1 
            then failwithf "WriteRandomDevice ERROR"
           
        member x.ReadRandomDevice(buf: byte array)  =
            if x.CommObject.ReadRandomDevice(buf) <> 1 
            then failwithf "ReadRandomDevice ERROR"
           

    