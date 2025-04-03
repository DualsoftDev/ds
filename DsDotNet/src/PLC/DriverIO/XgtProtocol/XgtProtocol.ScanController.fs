namespace XgtProtocol

open System
open System.Collections.Generic
open Dual.PLC.Common.FS
open System.Net.Sockets
open System.Text
open XgtProtocol.Scan

module ScanController =

    type ScanManager private () =
        static let scans = Dictionary<string, XgtProtocalScan>()

        static member StartScan(ip: string, tags: string seq, delay:int) =
            if not (scans.ContainsKey(ip)) then
                let scan = XgtProtocalScan(ip, delay)
                if scan.Connection.Connect() |> not then
                    failwith $"PLC Connection Failed: {ip}"

                scans.Add(ip, scan)

            scans.[ip].Scan(tags)

        static member StartScanAll(tagsPerPLC: IDictionary<string, string seq>, delay:int) =
            let totalTags = Dictionary<string, IDictionary<string, ITagPLC>>()
            for kv in tagsPerPLC do
                totalTags.[kv.Key] <- ScanManager.StartScan(kv.Key, kv.Value, delay)
            totalTags

        static member UpdateScan(ip: string, tags: string list) =
            if scans.ContainsKey(ip) then
                scans.[ip].ScanUpdate(tags)
            else
                failwith $"PLC IP {ip} not found in scan list."

        static member IsConnected(ip: string) =
            match scans.TryGetValue(ip) with
            | true, scan -> scan.IsConnected
            | _ -> false

        static member StopScan(ip: string) =
            match scans.TryGetValue(ip) with
            | true, scan ->
                scan.Disconnect()
                scans.Remove(ip) |> ignore
            | _ -> ()

        static member StopAll() =
            for kv in scans do
                kv.Value.Disconnect()
            scans.Clear()

        static member ActiveIPs =
            scans.Keys |> Seq.toList

        static member GetScanner(ip: string) =
            match scans.TryGetValue(ip) with
            | true, scan -> scan
            | _ -> Unchecked.defaultof<XgtProtocalScan>