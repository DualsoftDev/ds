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
        let mgr = ScanManager(20)
        let result = mgr.IsConnected(ipUnknown)
        Assert.False(result)

    [<Fact>]
    let ``StopScan should clear all scans`` () =
        let mgr = ScanManager(20)
        let input =
            dict [
                ip100, seq { "%MW100" }
                ip103, seq { "%MW200" }
            ]
        mgr.StartScanAll(input) |> ignore   
        Assert.True(mgr.IsConnected(ip100))
        Assert.True(mgr.IsConnected(ip103))
        Assert.Equal(2, mgr.ActiveIPs.Length)

        mgr.StartScan(ip100, dummyTags) |> ignore   
        Assert.True(mgr.IsConnected(ip100))
        mgr.StopScan(ip100)
        Assert.False(mgr.IsConnected(ip100))
        Assert.DoesNotContain(ip100, mgr.ActiveIPs)

        mgr.StartScan(ip100, seq { "%MW100" }) |> ignore
        mgr.UpdateScan(ip100, [ "%MW100"; "%MW101" ]) |> ignore   
        Assert.True(mgr.IsConnected(ip100))

        mgr.StartScan(ip100, dummyTags) |> ignore   
        mgr.StartScan(ip103, dummyTags) |> ignore   
        mgr.StopAll()
        Assert.Empty(mgr.ActiveIPs)
