namespace XgtProtocol.Tests

open System
open System.Collections.Generic
open Xunit
open XgtProtocol.ScanController
open Dual.PLC.Common.FS
open System.Threading

module IntegrationScanTests =

    let random = Random()

    /// 태그 주소에 따라 랜덤 값을 생성하고 해당 DataType 반환
    let generateTagValue (tag: string) : obj * DataType =
        if tag.Contains("X") then box true, DataType.Bit
        elif tag.Contains("B") then box (byte (random.Next(0, 256))), DataType.Byte
        elif tag.Contains("W") then box (uint16 (random.Next(0, 65536))), DataType.Word
        elif tag.Contains("D") then box (uint32 (random.Next())), DataType.DWord
        elif tag.Contains("L") then box (9876543210123456789UL), DataType.LWord
        else failwith $"알 수 없는 태그 타입: {tag}"

    [<Fact>]
    let ``ScanManager Integration - Random Write & Read for 5 Seconds`` () =
        let ip = "192.168.9.102"
        let tags = [ "%MD100"; "%MD101"; "%ML1000"; "%ML1001" ; "%ML1002" ; "%ML1003" ; "%ML1004" ; "%ML1005" ;"%RL123"; "%ML0000"; ]
        //let tags = [ "%RL123"; "%ML0000"; ]
        //let tags = 
        //    [ 
        //        "%MX0"; "%MX1"; "%MX2"; 
        //      ]
        
        
        
        let result = ScanManager.StartScan(ip, tags, 20)
        Assert.True(ScanManager.IsConnected(ip), "PLC 연결 실패")
        ScanManager.GetScanner(ip).TagValueChangedNotify
           .Subscribe(fun evt ->
                let tag = evt.Tag
                let value = tag.Value
                printfn $"[Read] {tag.Address} → {value})"
            ) |> ignore

        let startTime = DateTime.Now
        let duration = TimeSpan.FromSeconds(2)

        printfn "\n[✓] 랜덤 Write 테스트 시작: %O\n" startTime

        while DateTime.Now - startTime < duration do
            for kv in result do
                let tag = kv.Value
                let value, dtype = generateTagValue tag.Address
                tag.SetWriteValue(value)

                printfn $"[Write] {tag.Address} ← {value} ({dtype})"
            
           // Thread.Sleep(101)

        ScanManager.StopScan(ip)
        printfn "\n[✓] 테스트 완료 및 스캔 중단: %O\n" DateTime.Now
