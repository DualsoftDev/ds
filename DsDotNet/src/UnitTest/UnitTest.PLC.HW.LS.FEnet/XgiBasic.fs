namespace T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Engine.Common.FS
open System.Text.RegularExpressions
open Dsu.PLC.LS

type XgiBasic() =
    inherit FEnetTestBase("192.168.0.100")

    /// Unsigned Long with 0xFFFFFFFFFFFFFFFF
    let ulFF = 0xFFFFFFFFFFFFFFFFUL
    /// Unsigned Int with 0xFFFFFFFF
    let unFF = 0xFFFFFFFFu
    /// Unsigned Short with 0xFFFF
    let usFF = 0xFFFFus
    /// Unsigned Byte with 0xFF
    let uyFF = 0xFFuy

    override x.CreateLsTag (tag:string) (convertFEnet:bool) =
        LsTagXgi(x.Conn, tag, convertFEnet)

    [<Test>]
    member x.``Address convert test`` () =
        let tags = [
            "%IX0.0.0", "%IX0"
            "%IX0.0.1", "%IX1"
            "%IX0.0.8", "%IX8"
            "%IX0.0.10", "%IX10"
            "%IX0.0.63", "%IX63"
            "%IX0.1.0", "%IX64"
            "%IX0.1.1", "%IX65"
            "%IX1.1.1", "%IX1089"       // 1*16*64 + 1*64 + 1
            "%IX2.3.1", "%IX2241"       // 2*16*64 + 3*64 + 1
            "%IX32.0.0", "%IX32768"

            "%IB0.0", "%IX0"
            "%IB0.1", "%IX1"
            "%IB0.2", "%IX2"
            "%IB1.0", "%IX8"
            "%IB1.1", "%IX9"

            "%IW1.0", "%IX16"
            "%IW1.1", "%IX17"

            "%ID1.0", "%IX32"
            "%ID1.1", "%IX33"

            "%IL1.0", "%IX64"
            "%IL1.1", "%IX65"

            "%IB1.0.1", "%IB129"
            "%IW1.0.1", "%IW65"
            "%ID1.0.1", "%ID33"
            //"%IL1.0.1", "%IL65"     // 존재하지 않는 주소
            "%IL1.1.0", "%IL17"

            "%QX0.0.0", "%QX0"
            "%QX0.0.1", "%QX1"
            "%QX0.0.8", "%QX8"
            "%QX0.0.10", "%QX10"
            "%QX0.0.63", "%QX63"
            "%QX0.1.0", "%QX64"
            "%QX0.1.1", "%QX65"
            "%QX1.1.1", "%QX1089"       // 1*16*64 + 1*64 + 1
            "%QX2.3.1", "%QX2241"       // 2*16*64 + 3*64 + 1
            "%QX32.0.0", "%QX32768"

            "%QB0.0", "%QX0"
            "%QB0.1", "%QX1"
            "%QB0.2", "%QX2"
            "%QB1.0", "%QX8"
            "%QB1.1", "%QX9"

            "%QW1.0", "%QX16"
            "%QW1.1", "%QX17"

            "%QD1.0", "%QX32"
            "%QD1.1", "%QX33"

            "%QL1.0", "%QX64"
            "%QL1.1", "%QX65"

            "%QB1.0.1", "%QB129"
            "%QW1.0.1", "%QW65"
            "%QD1.0.1", "%QD33"
            "%QL1.1.0", "%QL17"
        ]
        for (tag, expected) in tags do
            let fenet = tryToFEnetTag CpuType.Xgi tag
            fenet.Value === expected

    [<Test>]
    member x.``Connection Check`` () =
        let cpu = x.Conn.Cpu :?> LsCpu
        cpu.CpuType === CpuType.Xgi

    [<Test>]
    member x.``Readings`` () =
        x.Write("%ML0", ulFF)

        let mb0 = x.Conn.ReadATag("%MB0")
        mb0 === uyFF
        x.Read("%MB1") === uyFF
        x.Read("%MB7") === uyFF

        x.Read("%MW0") === usFF
        x.Read("%MW1") === usFF
        x.Read("%MW2") === usFF
        x.Read("%MW3") === usFF

        x.Read("%ML0") === ulFF

    [<Test>]
    member x.``Readings All Memory bit type`` () =
        x.Write("%ML512", ulFF)
        x.Write("%LL512", ulFF)
        x.Write("%NL512", ulFF)
        x.Write("%KL512", ulFF)
        x.Write("%RL512", ulFF)
        x.Write("%WL512", ulFF)
        x.Write("%AL512", ulFF)           //WARNING
        x.Write("%FL512", ulFF)
        x.Write("%IL512", ulFF)
        x.Write("%QL512", ulFF)
        x.Write("%UL4.0.0", ulFF)
        x.ReadFEnet("%MX32768") === true
        x.ReadFEnet("%LX32768") === true
        x.ReadFEnet("%NX32768") === true
        x.ReadFEnet("%KX32768") === true
        x.ReadFEnet("%RX32768") === true
        x.ReadFEnet("%WX32768") === true
        x.ReadFEnet("%AX32768") === true                    //WARNING
        x.ReadFEnet("%FX32768") === true
        x.ReadFEnet("%IX32768") === true                    //32.0.0
        x.ReadFEnet("%QX32768") === true                    //32.0.0
        x.Read("%UX4.0.0") === true                         //4.0.0
        x.Read("%IX32.0.0") === true                        //32.0.0
        x.Read("%QX32.0.0") === true                        //32.0.0

        (*U메모리는 0.0.0 양식으로만 접근 가능*)
        (tryParseTag "%UX32768").IsNone === true


    [<Test>]
    member x.``Readings All Memory byte type`` () =
        x.Write("%ML512", ulFF)
        x.Write("%LL512", ulFF)
        x.Write("%NL512", ulFF)
        x.Write("%KL512", ulFF)
        x.Write("%RL512", ulFF)
        x.Write("%WL512", ulFF)
        x.Write("%AL512", ulFF)           //WARNING
        x.Write("%FL512", ulFF)
        x.Write("%IL512", ulFF)
        x.Write("%QL512", ulFF)
        x.Write("%UL4.0.0", ulFF)
        x.ReadFEnet("%MB4096") === uyFF
        x.ReadFEnet("%LB4096") === uyFF
        x.ReadFEnet("%NB4096") === uyFF
        x.ReadFEnet("%KB4096") === uyFF
        x.ReadFEnet("%RB4096") === uyFF
        x.ReadFEnet("%WB4096") === uyFF
        x.ReadFEnet("%AB4096") === uyFF                   //WARNING
        x.ReadFEnet("%FB4096") === uyFF
        x.ReadFEnet("%IB4096") === uyFF                    //32.0.0
        x.ReadFEnet("%QB4096") === uyFF                    //32.0.0
        x.Read("%UB4.0.0") === uyFF                       //4.0.0
        x.Read("%IB32.0.0") === uyFF                      //32.0.0
        x.Read("%QB32.0.0") === uyFF                      //32.0.0

        (*U메모리는 0.0.0 양식으로만 접근 가능*)
        (tryParseTag "%UB4096").IsNone === true

    [<Test>]
    member x.``Readings All Memory word type`` () =
        x.Write("%ML512", ulFF)
        x.Write("%LL512", ulFF)
        x.Write("%NL512", ulFF)
        x.Write("%KL512", ulFF)
        x.Write("%RL512", ulFF)
        x.Write("%WL512", ulFF)
        x.Write("%AL512", ulFF)           //WARNING
        x.Write("%FL512", ulFF)
        x.Write("%IL512", ulFF)
        x.Write("%QL512", ulFF)
        x.Write("%UL4.0.0", ulFF)
        x.ReadFEnet("%MW2048") === usFF
        x.ReadFEnet("%LW2048") === usFF
        x.ReadFEnet("%NW2048") === usFF
        x.ReadFEnet("%KW2048") === usFF
        x.ReadFEnet("%RW2048") === usFF
        x.ReadFEnet("%WW2048") === usFF
        x.ReadFEnet("%AW2048") === usFF                 //WARNING
        x.ReadFEnet("%FW2048") === usFF
        x.ReadFEnet("%IW2048") === usFF                 //32.0.0
        x.ReadFEnet("%QW2048") === usFF                 //32.0.0
        x.Read("%UW4.0.0") === usFF                     //4.0.0
        x.Read("%IW32.0.0") === usFF                    //32.0.0
        x.Read("%QW32.0.0") === usFF                    //32.0.0

        (*U메모리는 0.0.0 양식으로만 접근 가능*)
        (tryParseTag "%UW2048").IsNone === true

    [<Test>]
    member x.``Readings All Memory Double word type`` () =
        x.Write("%ML512", ulFF)
        x.Write("%LL512", ulFF)
        x.Write("%NL512", ulFF)
        x.Write("%KL512", ulFF)
        x.Write("%RL512", ulFF)
        x.Write("%WL512", ulFF)
        x.Write("%AL512", ulFF)           //WARNING
        x.Write("%FL512", ulFF)
        x.Write("%IL512", ulFF)
        x.Write("%QL512", ulFF)
        x.Write("%UL4.0.0", ulFF)
        x.ReadFEnet("%MD1024") === unFF
        x.ReadFEnet("%LD1024") === unFF
        x.ReadFEnet("%ND1024") === unFF
        x.ReadFEnet("%KD1024") === unFF
        x.ReadFEnet("%RD1024") === unFF
        x.ReadFEnet("%WD1024") === unFF
        x.ReadFEnet("%AD1024") === unFF              //WARNING
        x.ReadFEnet("%FD1024") === unFF
        x.ReadFEnet("%ID1024") === unFF              //32.0.0
        x.ReadFEnet("%QD1024") === unFF              //32.0.0
        x.Read("%ID32.0.0") === unFF                 //32.0.0
        x.Read("%QD32.0.0") === unFF                 //32.0.0

        (* U메모리는 0.0.0 양식으로만 접근 가능 *)
        (tryParseTag "%UD1024").IsNone === true

    [<Test>]
    member x.``Readings All Memory Long word type`` () =
        x.Write("%ML512", ulFF)
        x.Write("%LL512", ulFF)
        x.Write("%NL512", ulFF)
        x.Write("%KL512", ulFF)
        x.Write("%RL512", ulFF)
        x.Write("%WL512", ulFF)
        x.Write("%AL512", ulFF)           //WARNING
        x.Write("%FL512", ulFF)
        x.Write("%IL512", ulFF)
        x.Write("%QL512", ulFF)
        x.Write("%UL4.0.0", ulFF)
        x.ReadFEnet("%ML512") === ulFF
        x.ReadFEnet("%LL512") === ulFF
        x.ReadFEnet("%NL512") === ulFF
        x.ReadFEnet("%KL512") === ulFF
        x.ReadFEnet("%RL512") === ulFF
        x.ReadFEnet("%WL512") === ulFF
        x.ReadFEnet("%AL512") === ulFF              //WARNING
        x.ReadFEnet("%FL512") === ulFF
        x.ReadFEnet("%IL512") === ulFF              //32.0.0
        x.ReadFEnet("%QL512") === ulFF              //32.0.0
        x.Read("%UL4.0.0") === ulFF                 //4.0.0
        x.Read("%IL32.0.0") === ulFF                //32.0.0
        x.Read("%QL32.0.0") === ulFF                //32.0.0

        (*U메모리는 0.0.0 양식으로만 접근 가능*)
        (tryParseTag "%UL512").IsNone === true


    [<Test>]
    member x.``WriteAndRead`` () =
        let ul0 = 0xF1F2F3F4F5F6F7F8UL
        x.WriteFEnet("%ML1", ul0)
        x.Read("%ML1") === ul0


        x.Write("%ML1", ul0)
        x.Read("%ML1") === ul0
        noop()


    [<Test>]
    member x.``Q`` () =
        let q0 = "%QX0.0.0"
        x.Write(q0, true)
        x.Read(q0) === true
        x.Write(q0, false)
        x.Read(q0) === false

        let q1 = "%QX0.0.1"
        x.Write(q1, true)
        x.Read(q1) === true
        x.Write(q1, false)
        x.Read(q1) === false

        let q11 = "%QX0.1.1"
        x.Write(q11, true)
        x.Read(q11) === true
        x.ReadFEnet("%QX65") === true
        x.Write(q11, false)
        x.Read(q11) === false
        x.ReadFEnet("%QX65") === false

        let q111 = "%QX1.1.1"       // = 1089 : 1*16*64 + 1*64 + 1
        x.Write(q111, true)
        x.Read(q111) === true
        x.ReadFEnet("%QX1089") === true
        x.Write(q111, false)
        x.Read(q111) === false
        x.ReadFEnet("%QX1089") === false
