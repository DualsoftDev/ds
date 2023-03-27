namespace T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Engine.Common.FS
open System.Text.RegularExpressions
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


    //[<Test>]
    //member x.``WriteAndRead`` () =
    //    let ul0 = 0xF1F2F3F4F5F6F7F8UL
    //    x.Write("%ML1", ul0)
    //    x.Read("%ML1") === ul0
    //    noop()


