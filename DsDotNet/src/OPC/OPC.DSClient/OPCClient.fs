namespace OPC.DSClient

open System
open Opc.Ua
open Opc.Ua.Client
open Opc.Ua.Configuration
open OPC.DSClient.OPCClientConfig

[<AutoOpen>]
module OPCClientModule =

    /// OPC 클라이언트 모듈 정의
    type OPCDsClient() =
        let mutable session: Session option = None
        let connectionReadyEvent = Event<unit>()
        let connectionClosedEvent = Event<unit>()
        let connectionLostEvent = Event<unit>()
        let mutable _endpointUrl: string = ""
        let mutable _timeout: int = 6000
        let mutable keepAliveHandler = KeepAliveEventHandler(fun _sender _args ->())
        
        /// 연결 준비 완료 이벤트 외부 노출
        [<CLIEvent>]
        member this.ConnectionReady = connectionReadyEvent.Publish

        /// 연결 종료 이벤트 외부 노출
        [<CLIEvent>]
        member this.ConnectionClosed = connectionClosedEvent.Publish

        /// 연결 손실 이벤트 외부 노출
        [<CLIEvent>]
        member this.ConnectionLost = connectionLostEvent.Publish

        /// 현재 세션 반환
        member this.Session = session

        /// 클라이언트 준비 완료 이벤트 트리거
        member private this.TriggerConnectionReady() =
            printfn "OPC UA client is ready."
            connectionReadyEvent.Trigger()

        /// 세션 종료 이벤트 트리거
        member private this.TriggerConnectionClosed() =
            printfn "OPC UA session closed."
            connectionClosedEvent.Trigger()

        /// 연결 손실 이벤트 트리거
        member private this.TriggerConnectionLost() =
            printfn "OPC UA connection lost."
            connectionLostEvent.Trigger()

        member this.InitializeOPC(url: string, timeout: int) =
            _endpointUrl <- url
            _timeout <- timeout
            let config = createClientConfiguration()
            let application = new ApplicationInstance(
                ApplicationName = "UA Reference Client",
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config
            )

            // 인증서 확인
            if not (application.CheckApplicationInstanceCertificates(false, 0us).Result) then
                raise (Exception("Invalid application certificate!"))

            // 엔드포인트 설정
            let endpointDescription = CoreClientUtils.SelectEndpoint(application.ApplicationConfiguration, _endpointUrl, false)
            let endpointConfiguration = EndpointConfiguration.Create(application.ApplicationConfiguration)
            let endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration)

            try
                let newSession =
                    Session.Create(
                        application.ApplicationConfiguration,
                        endpoint,
                        false,
                        "UA Reference Client Session",
                        uint32 timeout,
                        null,
                        null
                    ).Result

                // KeepAlive 이벤트 핸들러
                newSession.add_KeepAlive(keepAliveHandler)

                session <- Some newSession

                printfn "OPC UA session is ready for client operations."
                this.TriggerConnectionReady()

            with ex ->
                printfn "Failed to create session: %s" ex.Message
                this.TriggerConnectionClosed()
                raise ex

        /// 연결 종료
        member this.Disconnect() =
            match session with
            | Some activeSession ->
                activeSession.Close() |> ignore
                activeSession.Dispose()
                session <- None
                this.TriggerConnectionClosed()
            | None ->
                printfn "No active session to close."
