namespace T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Engine.Common.FS
open System.Text.RegularExpressions
open Dsu.PLC.LS

type XgiBasic() =
    inherit FEnetTestBase("192.168.0.100")

    let LAddress = 512      // 모든 L주소 512에 최대값 넣기(A 제외)
    let DAddress = LAddress * 2
    let WAddress = DAddress * 2
    let BAddress = WAddress * 2
    let bitAddress = BAddress * 8


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
            "%IX1.1.1", "%IX4161"       // 1*64*64 + 1*64 + 1
            "%IX2.3.1", "%IX8385"       // 2*64*64 + 3*64 + 1

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

            "%IB1.0.1", "%IB65"
            "%IW1.0.1", "%IW65"
            "%ID1.0.1", "%ID65"
            "%IL1.0.1", "%IL65"
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
    member x.``Readings All Memory bit type`` () =
        (* PLC 에서 %_X11 을 FF 값으로 채우고 테스트. 단, A는 0으로 고정됨 *)
        let memoryType = "X"
        let address = bitAddress
                    |>toString
        let answer = true
        x.Read("%M"+memoryType+address) === answer
        x.Read("%L"+memoryType+address) === answer
        x.Read("%N"+memoryType+address) === answer
        x.Read("%K"+memoryType+address) === answer
        x.Read("%R"+memoryType+address) === answer
        x.Read("%W"+memoryType+address) === answer
        x.Read("%A"+memoryType+address) =!= answer
        x.Read("%F"+memoryType+address) === answer 
        x.Read("%U"+memoryType+address) === answer          
        //x.Read("%I"+memoryType+address) === answer   
        //x.Read("%Q"+memoryType+address) === answer 

    [<Test>]
    member x.``Readings All Memory byte type`` () =
        (* PLC 에서 %_B11 을 FF 값으로 채우고 테스트. 단, A는 0으로 고정됨 *)
        let memoryType = "B"
        let address = BAddress
                    |>toString
        let answer = 0xFFuy
        x.Read("%M"+memoryType+address) === answer
        x.Read("%L"+memoryType+address) === answer
        x.Read("%N"+memoryType+address) === answer
        x.Read("%K"+memoryType+address) === answer
        x.Read("%R"+memoryType+address) === answer
        x.Read("%W"+memoryType+address) === answer
        x.Read("%A"+memoryType+address) === answer - answer
        x.Read("%F"+memoryType+address) === answer 
        x.Read("%U"+memoryType+address) === answer          
        //x.Read("%I"+memoryType+address) === answer   
        //x.Read("%Q"+memoryType+address) === answer           

    [<Test>]
    member x.``Readings All Memory word type`` () =
        (* PLC 에서 %_W11 을 FF 값으로 채우고 테스트. 단, A는 0으로 고정됨 *)
        let memoryType = "W"
        let address = WAddress
                    |>toString

        let answer = 0xFFFFus
        x.Read("%M"+memoryType+address) === answer
        x.Read("%L"+memoryType+address) === answer
        x.Read("%N"+memoryType+address) === answer
        x.Read("%K"+memoryType+address) === answer
        x.Read("%R"+memoryType+address) === answer
        x.Read("%W"+memoryType+address) === answer
        x.Read("%A"+memoryType+address) === answer - answer
        x.Read("%F"+memoryType+address) === answer
        x.Read("%U"+memoryType+address) === answer          
        //x.Read("%I"+memoryType+address) === answer   
        //x.Read("%Q"+memoryType+address) === answer      

    [<Test>]
    member x.``Readings All Memory Double word type`` () =
        (* PLC 에서 %_D11 을 FF 값으로 채우고 테스트. 단, A는 0으로 고정됨 *)
        let memoryType = "D"
        let address = DAddress
                    |>toString
        let answer = 0xFFFFFFFFu
        x.Read("%M"+memoryType+address) === answer
        x.Read("%L"+memoryType+address) === answer
        x.Read("%N"+memoryType+address) === answer
        x.Read("%K"+memoryType+address) === answer
        x.Read("%R"+memoryType+address) === answer
        x.Read("%W"+memoryType+address) === answer
        x.Read("%A"+memoryType+address) === answer - answer
        x.Read("%F"+memoryType+address) === answer
        x.Read("%U"+memoryType+address) === answer          
        //x.Read("%I"+memoryType+address) === answer   
        //x.Read("%Q"+memoryType+address) === answer          


    [<Test>]
    member x.``Readings All Memory Long word type`` () =
        (* PLC 에서 %_L11 을 FF 값으로 채우고 테스트. 단, A는 0으로 고정됨 *)
        let memoryType = "L"
        let address = LAddress
                    |>toString
        let answer = 0xFFFFFFFFFFFFFFFFUL
        x.Read("%M"+memoryType+address) === answer
        x.Read("%L"+memoryType+address) === answer
        x.Read("%N"+memoryType+address) === answer
        x.Read("%K"+memoryType+address) === answer
        x.Read("%R"+memoryType+address) === answer
        x.Read("%W"+memoryType+address) === answer
        x.Read("%A"+memoryType+address) === answer - answer
        x.Read("%F"+memoryType+address) === answer
        x.Read("%U"+memoryType+address) === answer          //4.0.0    
        //x.Read("%I"+memoryType+address) === answer   
        //x.Read("%Q"+memoryType+address) === answer        


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

        let q111 = "%QX1.1.1"       // = 4161 : 1*64*64 + 1*64 + 1
        x.Write(q111, true)
        x.Read(q111) === true
        x.ReadFEnet("%QX4161") === true
        x.Write(q111, false)
        x.Read(q111) === false
        x.ReadFEnet("%QX4161") === false
