namespace T

open NUnit.Framework
open AddressConvert
open Engine.Common.FS
open Dsu.PLC.LS

type XgbMkBasic() =
    inherit FEnetTestBase("192.168.0.101")

    override x.CreateLsTag (tag:string) (convertFEnet:bool) =
        LsTagXgbMk(x.Conn, tag, convertFEnet)
    //member private x.WriteTagValue(tag, value, convertFEnet) =
    //    let lsTag = LsTagXgbMk(x.Conn, tag, convertFEnet)
    //    lsTag.Value <- value
    //    x.Conn.WriteATag(lsTag) |> ignore
    //member private x.Write(tag, value) = x.WriteTagValue(tag, value, true)
    //member private x.WriteFEnet(tag, value) = x.WriteTagValue(tag, value, false)
    //member private x.Read(tag:string) = x.Conn.ReadATag(tag)
    //member private x.ReadFEnet(tag:string) = x.Conn.ReadATagFEnet(tag)

    [<Test>]
    member x.``Connection Check`` () =
        let cpu = x.Conn.Cpu :?> LsCpu
        cpu.CpuType === CpuType.XgbMk

    [<Test>]
    member x.``Address convert test`` () =
        let tags = [
            "P0000", "%PW0"
            "P0001", "%PW1"
            "P0101", "%PW101"
            "P00001", "%PX1"
            "P00008", "%PX8"
            "P0000F", "%PX15"
            "P0001F", "%PX31"   // 1*16 + 15
            "P0011F", "%PX191"  // 11*16 + 15
        ]
        for (tag, expected) in tags do
            let fenet = tryToFEnetTag CpuType.XgbMk tag
            fenet.Value === expected

    //[<Test>]
    //member x.``Readings`` () =
    //    (* PLC 에서 %ML0 를 FF 값으로 채우고 있다는 가정하에... *)
    //    let mb0 = x.Conn.ReadATag("%MB0")
    //    mb0 === 0xFFuy
    //    x.Read("%MB1") === 0xFFuy
    //    x.Read("%MB7") === 0xFFuy

    //    x.Read("%MW0") === 0xFFFFus
    //    x.Read("%MW1") === 0xFFFFus
    //    x.Read("%MW2") === 0xFFFFus
    //    x.Read("%MW3") === 0xFFFFus

    //    x.Read("%ML0") === 0xFFFFFFFFFFFFFFFFUL


    [<Test>]
    member x.``WriteAndRead`` () =
        let ul0 = 0xF1F2F3F4F5F6F7F8UL
        x.WriteFEnet("%ML1", ul0)
        x.ReadFEnet("%ML1") === ul0

        for i in [0..15] do
            x.WriteFEnet( sprintf "%%MX%X" (10*16+i), true)

        let mutable w5 = 0x1234us
        x.WriteFEnet("%MW5", w5)
        x.ReadFEnet("%MW5") === w5
        x.Read("M0005") === w5
        w5 <- 0x4321us
        x.Write("M0005", w5)
        x.ReadFEnet("%MW5") === w5
        x.Read("M0005") === w5

        (* XgbMk 에서 %MW 는 인식할 수 없어야 한다. *)
        (fun () -> x.Read("%MW5") |> ignore ) |> ShouldFail
        (fun () -> x.ReadFEnet("M0005") |> ignore ) |> ShouldFail


        noop()
    [<Test>]
    member x.``P`` () =
        (* P 영역은 write 가능한 영역과 불가능한 영역이 존재 하는 듯.. *)
        x.WriteFEnet("%PB64", 0x64uy)
        x.ReadFEnet("%PB64") === 0x64uy

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
        x.Write("M0032", 0xFFFFus)
        x.Read("M0032") === 0xFFFFus
        x.ReadFEnet("%MW32") === 0xFFFFus


