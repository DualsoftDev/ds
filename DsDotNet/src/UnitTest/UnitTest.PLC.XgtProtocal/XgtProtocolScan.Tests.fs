namespace XgtProtocol.Tests

open System.Collections.Generic
open Xunit
open XgtProtocol.ScanController

module ScanManagerTests =

    let dummyTags = [ "%MW00010"; "%MW00012" ]
    let ip100 = "192.168.9.100" // PLC IP xgi EFMTB
    let ip102 = "192.168.9.102" // PLC IP xgi Local Cpu ethernet
    let ip103 = "192.168.9.103" // PLC IP xgk EFMTB
    let ipUnknown = "10.0.0.1"

    [<Fact>]
    let ``IsConnected should return false for unknown IP`` () =
        let result = ScanManager.IsConnected(ipUnknown)
        Assert.False(result)

    [<Fact>]
    let ``StopScan should clear all scans`` () =
        let input =
            dict [
                ip100, seq { "%MW100" }
                ip103, seq { "%MW200" }
            ]

        ScanManager.StartScanAll(input, 20) |> ignore   
        Assert.True(ScanManager.IsConnected(ip100))
        Assert.True(ScanManager.IsConnected(ip103))

        ScanManager.StartScan(ip100, dummyTags, 20) |> ignore   
        Assert.True(ScanManager.IsConnected(ip100))
        ScanManager.StopScan(ip100)
        Assert.False(ScanManager.IsConnected(ip100))
        Assert.DoesNotContain(ip100, ScanManager.ActiveIPs)

        ScanManager.StartScan(ip100, seq { "%MW100" }, 20) |> ignore
        ScanManager.UpdateScan(ip100, [ "%MW100"; "%MW101" ]) |> ignore   
        Assert.True(ScanManager.IsConnected(ip100))

        ScanManager.StartScan(ip100, dummyTags, 20) |> ignore   
        ScanManager.StartScan(ip103, dummyTags, 20) |> ignore   
        ScanManager.StopAll()
        Assert.Empty(ScanManager.ActiveIPs)