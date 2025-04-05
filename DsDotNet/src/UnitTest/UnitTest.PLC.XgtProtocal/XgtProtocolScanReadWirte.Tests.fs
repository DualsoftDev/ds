namespace XgtProtocol.Tests

open System
open System.Collections.Generic
open System.Threading
open Xunit
open Dual.PLC.Common.FS
open XgtProtocol

module IntegrationScanTests =

    let random = Random()

    /// 태그 주소에 따라 랜덤 값을 생성하고 해당 DataType 반환
    let generateTagValue (tag: string) : obj * PlcDataSizeType =
        if tag.Contains("X") then box true, PlcDataSizeType.Boolean
        elif tag.Contains("B") then box (byte (random.Next(0, 256))), PlcDataSizeType.Byte
        elif tag.Contains("W") then box (uint16 (random.Next(0, 65536))), PlcDataSizeType.UInt16
        elif tag.Contains("D") then box (uint32 (random.Next())), PlcDataSizeType.UInt32
        elif tag.Contains("L") then box (9876543210123456789UL), PlcDataSizeType.UInt64
        else failwith $"알 수 없는 태그 타입: {tag}"

    [<Fact>]
    let ``Integration - Random Write & Read for 5 Seconds`` () =
        let scanMgr = XgtScanManager()
        let ip = "192.168.9.102"

        let tags = 
            [ "%MD100"; "%MD101"; "%ML1000"; "%ML1001" ; "%ML1002" ; "%ML1003"
              "%ML1004"; "%ML1005"; "%RL123"; "%ML0000" ]

        let result = scanMgr.StartScan(ip, tags, 20)
        Assert.True(scanMgr.GetScanner(ip).IsSome, "PLC 연결 실패")

        // 이벤트 구독
        scanMgr.GetScanner(ip).Value.TagValueChangedNotify
        |> Event.add (fun evt ->
            let tag = evt.Tag
            printfn $"[Read] {tag.Address} → {tag.Value}"
        )

        let startTime = DateTime.Now
        let duration = TimeSpan.FromSeconds(5)

        printfn "\n[✓] 랜덤 Write 테스트 시작: %O\n" startTime

        while DateTime.Now - startTime < duration do
            for kv in result do
                let tag = kv.Value
                let value, dtype = generateTagValue tag.Address
                tag.SetWriteValue(value)
                printfn $"[Write] {tag.Address} ← {value} ({dtype})"
            Thread.Sleep(100)

        scanMgr.StopScan(ip)
        printfn "\n[✓] 테스트 완료 및 스캔 중단: %O\n" DateTime.Now
