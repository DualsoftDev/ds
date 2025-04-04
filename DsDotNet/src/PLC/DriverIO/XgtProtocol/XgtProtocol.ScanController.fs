namespace XgtProtocol

open Dual.PLC.Common.FS

type XgtScanManager() =
    inherit PlcScanManagerBase<XgtPlcScan>()

    override _.CreateScanner(ip: string, delay: int) =
        XgtPlcScan(ip, delay)

type MxScanManager() = /////ahn!! 임시
    inherit PlcScanManagerBase<XgtPlcScan>()

    override _.CreateScanner(ip: string, delay: int) =
        XgtPlcScan(ip, delay)
