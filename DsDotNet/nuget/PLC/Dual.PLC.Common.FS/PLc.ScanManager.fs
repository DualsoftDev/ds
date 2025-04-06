namespace Dual.PLC.Common.FS

open System
open System.Collections.Generic

/// 공통 ScanManager 베이스 클래스 - 태그 매핑 반환형 버전
[<AbstractClass>]
type PlcScanManagerBase<'T when 'T :> PlcScanBase>() =

    let scanners = Dictionary<string, 'T>()

    /// 스캐너 인스턴스 생성 방식은 자식이 구현
    abstract member CreateScanner: ip: string * delay: int * timeoutMs: int -> 'T

    /// 스캔 시작 + 태그 매핑 반환
    member this.StartScan(ip: string, tags: string seq, delay: int, timeoutMs:int) : IDictionary<string, IPlcTagReadWrite> =
        if not (scanners.ContainsKey(ip)) then
            let scanner = this.CreateScanner(ip, delay, timeoutMs)
            scanner.Connect()
            scanners.Add(ip, scanner)

        scanners.[ip].Scan(tags)

    /// 여러 PLC에 대해 스캔 시작 및 전체 태그 매핑 반환
    member this.StartScanAll(tagsPerPLC: IDictionary<string, string seq>, delay: int, timeoutMs:int) : IDictionary<string, IDictionary<string, IPlcTagReadWrite>> =
        let totalTags = Dictionary<string, IDictionary<string, IPlcTagReadWrite>>()
        for kv in tagsPerPLC do
            let ip = kv.Key
            let tags = kv.Value
            let mapped = this.StartScan(ip, tags, delay, timeoutMs)
            totalTags.Add(ip, mapped)
        totalTags

    /// 태그 변경 시 스캔 내용 업데이트
    member this.UpdateScan(ip: string, tags: string list) =
        match scanners.TryGetValue(ip) with
        | true, scanner -> ignore (scanner.Scan(tags))
        | _ -> failwith $"PLC IP {ip} not found in scan list."

    /// 연결 여부 확인
    member this.IsConnected(ip: string) =
        match scanners.TryGetValue(ip) with
        | true, scanner -> scanner.IsConnected
        | _ -> false

    /// 특정 스캔 중지
    member this.StopScan(ip: string) =
        match scanners.TryGetValue(ip) with
        | true, scanner ->
            scanner.Disconnect()
            scanners.Remove(ip) |> ignore
        | _ -> ()

    /// 모든 스캔 중지
    member this.StopAll() =
        for kv in scanners do
            kv.Value.Disconnect()
        scanners.Clear()

    /// 현재 스캔 중인 IP 리스트
    member this.ActiveIPs =
        scanners.Keys |> Seq.toList

    member this.GetScanner(ip: string) : 'T option =
        match scanners.TryGetValue(ip) with
        | true, scanner -> Some scanner
        | _ -> None
