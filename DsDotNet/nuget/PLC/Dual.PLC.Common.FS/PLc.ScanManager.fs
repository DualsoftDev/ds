namespace Dual.PLC.Common.FS

open System
open System.Collections.Generic

/// 공통 ScanManager 베이스 클래스
[<AbstractClass>]
type PlcScanManagerBase<'T when 'T :> PlcScanBase>() =

    let scanners = Dictionary<string, 'T>()

    /// 스캐너 인스턴스 생성 방식 (자식 클래스에서 정의)
    abstract member CreateScanner: ip: string -> 'T

    /// 스캔 시작 - 스캐너가 없으면 생성 및 등록 후 Scan 수행
    member this.StartScan(ip: string, tags: seq<TagInfo>) : IDictionary<ScanAddress, PlcTagBase> =
        let scanner =
            match scanners.TryGetValue(ip) with
            | true, existing -> existing
            | false, _ ->
                let newScanner = this.CreateScanner(ip)
                newScanner.Connect()
                scanners.Add(ip, newScanner)
                newScanner

        scanner.Scan(tags)

    member this.StartScanReadOnly(ip: string, tags: seq<string>) : IDictionary<string, PlcTagBase> =
        let scanTags = tags |> Seq.map (fun x -> { Name = x; Address = x; Comment = "" ;IsOutput = false })
        this.StartScan(ip, scanTags) 

    /// 여러 PLC에 대해 스캔 시작 - IP 별 태그 목록 입력
    member this.StartScanAll(tagsPerPLC: IDictionary<string, seq<TagInfo>>) : IDictionary<string, IDictionary<string, PlcTagBase>> =
        tagsPerPLC
        |> Seq.map (fun kvp -> kvp.Key, this.StartScan(kvp.Key, kvp.Value))
        |> dict

    /// 기존 스캐너에 태그 목록 변경 적용 (IP 기반)
    member this.UpdateScan(ip: string, tags: list<TagInfo>) =
        match scanners.TryGetValue(ip) with
        | true, scanner -> ignore (scanner.Scan(tags))
        | _ -> failwith $"[UpdateScan] IP {ip}에 대한 스캐너가 존재하지 않습니다."

    member this.UpdateScanReadOnly(ip: string, tags: seq<string>)  =
        let scanTags = tags |> Seq.map (fun x -> { Name = x; Address = x; Comment = "" ;IsOutput = false }) |> Seq.toList
        this.UpdateScan(ip, scanTags)

    /// 현재 연결 상태 확인
    member this.IsConnected(ip: string) : bool =
        match scanners.TryGetValue(ip) with
        | true, scanner -> scanner.IsConnected
        | _ -> false

    /// 특정 IP의 스캐너 연결 종료 및 제거
    member this.StopScan(ip: string) =
        match scanners.TryGetValue(ip) with
        | true, scanner ->
            scanner.StopScan()  
            scanners.Remove(ip) |> ignore
        | _ -> ()

    /// 모든 PLC 스캔 정지 및 스캐너 초기화
    member this.StopAll() =
        scanners.Keys |> Seq.iter (fun ip -> this.StopScan(ip))
        scanners.Clear()

    /// 현재 스캔 중인 모든 IP 리스트 반환
    member this.ActiveIPs : string list =
        scanners.Keys |> Seq.toList

    /// 특정 IP의 스캐너 인스턴스를 반환 (option 타입)
    member this.GetScanner(ip: string) : 'T option =
        match scanners.TryGetValue(ip) with
        | true, scanner -> Some scanner
        | _ -> None
