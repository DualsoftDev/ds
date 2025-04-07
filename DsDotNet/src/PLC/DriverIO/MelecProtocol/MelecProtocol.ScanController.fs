namespace MelsecProtocol

open Dual.PLC.Common.FS


type MxScanManager() = /////ahn!! 임시
    inherit PlcScanManagerBase<XgtPlcScan>()

    override _.CreateScanner(ip: string, delay: int, timeoutMs:int) =
        XgtPlcScan(ip, delay, timeoutMs)
