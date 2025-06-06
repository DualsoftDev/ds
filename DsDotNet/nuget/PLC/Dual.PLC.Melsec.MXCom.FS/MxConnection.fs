namespace DsMxComm

open System
open Dual.PLC.Common.FS

[<AutoOpen>]
module DsMxConnect =

  
    type DsMxConnection(logicalStationNumber: int, onConnectChanged: ConnectChangedEventArgs -> unit) =
        let plc = PlcMxComponent(logicalStationNumber)

        member val IsConnected = false with get, set
        member x.LogicalStationNumber = logicalStationNumber.ToString()

        // 연결 상태 변경 시 콜백 호출
        member private x.TriggerConnectChanged(state: ConnectState) =
            onConnectChanged({ Ip = $"Station: {x.LogicalStationNumber}"; State = state })

        member x.Connect() =
            if plc.Open() then
                x.IsConnected <- true
                x.TriggerConnectChanged(Connected)
                printfn "MX Simulator 연결 성공!"
            else
                x.TriggerConnectChanged(ConnectFailed)
                failwith $"MX Simulator 연결 실패! 오류 코드: {plc.ErrorMessage}"

        member x.ReConnect() =
            if x.IsConnected then
                x.Disconnect()
            x.Connect()

        member x.Disconnect() =
            if x.IsConnected then
                plc.Close() |> ignore
                x.IsConnected <- false
                x.TriggerConnectChanged(Disconnected)
                printfn "MX Simulator 연결 종료"

        /// 랜덤 주소 읽기
        member x.ReadDeviceRandom(deviceNames: string[]) = plc.ReadDeviceRandom(deviceNames)
        /// 랜덤 주소 쓰기
        member x.WriteDeviceRandom(deviceNames: string[], values: int16[]) = plc.WriteDeviceRandom(deviceNames, values)
