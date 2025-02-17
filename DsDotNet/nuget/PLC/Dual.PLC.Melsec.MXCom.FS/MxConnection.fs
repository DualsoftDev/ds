namespace DsMxComm

open System
open Dual.Common.Core.FS
open XGCommLib
open System.Threading
open Dual.PLC.Common.FS

[<AutoOpen>]
module DsMxConnect =

    type DsMxConnection(ip: string, port: int, onConnectChanged: ConnectChangedEventArgs -> unit) =
        let ipPort = $"{ip}:{port}"

        new (ip, onConnectChanged) = DsMxConnection(ip, 2004, onConnectChanged)

        member val CommObject: CommObject20 = null with get, set
        member val Factory: CommObjectFactory20 = null with get, set
        member x.Ip = ip
        member x.IsConnected = x.CommObject <> null && x.CommObject.IsConnected() = 1

        // 연결 상태 변경 시 콜백 호출
        member private x.TriggerConnectChanged(state: ConnectState) =
            onConnectChanged({ Ip = ip; State = state })

        member x.Connect() =
            x.Factory <- 
                let t = Type.GetTypeFromCLSID(Guid("7BBF93C0-7C64-4205-A2B0-45D4BD1F51DC"))
                Activator.CreateInstance(t) :?> CommObjectFactory20
            x.CommObject <- x.Factory.GetMLDPCommObject20(ipPort)
            if x.CommObject.Connect("") <> 1 then
                x.TriggerConnectChanged(ConnectFailed)
                failwith $"Init Connection failed: {ipPort}"
            else
                //logInfo $"Connect Success: {ipPort}"
                x.TriggerConnectChanged(Connected)
                Thread.Sleep(500)

        member x.ReConnect() =
            if x.CommObject.Connect("") = 1 then
                //logInfo $"ReConnect Success: {ipPort}"
                x.TriggerConnectChanged(Reconnected)
                Thread.Sleep(500)
            else
                //logWarn $"ReConnect failed: {ipPort}"
                x.TriggerConnectChanged(ReconnectFailed)

        member x.Disconnect() =
            if x.CommObject <> null then
                x.CommObject.Disconnect() |> ignore
                x.TriggerConnectChanged(Disconnected)

        member x.CreateDevice(deviceType: string, memType: char, size: int, offset: int) : DeviceInfo =
            let di = x.Factory.CreateDevice()
            let dev =
                if deviceType.Length = 1 then deviceType[0] 
                elif deviceType.Length = 2 && deviceType.Substring(0, 2) = "ZR" then 'R' 
                else failwithf $"Unsupported device type: {deviceType}"
            di.ucDeviceType <- Convert.ToByte(dev) 
            di.ucDataType <- byte memType
            di.lSize <- size
            di.lOffset <- offset 
            di
