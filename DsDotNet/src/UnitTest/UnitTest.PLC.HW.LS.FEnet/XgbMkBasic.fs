namespace T

open NUnit.Framework
open AddressConvert
open Engine.Common.FS
open Dsu.PLC.LS
open Xunit
open System.Reactive.Linq
open Dsu.PLC.Common

[<Collection("XgbMkBasic")>]
type XgbMkBasic() =
    inherit FEnetTestBase("192.168.0.101")


    override x.CreateLsTag (tag:string) (convertFEnet:bool) =
        LsTagXgbMk(x.Conn, tag, convertFEnet)

    [<Test>]
    member x.``Connection Check`` () =
        let cpu = x.Conn.Cpu :?> LsCpu
        cpu.CpuType === CpuType.XgbMk

    [<Test>]
    member x.``Address convert test`` () =
        let tags = [
        (* word *)
            "P0000", "%PW0000"     
            "M0001", "%MW0001"
            "K0101", "%KW0101"
            "F0334", "%FW0334"
            "T0045", "%TW0045"
            "C0001", "%CW0001"
            "Z0018", "%ZW0018"
            "S0017", "%SW0017"        //S CANNOT USE BIT 
            "L0024", "%LW0024"   
            //"N0014", "%NW0014"    //5자리로 입력
            //"D0033", "%DW0033"    //5자리로 입력
            "D10033", "%DW10033"    //5자리
            "N10033", "%NW10033"    //5자리

        (* bit : word 4자리 bit한자리로 변환 *)
            "P00008", "%PX00008"
            "M01010", "%MX01010"
            "M00100", "%MX00100"
            "K0000A", "%KX0000A"
            "F00001", "%FX00001"
            "T00008", "%TX00008"
            "C0000F", "%CX0000F"       
            "Z0010F", "%ZX0010F"      
            "L0011F", "%LX0011F"      
            "N00012", "%NX00012"      
            "D00013", "%DX00013"      
            "D10013.F", "%DX10013F"     //5자리.bit 
            "N10013F", "%NX10013F"     //5자리{bit} 

        //U word & bit
            "U00.01", "%UW1"
            "U0.1", "%UW1"
            "U00000.00001", "%UW1"
            "U3.7", "%UW103"        //  3*32 + 7
            "U2.17", "%UW81"        //  2*32 + 17

            "U0.0.0", "%UX00"       //  {0*32+0}0
            "U0.0.3", "%UX03"       //  {0*32+0}3
            "U0.2.11","%UX2B"       //  {0*32+2}B
            "U1.1.1", "%UX331"      //  {1 * 32 + 1 }1

            (* 주소 개념은 있으나 XG5000에서 지원하지않음 *)
            //"D00003.F", "%DX63"   //  3*16 + 15

            (* S는XG5000에서 접근이 가능하지만 검색할 수 없다 *)

        ]
        for (tag, expected) in tags do
            let fenet = tryToFEnetTag CpuType.XgbMk tag
            fenet.Value === expected

    [<Test>]
    member x.``Invalid format test`` () =
        (x.Conn.Cpu :?> LsCpu).CpuType === CpuType.XgbMk

        (* XgbMk 에서 %MW 는 인식할 수 없어야 한다. *)
        (fun () -> x.Read("%MW5")             |> ignore ) |> ShouldFail
        (fun () -> x.ReadFEnet("M0005")       |> ignore ) |> ShouldFail
        (fun () -> x.Write("%MW5", 0us)       |> ignore ) |> ShouldFail
        (fun () -> x.WriteFEnet("M0005", 0us) |> ignore ) |> ShouldFail


    [<Test>]
    member x.``WriteAndRead`` () =
        (* Writing bit in M P K Z L D *)
        for i in [0..15] do
            let mem = sprintf "%%MX10%X" (i)
            let memgb = sprintf "M0010%X" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

        for i in [0..15] do
            let mem = sprintf "%%PX10%X" i
            let memgb = sprintf "P0010%X" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

        for i in [0..15] do
            let mem = sprintf "%%KX10%X" i
            let memgb = sprintf "K0010%X" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

        for i in [0..15] do
            let mem = sprintf "%%ZX10%X" i
            let memgb = sprintf "Z0010%X" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

        for i in [0..15] do
            let mem = sprintf "%%LX10%X" i
            let memgb = sprintf "L0010%X" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

        for i in [0..15] do
            let mem = sprintf "%%DX10%X" i
            let memgb = sprintf "D0010%X" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

        (* Writing word in M P K T C Z S L D U *)
        let mutable w5 = 0x1234us
        x.WriteFEnet("%MW5", w5)
        x.ReadFEnet("%MW5") === w5
        x.Read("M0005") === w5
        w5 <- 0x4321us
        x.Write("M0005", w5)
        x.ReadFEnet("%MW5") === w5
        x.Read("M0005") === w5

        x.WriteFEnet("%MW100", 123us)
        x.ReadFEnet("%MW100") === 123us
        x.Read("M0100") === 123us

        x.WriteFEnet("%PW100", 123us)
        x.ReadFEnet("%PW100") === 123us
        x.Read("P0100") === 123us

        x.WriteFEnet("%KW100", 123us)
        x.ReadFEnet("%KW100") === 123us
        x.Read("K0100") === 123us

        x.WriteFEnet("%TW100", 123us)
        x.ReadFEnet("%TW100") === 123us
        x.Read("T0100") === 123us

        x.WriteFEnet("%CW100", 123us)
        x.ReadFEnet("%CW100") === 123us
        x.Read("C0100") === 123us

        x.WriteFEnet("%SW3", 1us)
        x.ReadFEnet("%SW3") === 1us
        x.Read("S0003") === 1us


        x.WriteFEnet("%ZW100", 123us)
        x.ReadFEnet("%ZW100") === 123us
        x.Read("Z0100") === 123us

        x.WriteFEnet("%LW100", 123us)
        x.ReadFEnet("%LW100") === 123us
        x.Read("L0100") === 123us

        x.WriteFEnet("%DW100", 123us)
        x.ReadFEnet("%DW100") === 123us
        x.Read("D0100") === 123us



        (* F 영역은 FENet 통신에서 쓰기가 불가능하다. XG5000에서는 주소 200 이상에서 가능 *)
        (fun () -> x.WriteFEnet("%FW200", 123us) |> ignore)
        |> ShouldFailWithSubstringT "LS Protocol Error with unknown code = 10x"
        x.ReadFEnet("%FW1") === 40960us
        x.Read("F0001") === 40960us

        (* N 영역은 FENet 통신과 XG5000 모두 쓰기가 불가능하다 *)
        (fun () -> x.WriteFEnet("%NW200", 123us) |> ignore)
        |> ShouldFailWithSubstringT "LS Protocol Error with unknown code = 10x"
        x.ReadFEnet("%NW1") === 0us
        x.Read("N0001") === 0us

        //U
        x.WriteFEnet("%UW33", 65535us)
        x.ReadFEnet("%UW33") === 65535us
        x.Read("U01.01") === 65535us

        (* U0.10.0 ~ U0.10.15 -> %UX100 ~ %UX10F *)
        for i in [0..15] do
            let mem = sprintf "%%UX10%X" i
            let memgb = sprintf "U0.10.%d" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

    [<Test>]
    member x.``P`` () =
        (* P 영역은 I/Q 를 통합한 영역으로, I 영역에는 write 불가.  Q 영역은 write 가능 *)
        let bitAddress = "P0002A"
        x.Write(bitAddress, false)
        x.Read(bitAddress) === false
        x.Write(bitAddress, true)
        x.Read(bitAddress) === true



        x.WriteFEnet("%PB128", 0x63uy)
        x.ReadFEnet("%PB128") === 0x63uy

        x.WriteFEnet("%PW33", 0x33us)
        x.ReadFEnet("%PW33") === 0x33us


        x.WriteFEnet("%PW50", 0x1234us)
        x.ReadFEnet("%PW50") === 0x1234us

        let offset = 50*16+0
        let tag = sprintf "%%PX%X" offset
        x.WriteFEnet(tag, true)
        x.ReadFEnet(tag) === true
        x.WriteFEnet(tag, false)
        x.ReadFEnet(tag) === false

        noop()

    [<Test>]
    member x.``M with native`` () =
        let mutable w = 0x1234us
        x.Write("M0032", w)
        x.Read("M0032") === w
        x.ReadFEnet("%MW32") === w

        w <- 0x4321us
        x.WriteFEnet("%MW32", w)
        x.Read("M0032") === w
        x.ReadFEnet("%MW32") === w


    [<Test>]
    member x.``Add monitoring test`` () =
        let castValue (o : obj) : obj =
            match o with
            | :? bool as b -> b
            | :? uint16 as ui16 -> ui16
            | _ -> failwith "Invalid type"

        let testList : (string * obj) list = [
                        ("M00231", true);
                        ("L0102A", true);
                        ("P0102A", true);
                        ("K0100A", true);
                        ("Z0010F", true);
                        ("L0100A", true);
                        ("D0100A", true);
                        ("P0512", 32us);
                        ("M0512", 32us);
                        ("Z0012", 32us);
                        ("K0512", 32us);
                        ("T0512", 32us);
                        ("C0512", 32us);
                        ("S0012", 32us);
                        ("L0512", 32us);
                        ("D0512", 32us);
        ]
        let subscription =
            x.Conn.Subject.ToIObservable()
            |> Observable.OfType<TagValueChangedEvent>
            |> fun x -> x.Subscribe(fun evt ->      //evt.Tag.Name evt.Tag.Value
                            for (tag, value) in testList  do
                                if tag.Equals evt.Tag.Name then
                                    castValue value === evt.Tag.Value
                            logDebug "%s value Changed %A -> %A" evt.Tag.Name evt.Tag.OldValue evt.Tag.Value
                            ignore())

        for (tag, value) in testList do
            let input = castValue value
            x.Write(tag, input)
            x.Conn.AddMonitoringTag(LsTagXgi(x.Conn, tag)) |> ignore
        noop()

    [<Test>]
    member x.``X Max memory test`` () =
        (*
        P M T C     0 ~ 1023
        F           0 ~ 1023 (GX5000에서 200부터 쓰기 가능, FEnet은 불가능)
        K L         0 ~ 4095
        S Z         0 ~ 127

        5자리
        D           0 ~ 10239
        N           0 ~ 10239 (GX5000, FEnet read만 가능)
        *)
        let doInvalidRequest add =
            //System.Exception : LS Protocol Error: 각 디바이스별 지원하는 영역을 초과해서 요구한 경우
            //11x Unknown error?
            (fun () -> x.Write(add, 64us))    |> ShouldFailWithSubstringT "11x"
            (fun () -> x.Read(add) |> ignore) |> ShouldFailWithSubstringT "11x"

        let doNormalRequest add =
            //System.Exception : LS Protocol Error: 각 디바이스별 지원하는 영역을 초과해서 요구한 경우
            //11x Unknown error?
            x.Write(add, 64us)  
            x.Read(add) |> ignore

        let invalidAddresses = [
            yield! ["P"; "M"; "T"; "C"; "K"; "L"; "S"; "Z";] |> List.map (sprintf "%s9999")
        ]
        invalidAddresses |> iter doInvalidRequest

        let invalidaddressesD = [
            yield! ["D";] |> List.map (sprintf "%s99999")
        ]
        invalidaddressesD |> iter doInvalidRequest

        let invalidAddresses_nor = [
            yield! ["P"; "M"; "T"; "C"; "K"; "L"; "S"; "Z";] |> List.map (sprintf "%s0013")
        ]
        invalidAddresses_nor |> iter doNormalRequest
        let invalidaddressesD_nor = [
            yield! ["D";] |> List.map (sprintf "%s10010")
        ]
        invalidaddressesD_nor |> iter doNormalRequest
