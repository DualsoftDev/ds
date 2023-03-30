namespace T

open NUnit.Framework
open AddressConvert
open Engine.Common.FS
open Dsu.PLC.LS
open Xunit

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
        //word
            "P0000", "%PW0"     
            "M0001", "%MW1"
            "K0101", "%KW101"
            "F0334", "%FW334"
            "T0045", "%TW45"
            "C0001", "%CW1"
            "Z0018", "%ZW18"
            "S0017", "%SW17"     //S CANNOT USE BIT 
            "L0024", "%LW24"   
            "N0014", "%NW14"    
            "D0033", "%DW33"
        //bit
            "P00008", "%PX8"
            "M00100", "%MX160"   // 10*16 + 0
            "K0000A", "%KX10"
            "F00001", "%FX1"
            "T00008", "%TX8"
            "C0000F", "%CX15"   // 0 + 15
            "Z0010F", "%ZX175"  // 10*16 + 15
            "L0011F", "%LX191"  // 11*16 + 15
            "N0012F", "%NX207"  // 12*16 + 15
            "D0013F", "%DX223"  // 13*16 + 15

            //"D00003.F", "%DX63"   //  3*16 + 15



            //U
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
    member x.``X WriteAndRead`` () =
        //let ul0 = 0xF1F2F3F4F5F6F7F8UL
        //x.WriteFEnet("%ML1", ul0)
        //x.ReadFEnet("%ML1") === ul0

        
        for i in [0..15] do
            let mem = sprintf "%%MX%d" (10*16+i)    //MX160+i
            let memgb = sprintf "M0010%X" i         //M0010(i)
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true

        for i in [0..15] do
            let mem = sprintf "%%PX%d" (10*16+i)    //MX160+i
            let memgb = sprintf "P0010%X" i         //M0010(i)
            x.WriteFEnet(mem, true)
            x.ReadFEnet(mem) === true
            x.Read(memgb) === true
        
        let mutable w5 = 0x1234us
        x.WriteFEnet("%MW5", w5)
        x.ReadFEnet("%MW5") === w5
        x.Read("M0005") === w5
        w5 <- 0x4321us
        x.Write("M0005", w5)
        x.ReadFEnet("%MW5") === w5
        x.Read("M0005") === w5



        x.Write("M0023F", true)
        x.Read("M0023F") === true

        //x.WriteFEnet("%ML100", 123UL)
        //x.ReadFEnet("%ML100") === 123UL

        x.WriteFEnet("%MW100", 123us)
        x.ReadFEnet("%MW100") === 123us
        x.Read("%M0100") === 123us

        x.WriteFEnet("%KW100", 123us)
        x.ReadFEnet("%KW100") === 123us
        x.Read("%K0100") === 123us

        (*F 영역은 FENet 통신에서 쓰기가 불가능하다. XG5000에서는 주소 200 이상에서 가능*)
        (fun () -> x.WriteFEnet("%FW200", 123us) |> ignore) 
        |> ShouldFailWithSubstringT "LS Protocol Error with unknown code = 10x"

        x.ReadFEnet("%FW1") === 40960us
        x.Read("%F0001") === 40960us

        x.WriteFEnet("%TW100", 123us)
        x.ReadFEnet("%TW100") === 123us
        x.Read("%T0100") === 123us

        x.WriteFEnet("%CW100", 123us)
        x.ReadFEnet("%CW100") === 123us
        x.Read("%C0100") === 123us

        x.WriteFEnet("%SW3", 1us)
        x.ReadFEnet("%SW3") === 1us
        x.Read("%S0003") === 1us

    [<Test>]
    member x.``P`` () =
        (* P 영역은 write 가능한 영역과 불가능한 영역이 존재 하는 듯.. *)
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
    
    (*보류*)
    //[<Test>]
    //member x.``Write D and L word test`` () =
    //    x.WriteFEnet("%ML3", ulFF)
    //    x.ReadFEnet("%ML3") === ulFF 