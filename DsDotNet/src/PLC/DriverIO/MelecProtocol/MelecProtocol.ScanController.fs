namespace MelsecProtocol

open Dual.PLC.Common.FS


type MxScanManager(delay: int, timeoutMs:int, isMonitorOnly: bool, port, isUDP) =
    inherit PlcScanManagerBase<MxPlcScan>()

    override _.CreateScanner(ip: string) =
        MxPlcScan(ip, port, isUDP, delay, timeoutMs, isMonitorOnly)
