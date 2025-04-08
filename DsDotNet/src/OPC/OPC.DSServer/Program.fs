namespace OPC.DSServer

open System
open Engine.Runtime
open Engine.Core

/// <summary>
/// 테스트 코드
/// </summary>
module Program =

    [<EntryPoint>]
    let main _ =
        try
            // 4. RuntimeModel 초기화
            let runtimeModel = 
                new RuntimeModel(@"z://HelloDS.dsz", Engine.Core.RuntimeGeneratorModule.PlatformTarget.WINDOWS)

            // OPC UA 서버 시작
            DsOpcUaServerManager.Start(runtimeModel.System, RuntimePackage.Simulation, "127")

            printfn "종료하려면 아무 키나 누르세요..."
            Console.ReadKey() |> ignore

            // OPC UA 서버 종료
            DsOpcUaServerManager.Stop(runtimeModel.System)
        with ex ->
            printfn "오류: %s" ex.Message
        0 // 프로그램 종료 코드
