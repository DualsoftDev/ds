namespace T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Engine.Common.FS
open System.Text.RegularExpressions
open Dsu.PLC.LS

type XgiBasic() =
    inherit FEnetTestBase("192.168.0.100")

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
        x.Write("%ML512", 18446744073709551615UL)
        x.Write("%LL512", 18446744073709551615UL)
        x.Write("%NL512", 18446744073709551615UL)
        x.Write("%KL512", 18446744073709551615UL)
        x.Write("%RL512", 18446744073709551615UL)
        x.Write("%WL512", 18446744073709551615UL)
        x.Write("%AL512", 18446744073709551615UL)           //WARNING
        x.Write("%FL512", 18446744073709551615UL)
        x.Write("%IL512", 18446744073709551615UL)
        x.Write("%QL512", 18446744073709551615UL)
        x.Write("%UL4.0.0", 18446744073709551615UL)
        x.ReadFEnet("%MX32768") === true
        x.ReadFEnet("%LX32768") === true
        x.ReadFEnet("%NX32768") === true
        x.ReadFEnet("%KX32768") === true
        x.ReadFEnet("%RX32768") === true
        x.ReadFEnet("%WX32768") === true
        x.ReadFEnet("%AX32768") === true                    //WARNING
        x.ReadFEnet("%FX32768") === true 
        //x.ReadFEnet("%UX32768") === true                  //4.0.0  없는 주소  
        x.ReadFEnet("%IX32768") === true                    //32.0.0
        x.ReadFEnet("%QX32768") === true                    //32.0.0 
        x.Read("%UX4.0.0") === true                         //4.0.0    
        x.Read("%IX32.0.0") === true                        //32.0.0
        x.Read("%QX32.0.0") === true                        //32.0.0 

    [<Test>]
    member x.``Readings All Memory byte type`` () =
        x.Write("%ML512", 18446744073709551615UL)
        x.Write("%LL512", 18446744073709551615UL)
        x.Write("%NL512", 18446744073709551615UL)
        x.Write("%KL512", 18446744073709551615UL)
        x.Write("%RL512", 18446744073709551615UL)
        x.Write("%WL512", 18446744073709551615UL)
        x.Write("%AL512", 18446744073709551615UL)           //WARNING
        x.Write("%FL512", 18446744073709551615UL)
        x.Write("%IL512", 18446744073709551615UL)
        x.Write("%QL512", 18446744073709551615UL)
        x.Write("%UL4.0.0", 18446744073709551615UL)
        x.ReadFEnet("%MB4096") === 0xFFuy
        x.ReadFEnet("%LB4096") === 0xFFuy
        x.ReadFEnet("%NB4096") === 0xFFuy
        x.ReadFEnet("%KB4096") === 0xFFuy
        x.ReadFEnet("%RB4096") === 0xFFuy
        x.ReadFEnet("%WB4096") === 0xFFuy
        x.ReadFEnet("%AB4096") === 0xFFuy                   //WARNING
        x.ReadFEnet("%FB4096") === 0xFFuy 
        //x.ReadFEnet("%U4096") === 0xFFuy                  //4.0.0    
        x.ReadFEnet("%IB4096") === 0xFFuy                    //32.0.0
        x.ReadFEnet("%QB4096") === 0xFFuy                    //32.0.0    
        x.Read("%UB4.0.0") === 0xFFuy                       //4.0.0    
        x.Read("%IB32.0.0") === 0xFFuy                      //32.0.0
        x.Read("%QB32.0.0") === 0xFFuy                      //32.0.0 

    [<Test>]
    member x.``Readings All Memory word type`` () =
        x.Write("%ML512", 18446744073709551615UL)
        x.Write("%LL512", 18446744073709551615UL)
        x.Write("%NL512", 18446744073709551615UL)
        x.Write("%KL512", 18446744073709551615UL)
        x.Write("%RL512", 18446744073709551615UL)
        x.Write("%WL512", 18446744073709551615UL)
        x.Write("%AL512", 18446744073709551615UL)           //WARNING
        x.Write("%FL512", 18446744073709551615UL)
        x.Write("%IL512", 18446744073709551615UL)
        x.Write("%QL512", 18446744073709551615UL)
        x.Write("%UL4.0.0", 18446744073709551615UL)
        x.ReadFEnet("%MW2048") === 0xFFFFus
        x.ReadFEnet("%LW2048") === 0xFFFFus
        x.ReadFEnet("%NW2048") === 0xFFFFus
        x.ReadFEnet("%KW2048") === 0xFFFFus
        x.ReadFEnet("%RW2048") === 0xFFFFus
        x.ReadFEnet("%WW2048") === 0xFFFFus
        x.ReadFEnet("%AW2048") === 0xFFFFus                 //WARNING
        x.ReadFEnet("%FW2048") === 0xFFFFus 
        //x.ReadFEnet("%UW2048") === 0xFFFFus               //4.0.0    
        x.ReadFEnet("%IW2048") === 0xFFFFus                 //32.0.0
        x.ReadFEnet("%QW2048") === 0xFFFFus                 //32.0.0    
        x.Read("%UW4.0.0") === 0xFFFFus                     //4.0.0    
        x.Read("%IW32.0.0") === 0xFFFFus                    //32.0.0
        x.Read("%QW32.0.0") === 0xFFFFus                    //32.0.0   

    [<Test>]
    member x.``Readings All Memory Double word type`` () =
        x.Write("%ML512", 18446744073709551615UL)
        x.Write("%LL512", 18446744073709551615UL)
        x.Write("%NL512", 18446744073709551615UL)
        x.Write("%KL512", 18446744073709551615UL)
        x.Write("%RL512", 18446744073709551615UL)
        x.Write("%WL512", 18446744073709551615UL)
        x.Write("%AL512", 18446744073709551615UL)           //WARNING
        x.Write("%FL512", 18446744073709551615UL)
        x.Write("%IL512", 18446744073709551615UL)
        x.Write("%QL512", 18446744073709551615UL)
        x.Write("%UL4.0.0", 18446744073709551615UL)
        x.ReadFEnet("%MD1024") === 0xFFFFFFFFu
        x.ReadFEnet("%LD1024") === 0xFFFFFFFFu
        x.ReadFEnet("%ND1024") === 0xFFFFFFFFu
        x.ReadFEnet("%KD1024") === 0xFFFFFFFFu
        x.ReadFEnet("%RD1024") === 0xFFFFFFFFu
        x.ReadFEnet("%WD1024") === 0xFFFFFFFFu
        x.ReadFEnet("%AD1024") === 0xFFFFFFFFu              //WARNING
        x.ReadFEnet("%FD1024") === 0xFFFFFFFFu 
        //x.ReadFEnet("%UD1024") === 0xFFFFFFFFu            //4.0.0    
        x.ReadFEnet("%ID1024") === 0xFFFFFFFFu              //32.0.0
        x.ReadFEnet("%QD1024") === 0xFFFFFFFFu              //32.0.0            
        x.Read("%UD4.0.0") === 0xFFFFFFFFu                  //4.0.0    
        x.Read("%ID32.0.0") === 0xFFFFFFFFu                 //32.0.0
        x.Read("%QD32.0.0") === 0xFFFFFFFFu                 //32.0.0 

    [<Test>]
    member x.``Readings All Memory Long word type`` () =
        x.Write("%ML512", 18446744073709551615UL)
        x.Write("%LL512", 18446744073709551615UL)
        x.Write("%NL512", 18446744073709551615UL)
        x.Write("%KL512", 18446744073709551615UL)
        x.Write("%RL512", 18446744073709551615UL)
        x.Write("%WL512", 18446744073709551615UL)
        x.Write("%AL512", 18446744073709551615UL)           //WARNING
        x.Write("%FL512", 18446744073709551615UL)
        x.Write("%IL512", 18446744073709551615UL)
        x.Write("%QL512", 18446744073709551615UL)
        x.Write("%UL4.0.0", 18446744073709551615UL)
        x.ReadFEnet("%ML512") === 0xFFFFFFFFFFFFFFFFUL
        x.ReadFEnet("%LL512") === 0xFFFFFFFFFFFFFFFFUL
        x.ReadFEnet("%NL512") === 0xFFFFFFFFFFFFFFFFUL
        x.ReadFEnet("%KL512") === 0xFFFFFFFFFFFFFFFFUL
        x.ReadFEnet("%RL512") === 0xFFFFFFFFFFFFFFFFUL
        x.ReadFEnet("%WL512") === 0xFFFFFFFFFFFFFFFFUL
        x.ReadFEnet("%AL512") === 0xFFFFFFFFFFFFFFFFUL              //WARNING
        x.ReadFEnet("%FL512") === 0xFFFFFFFFFFFFFFFFUL 
        //x.ReadFEnet("%UL512") === 0xFFFFFFFFFFFFFFFFUL            //4.0.0    
        x.ReadFEnet("%IL512") === 0xFFFFFFFFFFFFFFFFUL              //32.0.0
        x.ReadFEnet("%QL512") === 0xFFFFFFFFFFFFFFFFUL              //32.0.0    
        x.Read("%UL4.0.0") === 0xFFFFFFFFFFFFFFFFUL                 //4.0.0    
        x.Read("%IL32.0.0") === 0xFFFFFFFFFFFFFFFFUL                //32.0.0
        x.Read("%QL32.0.0") === 0xFFFFFFFFFFFFFFFFUL                //32.0.0 


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
