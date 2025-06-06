namespace T

open System.Reactive.Linq
open System.Collections.Generic
open NUnit.Framework
open Xunit

open AddressConvert
open Dual.Common.Core.FS
open Dsu.PLC.LS
open Dual.PLC.Common
open FSharpPlus.Data.ContT

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
            "S0017", "%SW0017"      //S CANNOT USE BIT

            //"N0014", "%NW0014"    //bit, word 판단을 위해 4자리 허용 안함
            //"D0033", "%DW0033"    //bit, word 판단을 위해 4자리 허용 안함
            //"D0033", "%DW0033"    //bit, word 판단을 위해 4자리 허용 안함
            "D01033", "%DW01033"    //5자리  올바른 표현법
            "N10033", "%NW10033"    //5자리  올바른 표현법
            "L10033", "%LW10033"    //5자리  올바른 표현법

        (* bit : word 4자리 bit한자리로 변환 *)
            "P00008", "%PX0000.8"
            "M01010", "%MX0101.0"
            "M00100", "%MX0010.0"
            "K0000A", "%KX0000.A"
            "F00001", "%FX0000.1"
            "T00008", "%TX0000.8"
            "C0000F", "%CX0000.F"
            "Z0010F", "%ZX0010.F"

            //"D010013.F", "%DX010013F"  //D bit, word 판단을 위해 {6자리word}.{bit}만 허용
            //"N0001A", "%NX00001A"      //bit, word 판단을 위해 {5자리word}{bit}만 허용
            //"N0013F", "%NX0013F"       //bit, word 판단을 위해 {5자리word}{bit}만 허용
            "D10013.F", "%DX10013.F"    //D 5자리.bit  올바른 표현법
            "N10013F",  "%NX10013.F"      //N 5자리{bit} 올바른 표현법
            "L10013F",  "%LX10013.F"      //N 5자리{bit} 올바른 표현법

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

            (*
                D 메모리 특이사항:
                    D의 bit접근법은 주소 개념은 XG5000 메인의 변수/설명 -> 디바이스 보기에서 지원
                    디바이스 모니터에서는 지원하지 않는다.

                S 메모리 특이사항:
                    S는 XG5000의 디바이스 모니터에서 0~127 단위로 접근 가능하다.
                    디바이스 모니터에서는 bit단위로 검색할 수 없다.
                    XG5000 메인의 변수/설명 -> 디바이스 보기에서 0.0 ~ 127.99범위로 찾아서 선택할 수 있다.
                    FENet 통신에서는 S0 ~ S120에 WORD단위로 쓰기가 가능하지만 BIT단위는 알 수 없음

                F, N메모리는 쓰기 접근이 불가능하다. (F는 GX5000 디바이스모니터에서 200이상부터 접근 가능하지만 권장하지 않음)
            *)

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



            (*D N L memory*)
        for i in [0..15] do
            let mem = sprintf "%%DX10%X" i
            let memgb = sprintf "D00010.%X" i
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

            (*N메모리는 읽기만 가능*)
        for i in [0..15] do
            let mem = sprintf "%%NX10%X" i
            let memgb = sprintf "N00010%X" i
            x.ReadFEnet(mem) === false
            x.Read(memgb) === false

        for i in [0..15] do
            let mem = sprintf "%%LX10%X" i
            let memgb = sprintf "L00010%X" i
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


        x.WriteFEnet("%DW100", 123us)
        x.ReadFEnet("%DW100") === 123us
        x.Read("D00100") === 123us

        x.WriteFEnet("%LW100", 123us)
        x.ReadFEnet("%LW100") === 123us
        x.Read("L00100") === 123us



        (* F 영역은 FENet 통신에서 쓰기가 불가능하다. XG5000에서는 주소 200 이상에서 가능 *)
        (fun () -> x.WriteFEnet("%FW200", 123us) |> ignore)
        |> ShouldFailWithSubstringT "LS Protocol Error with unknown code = 10x"
        x.ReadFEnet("%FW1") === 40960us
        x.Read("F0001") === 40960us

        (* N 영역은 FENet 통신과 XG5000 모두 쓰기가 불가능하다 *)
        (fun () -> x.WriteFEnet("%NW200", 123us) |> ignore)
        |> ShouldFailWithSubstringT "LS Protocol Error with unknown code = 10x"
        x.ReadFEnet("%NW15") === 0us
        x.Read("N00015") === 0us

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
    member x.``x U`` () =

        ()

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

        let testTags:HashSet<LsTagXgi> =
            let createTag (name, valueToBeWritten) =
                let tag = LsTagXgi(x.Conn, name)
                tag.UserObject <- valueToBeWritten
                tag

            let testList : (string * obj) list = [
                ("M00231",   true);
                ("L00102A",  true);
                ("P0102A",   true);
                ("K0100A",   true);
                ("Z0010F",   true);
                ("L00100A",  true);
                ("D00100.A", true);
                ("P0512",    32us);
                ("M0512",    32us);
                ("Z0012",    32us);
                ("K0512",    32us);
                ("T0512",    32us);
                ("C0512",    32us);
                ("S0012",    32us);
                ("L00512",   32us);
                ("D00512",   32us);
            ]

            testList |> map createTag |> HashSet

        let mutable finished = false
        let subscription =
            x.Conn.Subject.ToIObservable()
            |> Observable.OfType<TagValueChangedEvent>
            |> fun x -> x.Subscribe(
                fun evt ->      //evt.Tag.Name evt.Tag.Value
                    match evt.Tag with
                    | :? LsTagXgi as evTag ->
                        logDebug "%s value Changed %A -> %A" evTag.Name evt.Tag.OldValue evt.Tag.Value
                        if testTags.Contains(evTag) then
                            evTag.UserObject === evTag.Value
                            testTags.Remove(evTag) |> ignore
                            if testTags.isEmpty() then
                                finished <- true
                    | _ ->
                        () )

        for t in testTags do
            x.Conn.AddMonitoringTag(t) |> ignore

        for t in testTags do
            let input = castValue t.UserObject
            x.Write(t, input)

        while not finished do
            System.Threading.Thread.Sleep(50)
        subscription.Dispose()

        testTags.isEmpty() === true
        noop()

    [<Test>]
    member x.``Max memory test`` () =
        (*
        디바이스 모니터 기준
        P M T C     0 ~ 1023
        F           0 ~ 1023 (GX5000에서 200부터 쓰기 가능, FEnet은 불가능)
        K L         0 ~ 4095
        S Z         0 ~ 127

        5자리
        D           0 ~ 10239
        N           0 ~ 10239 (GX5000, FEnet read만 가능)
        *)
        let doInvalidRequest (addr:string) =
            //System.Exception : LS Protocol Error: 각 디바이스별 지원하는 영역을 초과해서 요구한 경우
            //11x Unknown error?
            (fun () -> x.Write(addr, 64us))    |> ShouldFailWithSubstringT "11x"
            (fun () -> x.Read(addr) |> ignore) |> ShouldFailWithSubstringT "11x"

        let doNormalRequest (addr:string) =
            x.Write(addr, 64us)
            x.Read(addr) === 64us

        let invalidAddresses = ["P"; "M"; "T"; "C"; "K"; "L"; "S"; "Z";] |> map (sprintf "%s9999")
        invalidAddresses |> iter doInvalidRequest

        let invalidaddressesD = [ "D99999" ];
        invalidaddressesD |> iter doInvalidRequest

        let invalidAddresses_nor = ["P"; "M"; "T"; "C"; "K"; "L"; "S"; "Z";] |> map (sprintf "%s0013")
        invalidAddresses_nor |> iter doNormalRequest

        let invalidaddressesD_nor = ["D";"L"] |> map (sprintf "%s00010")
        invalidaddressesD_nor |> iter doNormalRequest


    [<Test>]
    member x.``XGK Address parsing test`` () =
        let testDevice (testBitDevice:bool) (typ:DeviceType) =
            //let h = Regex(@"^%([PMLKFTCS])(\d{1,4})([\da-fA-F])$").Match("%P0A")


            let strTyp = typ.ToString()
            let qnas = [
                match typ with
                | DeviceType.D ->
                    if testBitDevice then
                        yield $"{strTyp}00000.0", $"%%{strTyp}X00000.0"
                        yield $"{strTyp}00000.1", $"%%{strTyp}X00000.1"
                        yield $"{strTyp}00000.2", $"%%{strTyp}X00000.2"
                        yield $"{strTyp}00001.0", $"%%{strTyp}X00001.0"
                        yield $"{strTyp}00001.1", $"%%{strTyp}X00001.1"
                        yield $"{strTyp}00001.2", $"%%{strTyp}X00001.2"
                        yield $"{strTyp}00011.2", $"%%{strTyp}X00011.2"
                        yield $"{strTyp}01011.2", $"%%{strTyp}X01011.2"
                        yield $"{strTyp}10011.F", $"%%{strTyp}X10011.F"

                    yield $"{strTyp}00001" , $"%%{strTyp}W00001"
                    yield $"{strTyp}00002" , $"%%{strTyp}W00002"
                    yield $"{strTyp}00003" , $"%%{strTyp}W00003"
                | DeviceType.N | DeviceType.L ->
                    if testBitDevice then
                        yield $"{strTyp}000000", $"%%{strTyp}X000000"
                        yield $"{strTyp}000001", $"%%{strTyp}X000001"
                        yield $"{strTyp}000002", $"%%{strTyp}X000002"
                        yield $"{strTyp}000010", $"%%{strTyp}X000010"
                        yield $"{strTyp}000011", $"%%{strTyp}X000011"
                        yield $"{strTyp}000012", $"%%{strTyp}X000012"
                        yield $"{strTyp}000112", $"%%{strTyp}X000112"
                        yield $"{strTyp}010112", $"%%{strTyp}X010112"
                        yield $"{strTyp}10011F", $"%%{strTyp}X10011F"

                    yield $"{strTyp}00001" , $"%%{strTyp}W00001"
                    yield $"{strTyp}00002" , $"%%{strTyp}W00002"
                    yield $"{strTyp}00003" , $"%%{strTyp}W00003"
                | _ ->
                    if testBitDevice then
                        yield $"{strTyp}00000", $"%%{strTyp}X00000"
                        yield $"{strTyp}00001", $"%%{strTyp}X00001"
                        yield $"{strTyp}00002", $"%%{strTyp}X00002"
                        yield $"{strTyp}00010", $"%%{strTyp}X00010"
                        yield $"{strTyp}00011", $"%%{strTyp}X00011"
                        yield $"{strTyp}00012", $"%%{strTyp}X00012"
                        yield $"{strTyp}00112", $"%%{strTyp}X00112"
                        yield $"{strTyp}10112", $"%%{strTyp}X10112"
                        yield $"{strTyp}1011F", $"%%{strTyp}X1011F"

                    yield $"{strTyp}0001" , $"%%{strTyp}W0001"
                    yield $"{strTyp}0002" , $"%%{strTyp}W0002"
                    yield $"{strTyp}0003" , $"%%{strTyp}W0003"
            ]

            for (tag, answer) in qnas do
                let lsTag = new LsTagXgk(x.Conn, tag)
                lsTag.FEnetTagName === answer

        let testUDeivce() =
            let qnas = [
                "U0.0",  "%UW0"
                "U0.1",  "%UW1"
                "U0.31", "%UW31"
                "U1.0",  "%UW32"
            ]
            for (tag, answer) in qnas do
                let lsTag = new LsTagXgk(x.Conn, tag)
                lsTag.FEnetTagName === answer

            let invalids = [
                //"U0.0.0"            // XG5000 UI 상에서는 지원되지 않고, FEnet 통신으로는 지원됨.
                "U0.32"             // U0.31 에서 끝나고, U1.0 으로 시작해야 함
            ]
            for tag in invalids do
                let fEnetTag = tryToFEnetTag CpuType.Xgk tag
                fEnetTag.IsNone === true

        let testBitAndWordDevice = testDevice true
        let testWordDevice = testDevice false

        let bitAndWordDeviceTypes = [ P; M; L; K; F; T; C; ]    //S Step제어용 디바이스 수집 불가
        let wordDeviceTypes = [D; R; T; C]                   // U    // '사용설명서_XGK_XGB_명령어집_국문_v2.8.pdf', pp.2-12

        bitAndWordDeviceTypes |> iter testBitAndWordDevice
        wordDeviceTypes |> iter testWordDevice

        testUDeivce()
