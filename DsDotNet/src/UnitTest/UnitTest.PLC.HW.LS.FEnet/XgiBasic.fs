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


    member private x.Write(tag, value) =
        let lsTag = LsTagXgi(x.Conn, tag)
        lsTag.Value <- value
        x.Conn.WriteATag(lsTag) |> ignore
    member private x.Read(tag:string) = x.Conn.ReadATag(tag)

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
        (* PLC 에서 %_B11 을 FF 값으로 채우고 테스트. 단, F와 A는 0으로 고정됨 *)
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
        (* PLC 에서 %_B11 을 FF 값으로 채우고 테스트. 단, F와 A는 0으로 고정됨 *)
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
        (* PLC 에서 %_W11 을 FF 값으로 채우고 테스트. 단, F와 A는 0으로 고정됨 *)
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
        (* PLC 에서 %_W11 을 FF 값으로 채우고 테스트. 단, F와 A는 0으로 고정됨 *)
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
        (* PLC 에서 %_W11 을 FF 값으로 채우고 테스트. 단, A는 0으로 고정됨 *)
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
        x.Write("%ML1", ul0)
        x.Read("%ML1") === ul0
        noop()


