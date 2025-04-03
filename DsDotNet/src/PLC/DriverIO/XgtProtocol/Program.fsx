namespace XgtProtocol.FS

open System
open System.Threading



module Test =

    let testLoop (plcName: string) (plc: XgtProtocol.FS.XgtProtocol) (areaCodes: char list) =
        let rnd = Random()

        while true do
            printfn $"===== [{plcName}] 테스트 루프 시작: {DateTime.Now} ====="
            for code in areaCodes do
                try
                    let bitAddr = sprintf "%%%cX1000" code
                    let byteAddr = sprintf "%%%cB1000" code
                    let wordAddr = sprintf "%%%cW1000" code
                    let dwordAddr = sprintf "%%%cD1000" code
                    let lwordAddr = sprintf "%%%cL1000" code

                    printfn "--- [%c 영역 테스트] ---" code

                    let bitVal = true
                    let bitOk = plc.WriteData(bitAddr, Bit, box bitVal)
                    let bitRead = plc.ReadData(bitAddr, Bit)
                    printfn "[Bit]   %s := %b → %b / Read → %A" bitAddr bitVal bitOk bitRead

                    let byteVal = byte (rnd.Next(0, 255))
                    let byteOk = plc.WriteData(byteAddr, DataType.Byte, box byteVal)
                    let byteRead = plc.ReadData(byteAddr, DataType.Byte)
                    printfn "[Byte]  %s := 0x%02X → %b / Read → %A" byteAddr byteVal byteOk byteRead

                    let wordVal = uint16 (rnd.Next(0, int(Int16.MaxValue)))
                    let wordOk = plc.WriteData(wordAddr, Word, box wordVal)
                    let wordRead = plc.ReadData(wordAddr, Word)
                    printfn "[Word]  %s := %d → %b / Read → %A" wordAddr wordVal wordOk wordRead

                    let dwordVal = uint32 (rnd.Next(0, Int32.MaxValue))
                    let dwordOk = plc.WriteData(dwordAddr, DWord, box dwordVal)
                    let dwordRead = plc.ReadData(dwordAddr, DWord)
                    printfn "[DWord] %s := %d → %b / Read → %A" dwordAddr dwordVal dwordOk dwordRead

                    let lwordVal = 9876543210123456789UL
                    let lwordOk = plc.WriteData(lwordAddr, LWord, box lwordVal)
                    let lwordRead = plc.ReadData(lwordAddr, LWord)
                    printfn "[LWord] %s := %A → %b / Read → %A" lwordAddr lwordVal lwordOk lwordRead

                with ex ->
                    printfn "[!] 예외 발생: %s" ex.Message

            printfn $"===== [{plcName}] 루프 종료 후 0초 대기 =====\n"
            //Thread.Sleep(5000) // 5초 간격

    //[<EntryPoint>]
    //let main argv =
    //    let ipXGI = "192.168.9.102"
    //    let ipXGK = "192.168.9.103"
    //    let port = 2004

    //    let plcXGI = new XgtProtocol.FS.XgtProtocol(ipXGI, port)
    //    let plcXGK = new XgtProtocol.FS.XgtProtocol(ipXGK, port)

    //    let areaCodesXGI = [ 'I'; (*'Q';*) 'M'; 'L'; 'N'; 'K'; 'U'; 'R'; 'A'; 'W';(* 'F'*) ]
    //    let areaCodesXGK = [ 'P'; 'M'; 'K'; (*'F';*) 'T'; 'C'; 'U'; 'Z'; 'S'; 'L'; 'N'; 'D'; 'R' ]

    //    if plcXGI.Connect() && plcXGK.Connect() then
    //        printfn "[✓] 두 PLC 모두 연결됨.\n"

    //        let threadXGI = Thread(ThreadStart(fun () -> testLoop "XGI" plcXGI areaCodesXGI))
    //        let threadXGK = Thread(ThreadStart(fun () -> testLoop "XGK" plcXGK areaCodesXGK))

    //        threadXGI.Start()
    //        threadXGK.Start()

    //        threadXGI.Join()
    //        threadXGK.Join()

    //    else
    //        printfn "[X] PLC 연결 실패."

    //    0
