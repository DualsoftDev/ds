namespace XgtProtocol

open Dual.PLC.Common.FS

type XgtScanManager(delay: int, timeoutMs:int, isMonitorOnly: bool) =
    inherit PlcScanManagerBase<XgtPlcScan>()

    override _.CreateScanner(ip: string) =
        XgtPlcScan(ip, delay, timeoutMs, isMonitorOnly)
