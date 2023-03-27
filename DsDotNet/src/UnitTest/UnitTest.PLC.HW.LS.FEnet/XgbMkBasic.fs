namespace T

open NUnit.Framework
open AddressConvert
open Engine.Common.FS
open Dsu.PLC.LS

type XgbMkBasic() =
    inherit FEnetTestBase("192.168.0.101")

    member private x.Write(tag, value) =
        let lsTag = LsTagXgbMk(x.Conn, tag)
        lsTag.Value <- value
        x.Conn.WriteATag(lsTag) |> ignore
    member private x.Read(tag:string) = x.Conn.ReadATag(tag)

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
        x.Write("%ML1", ul0)
        x.Read("%ML1") === ul0

        for i in [0..15] do
            x.Write( sprintf "%%MX%X" (10*16+i), true)

        noop()
    [<Test>]
    member x.``P`` () =
        (* P 영역은 write 가능한 영역과 불가능한 영역이 존재 하는 듯.. *)
        x.Write("%PB64", 0x64uy)
        x.Read("%PB64") === 0x64uy

        x.Write("%PW33", 0x33us)
        x.Read("%PW33") === 0x33us


        x.Write("%PW50", 0x1234us)
        x.Read("%PW50") === 0x1234us

        let offset = 50*16+0
        let tag = sprintf "%%PX%X" offset
        x.Write(tag, true)
        x.Read(tag) === true
        x.Write(tag, false)
        x.Read(tag) === false

        noop()



