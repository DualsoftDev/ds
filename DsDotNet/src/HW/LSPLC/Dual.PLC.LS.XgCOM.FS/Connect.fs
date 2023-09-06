namespace DsXgComm

open Dual.Common.Core.FS
open XGCommLib
open System
open System.Threading

module Connect =
    let [<Literal>] MAX_RANDOM_READ_POINTS = 64
    let [<Literal>] MAX_ARRAY_BYTE_SIZE = 512   // 64*8
    type DsXgConnection() =
        let (|?) = defaultArg
        let tryCnt = 3
        let rec tryFunction budget (f: 'T -> int) (arg: 'T) (name:string) : bool =
            if budget = 0 then
                failwithlog $"All attempts failed: {name}"
                false
            else
                let result = f arg
                if result <> 1 then
                    logWarn $"Retrying {name} with remaining {budget}"
                    Thread.Sleep 1000
                    tryFunction (budget - 1) f arg name
                else
                    true

        member val CommObject:CommObject20 = null with get, set
        member val Factory:CommObjectFactory20 = null with get, set

        member x.Connect (connStr : string) : bool =
            x.Factory <-
                // DO NOT WORK : let t = Type.GetTypeFromProgID("XGCommLib.CommObjectFactory")
                let t = Type.GetTypeFromCLSID(Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC")) // CommObjectFactory
                Activator.CreateInstance(t) :?> CommObjectFactory20
            x.CommObject <- x.Factory.GetMLDPCommObject20(connStr)
            let isCn = tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            Thread.Sleep(500)
            //x.CommObject.WriteDevice_Bit ("M", 7, 1) |>ignore
            //Thread.Sleep(500)
            //x.CommObject.WriteDevice_Bit ("M", 7, 0) |>ignore

            isCn

        member x.CheckConnect() : bool=
            let mutable isCn = false
            if x.CommObject.IsConnected() <> 1 then
                isCn <- tryFunction tryCnt x.CommObject.Connect "" "Connecting"
            isCn

        member x.Disconnect () =
            x.CommObject.Disconnect()

        member x.WriteBit(deviceType:string, bitOffset:int, value:int) =
            x.CommObject.WriteDevice_Bit (deviceType, bitOffset, value) |>ignore

        member x.ReadBit(bstrDevice:char): byte array =
            x.CommObject.RemoveAll()
            let di = x.Factory.CreateDevice()
            di.ucDeviceType <- Convert.ToByte(bstrDevice)
            di.ucDataType <- Convert.ToByte('B')

            let rBuf = Array.zeroCreate<byte>(MAX_RANDOM_READ_POINTS)
            x.CommObject.RemoveAll()
            for i = 0 to MAX_RANDOM_READ_POINTS-1 do
                di.lSize <- 8
                di.lOffset <- i * 8
                //wBuf[i] <- byte i
                x.CommObject.AddDeviceInfo(di)
            // working : 단 random device 갯수가 64 이하 일 때...
            if x.CommObject.ReadRandomDevice rBuf <> 1 then
                            failwith "ReadRandomDevice ERROR"
            rBuf

     

        member x.CreateLWordDevice(deviceType:DeviceType, offset:int) : DeviceInfo =
            let di = x.Factory.CreateDevice()
            let dt =
                match deviceType with
                | DeviceType.M -> 'M'
                | DeviceType.I -> 'I'
                | DeviceType.Q -> 'Q'
                | DeviceType.W -> 'W'
                | DeviceType.R -> 'R'
                | _ -> failwithlog $"Unsupported device type {deviceType}"

            di.ucDeviceType <- Convert.ToByte(dt)
            di.ucDataType <- Convert.ToByte('B')
            di.lSize <- 8
            di.lOffset <- 8 * offset
            di


        [<Obsolete>]
        member private x.CreateDevice(deviceType:char, memType:char, ?size:int, ?offset:int) : DeviceInfo =
            let size = size |? 8
            let offset = offset |? 0
            let di = x.Factory.CreateDevice()
            di.ucDeviceType <- Convert.ToByte(deviceType)
            di.ucDataType <- Convert.ToByte(memType)
            di.lSize <- size
            di.lOffset <- offset
            di

        [<Obsolete>]
        member x.CreateMByteDevice(offset:int) : DeviceInfo =
            let di = x.Factory.CreateDevice()
            di.ucDeviceType <- Convert.ToByte('M')
            di.ucDataType <- Convert.ToByte('B')
            di.lSize <- 1
            di.lOffset <- offset
            di






