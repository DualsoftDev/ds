namespace XgtProtocol

open Dual.PLC.Common.FS

type XgtScanManager(localEthernet:bool, delay: int, timeoutMs:int, isMonitorOnly: bool) =
    inherit PlcScanManagerBase<XgtPlcScan>()

    override _.CreateScanner(ip: string) =
        XgtPlcScan(ip, localEthernet, delay, timeoutMs, isMonitorOnly)
