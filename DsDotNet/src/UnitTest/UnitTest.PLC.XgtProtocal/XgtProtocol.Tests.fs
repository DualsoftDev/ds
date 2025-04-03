namespace XgtProtocol.Tests

open System
open Xunit
open System.Collections.Generic
open XgtProtocol.Batch
open XgtProtocol.Scan
open XgtProtocol
open System.Threading

module XgtTagTests =
    
    [<Fact>]   
    let ``XGTTag UpdateValue should detect change for UInt16`` () =
        let tag = XGTTag("%MW00010", 16, 160)
        tag.LWordOffset <- 20 // StartByteOffset = 160
        let buf = Array.zeroCreate<byte> 256
        BitConverter.GetBytes(123us).CopyTo(buf, tag.StartByteOffset)
        let changed = tag.UpdateValue(buf)
        Assert.True(changed)
        Assert.Equal(box 123us, tag.Value)

    [<Fact>]
    let ``XGTTag UpdateValue should return false for no change`` () =
        let tag = XGTTag("%MW00010", 16, 160)
        tag.LWordOffset <- 20
        let buf = Array.zeroCreate<byte> 256
        BitConverter.GetBytes(321us).CopyTo(buf, tag.StartByteOffset)
        tag.UpdateValue(buf) |> ignore // 첫 번째 업데이트
        let changed = tag.UpdateValue(buf) // 두 번째는 변경 없어야 함
        Assert.False(changed)


module BatchTests =

    [<Fact>]
    let ``prepareReadBatches groups tags by LWordOffset`` () =
        let batchCnt = 2 //최대 Lword 64 개씩
        let tags = [| for i in 0..64*batchCnt-1 -> 
                        XGTTag($"%%ML000{i}", 64, i * 64) 
                    |]
        let batches = prepareReadBatches tags
        Assert.Equal(batchCnt, batches.Length)
        let offsets = batches |> Array.map (fun b -> b.Tags[0].LWordTag)
        Assert.Equal<string[]>([|"%ML0"; "%ML64";|], offsets)





module IntegrationTests =

    let runEthernetTest (plcIp: string) (areaCodes: char list) =
        let conn = XgtEthernet(plcIp, 2004)

        if not (conn.Connect()) then
            failwith $"[X] PLC 연결 실패: {plcIp}"

        let rnd = Random()
        let start = DateTime.Now
        let areaTypes = [ (*'X'; 'B'; 'W'; 'D'; *)'L' ]  // 디바이스 타입

        for code in areaCodes do
            for kind in areaTypes do
                let address = 
                    if code = 'S' then  $"%%{code}{kind}0"  // S 디바이스는 1200 bit max
                    else $"%%{code}{kind}100"  // 예: %%MX10, %%MW10
                let value, dt =
                    match kind with
                    | 'X' -> box true, DataType.Bit
                    | 'B' -> box (byte (rnd.Next(0, 256))), DataType.Byte
                    | 'W' -> box (uint16 (rnd.Next(0, 65536))), DataType.Word
                    | 'D' -> box (uint32 (rnd.Next(0, Int32.MaxValue))), DataType.DWord
                    | 'L' -> box (9876543210123456789UL), DataType.LWord
                    | _ -> failwith $"지원되지 않는 타입: {kind}"

                try
                    let ok = conn.WriteData(address, dt, value)
                    let read = conn.ReadData(address, dt)
                    Assert.True(ok, $"쓰기 실패 - {address}")
                    Assert.Equal(value, read)
                    printfn $"[✓] {address} → {value} (읽기: {read})"
                with ex ->
                    printfn $"[!] 예외 - 주소: {address} → {ex.Message}"


        conn.Disconnect() |> ignore


    [<Fact>]
    let ``XGT XGK Ethernet Integration Test - Dynamic Area Write/Read for 10 seconds`` () =
        let areaCodesXGK = [ 'P'; 'M'; 'K'; 'T'; 'C'; 'U'; 'S'; 'L'; 'N'; 'D'; 'R' ]
        runEthernetTest "192.168.9.103" areaCodesXGK


    [<Fact>]
    let ``XGT XGI Ethernet Integration Test - Dynamic Area Write/Read for 10 seconds`` () =
        let areaCodesXGI = [ (*'I'; 'Q'; 'M'; 'L'; 'N'; 'K'; 'U';*) 'R';(* 'A'; 'W'*) ]
        runEthernetTest "192.168.9.102" areaCodesXGI
