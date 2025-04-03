namespace XgtProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS
open XgtProtocol.Scan

module ScanController =

    type ScanManager(plcScanDelay: int) =
        let scans = Dictionary<string, XgtProtocalScan>()

        /// 스캔 시작
        member this.StartScan(ip: string, tags: string seq) =
            if not (scans.ContainsKey(ip)) then
                let scan = XgtProtocalScan(ip, plcScanDelay)
                if scan.Connection.Connect() |> not then
                    failwith $"PLC Connection Failed: {ip}"

                scans.Add(ip, scan)

            scans.[ip].Scan(tags)

        /// 스캔 시작 - 다중 PLC
        member this.StartScanAll(tagsPerPLC: IDictionary<string, string seq>) =
            let totalTags = Dictionary<string, IDictionary<string, XGTTag>>()   
            for kv in tagsPerPLC do
                totalTags.[kv.Key] <- this.StartScan(kv.Key, kv.Value)
            totalTags

        /// 스캔 갱신 (IP 변경 없이 태그 변경)
        member this.UpdateScan(ip: string, tags: string list) =
            if scans.ContainsKey(ip) then
                scans.[ip].ScanUpdate(tags) 
            else
                this.StartScan(ip, tags)

        /// 연결 확인
        member this.IsConnected(ip: string) =
            match scans.TryGetValue(ip) with
            | true, scan -> scan.IsConnected
            | _ -> false

        /// 스캔 중지
        member this.StopScan(ip: string) =
            match scans.TryGetValue(ip) with
            | true, scan ->
                scan.Disconnect()
                scans.Remove(ip) |> ignore
            | _ -> ()

        /// 전체 스캔 중지
        member this.StopAll() =
            for kv in scans do
                kv.Value.Disconnect()
            scans.Clear()

        /// 현재 활성 스캔 IP 리스트
        member this.ActiveIPs =
            scans.Keys |> Seq.toList

        member this.GetScanner(ip: string) = scans.[ip]
