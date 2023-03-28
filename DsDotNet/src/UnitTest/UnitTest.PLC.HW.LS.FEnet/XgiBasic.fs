namespace T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Engine.Common.FS
open System.Text.RegularExpressions
open Dsu.PLC.LS

type XgiBasic() =
    inherit FEnetTestBase("192.168.0.111")

    override x.CreateLsTag (tag:string) (convertFEnet:bool) =
        LsTagXgi(x.Conn, tag, convertFEnet)

    [<Test>]
    member x.``Connection Check`` () =
        let cpu = x.Conn.Cpu :?> LsCpu
        cpu.CpuType === CpuType.Xgi

    [<Test>]
    member x.``Readings`` () =
        (* PLC 에서 %ML0 를 FF 값으로 채우고 있다는 가정하에... *)
        let mb0 = x.Conn.ReadATag("%MB0")
        mb0 === 0xFFuy
        x.Read("%MB1") === 0xFFuy
        x.Read("%MB7") === 0xFFuy

        x.Read("%MW0") === 0xFFFFus
        x.Read("%MW1") === 0xFFFFus
        x.Read("%MW2") === 0xFFFFus
        x.Read("%MW3") === 0xFFFFus

        x.Read("%ML0") === 0xFFFFFFFFFFFFFFFFUL


    [<Test>]
    member x.``WriteAndRead`` () =
        let ul0 = 0xF1F2F3F4F5F6F7F8UL
        x.WriteFEnet("%ML1", ul0)
        x.Read("%ML1") === ul0


        x.Write("%ML1", ul0)
        x.Read("%ML1") === ul0
        noop()


