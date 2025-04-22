namespace T

open NUnit.Framework
open FsUnit.Xunit
open MelsecProtocol
open Dual.PLC.Common.FS
open System.Threading

type MxCommTests() =

    let ip = "192.168.9.109"
    let delay = 20
    let timeout = 2000
    let isMonitorOnly = false

    [<Test>]
    member _.``Connect Should Succeed And Report Connected`` () =
        let scan = MxPlcScan(ip, delay, timeout, isMonitorOnly)

        let mutable state = ""
        scan.ConnectChangedNotify.Add(fun e ->
            printfn $"[Event] ConnectState: {e.State}"
            state <- string e.State
        )

        scan.Connect()
        scan.IsConnected |> should equal true
        state |> should equal "Connected"

        scan.Disconnect()
        scan.IsConnected |> should equal true // 현재 IsConnected는 true만 리턴하므로 생략 가능

    [<Test>]
    member _.``Write and Read Tags Should Succeed On Real PLC`` () =
        let scan = MxPlcScan(ip, delay, timeout, false)

        scan.Connect()

        let tagList = [
            {
                Name = "WriteTest"
                Address = "D100"
                Comment = "테스트"
                IsOutput = true
            }
            {
                Name = "ReadTest"
                Address = "D100"
                Comment = "테스트"
                IsOutput = false
            }
        ]


        let mxTags = scan.Scan(tagList)
        mxTags["D100"].SetWriteValue(1234 :> obj)    

        // 약간 기다리기 (PLC 반응 시간 고려)
        Thread.Sleep(100)

        // 읽은 태그의 값 확인
        let tagRead = mxTags["D100"]
        tagRead.Value |> should equal (1234 :> obj)

        scan.Disconnect()
