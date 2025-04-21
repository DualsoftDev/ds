namespace OPC.DSServer

open System
open System.Threading.Tasks
open Opc.Ua
open Opc.Ua.Configuration
open Engine.Runtime
open Engine.Core.Interface
open Engine.Core
open System.IO
open System.Collections.Generic

module DsOpcUaServerManager =

    let mutable server: DsOPCServer option = None
    let IsRunning():bool = server.IsSome 
    let IsConnectedNotDSClient():bool = 
        match server with
        | Some s -> s.IsConnectedNotDSClient
        | None -> false

    let GetDSClients() = 
        match server with
        | Some s -> s.ClientDsSessions
        | None -> []

        /// <summary>
    /// OPC UA 서버 종료
    /// </summary>
    let Stop(dsSys: DsSystem) =
        match server with
        | Some s ->
            SaveStatisticsToJson (dsSys.Name, DsTimeAnalysisMoudle.getStatsJson())
            s.Stop()
            server <- None
            printfn "OPC UA 서버가 종료되었습니다."
        | None ->
            printfn "서버가 실행 중이 아닙니다."

    let ChangeDSStorage(dsSys: DsSystem) =
        match server with
        | Some s ->
            s.ChangeDSStorage(dsSys.TagManager.Storages)
        | None ->
            printfn "서버가 실행 중이 아닙니다."

    let DeleteStatisticsFile(systemName:string) =
        let filePath = Path.Combine(fromServerConfig "StatisticsFilePath", $"{systemName}.json")
        if File.Exists filePath
        then 
            File.Delete filePath

        filePath

    /// <summary>
    /// OPC UA 서버 시작
    /// </summary>
    let Start(dsSys: DsSystem, mode:RuntimeMode, targetIP:string) =
        printfn "OPC UA 서버 초기화 중..."
        if server.IsSome  then Stop(dsSys);
        // 1. 애플리케이션 인스턴스 생성
        let application = 
            ApplicationInstance(
                ApplicationName = "DsOpcServer",
                ApplicationType = ApplicationType.Server
            )
        // 2. 설정 로드
        let config = DsOPCServerConfig.createApplicationConfiguration(mode, targetIP)
        //application.LoadApplicationConfiguration(false).Wait()
        application.ApplicationConfiguration <- config

        // 3. 인증서  확인
        let isCertValid  = application.CheckApplicationInstanceCertificates(false, 2048us).Result
        if not(isCertValid)
        then
            failwith("Failed to validate or generate the application certificate.")

        // 4. 서버 시작
        let opcServer = new DsOPCServer(dsSys, mode)
        server <- Some opcServer
        try
            application.Start(opcServer).Wait()
        with
        | :? AggregateException as aggEx ->
            aggEx.InnerExceptions
            |> Seq.iter (fun ex -> 
                match ex with
                | :? ServiceResultException as sre when sre.InnerResult <> null -> failwith sre.InnerResult.AdditionalInfo
                | _ -> failwith ex.Message)
        | _ ->
            failwith "Error starting OPCServer"


        printfn "OPC UA 서버가 시작되었습니다."
        printfn "엔드포인트:"
        opcServer.GetEndpoints()
        |> Seq.iter (fun endpoint -> printfn "%s" endpoint.EndpointUrl)

