namespace MelsecProtocol

open Dual.PLC.Common.FS


type MxScanManager(delay: int, timeoutMs:int, isMonitorOnly: bool) =
    inherit PlcScanManagerBase<MxPlcScan>()

    override _.CreateScanner(ip: string) =
        MxPlcScan(ip, delay, timeoutMs, isMonitorOnly)
