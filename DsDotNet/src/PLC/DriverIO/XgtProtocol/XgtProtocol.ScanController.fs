namespace XgtProtocol

open Dual.PLC.Common.FS

type XgtScanManager() =
    inherit PlcScanManagerBase<XgtPlcScan>()

    override _.CreateScanner(ip: string, delay: int, timeoutMs:int) =
        XgtPlcScan(ip, delay, timeoutMs)

type MxScanManager() = /////ahn!! 임시
    inherit PlcScanManagerBase<XgtPlcScan>()

    override _.CreateScanner(ip: string, delay: int, timeoutMs:int) =
        XgtPlcScan(ip, delay, timeoutMs)
