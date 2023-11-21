namespace IOMap.LS

open System
open System.Threading

[<AutoOpen>]
module Connect =

    type MNC2Connection(connStr) =
      
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

      
        member val Delayms:int = 10 with get, set

        member x.Connect () : bool =
            Thread.Sleep(200)
            //x.CommObject.Connect()

            true

        member x.CheckConnect() : bool=
            let mutable isCn = false
            //if x.CommObject.IsConnected() <> 1 then
            //    isCn <- tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            isCn

   
        member x.Disconnect() =
            //x.CommObject.Disconnect()
            true

        member x.ReConnect() =
            x.Disconnect() |> ignore
            x.Connect()

      

        member x.WriteRandomDevice(buf: byte array)  =
            //if x.CommObject.WriteRandomDevice(buf) <> 1 
            //then failwithf "WriteRandomDevice ERROR"
            ()
        member x.ReadRandomDevice(buf: byte array)  =
            //if x.CommObject.ReadRandomDevice(buf) <> 1 
            //then failwithf "ReadRandomDevice ERROR"
            ()
    