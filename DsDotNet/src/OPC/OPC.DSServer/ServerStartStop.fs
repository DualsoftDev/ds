namespace OPC.DSServer

open System
open System.Threading.Tasks
open Opc.Ua
open Opc.Ua.Configuration
open Engine.Runtime
open Engine.Core.Interface
open Engine.Core

module DsOpcUaServerManager =

    let mutable server: DsOPCServer option = None

    /// <summary>
    /// OPC UA 서버 시작
    /// </summary>
    let Start(dsSys: DsSystem, opcConfigPath:string) =
        printfn "OPC UA 서버 초기화 중..."

        let config = 
            if String.IsNullOrEmpty opcConfigPath
            then "DsOpcServer"
            else opcConfigPath

        // 1. 애플리케이션 인스턴스 생성
        let application = 
            ApplicationInstance(
                ApplicationName = "DsOpcServer",
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = config
            )
        // 2. 설정 파일 로드
        application.LoadApplicationConfiguration(false).Wait()

        // 3. 인증서  확인
        application.CheckApplicationInstanceCertificate(false, 2048us).Wait()  

        // 4. 서버 시작
        let opcServer = new DsOPCServer(dsSys)
        server <- Some opcServer
        application.Start(opcServer).Wait()

        printfn "OPC UA 서버가 시작되었습니다."
        printfn "엔드포인트:"
        opcServer.GetEndpoints()
        |> Seq.iter (fun endpoint -> printfn "%s" endpoint.EndpointUrl)

    /// <summary>
    /// OPC UA 서버 종료
    /// </summary>
    let Stop() =
        match server with
        | Some s ->
            s.Stop()
            printfn "OPC UA 서버가 종료되었습니다."
        | None ->
            printfn "서버가 실행 중이 아닙니다."
