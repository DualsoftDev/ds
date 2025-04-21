namespace OPC.DSServer

open System
open System.Collections.Generic
open Opc.Ua
open Opc.Ua.Configuration
open Opc.Ua.Server
open Engine.Core.Interface
open Engine.Core


open Opc.Ua
open Opc.Ua.Configuration

[<AutoOpen>]
module DsOPCServerConfig =
    let createApplicationConfiguration (mode: RuntimeMode, targetIp:string) =
        let instanceName = $"{targetIp}_{mode.ToString()}"   

        let applySecurityPolicies (serverConfig: ServerConfiguration) =
            // 기본적인 보안 정책 리스트
            let securityPolicies = [
                (MessageSecurityMode.None, SecurityPolicies.None) // 서명, 보안 없음
                (MessageSecurityMode.None, SecurityPolicies.Https)
                (MessageSecurityMode.SignAndEncrypt, SecurityPolicies.Https)
                (MessageSecurityMode.SignAndEncrypt, SecurityPolicies.None) 
                (MessageSecurityMode.SignAndEncrypt, SecurityPolicies.Basic256Sha256)
                (MessageSecurityMode.SignAndEncrypt, SecurityPolicies.Basic256)
                (MessageSecurityMode.SignAndEncrypt, SecurityPolicies.Basic128Rsa15)
            ]

            // 각 정책을 ServerConfiguration에 추가
            for (securityMode, policyUri) in securityPolicies do
                let policy = ServerSecurityPolicy(
                    SecurityMode = securityMode,
                    SecurityPolicyUri = policyUri
                )
                serverConfig.SecurityPolicies.Add(policy)
                  // User Token Policies
            serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.Anonymous))
            serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.UserName))
            serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.Certificate))


        let config = ApplicationConfiguration()
        config.ApplicationName <- $"Dualsoft OPC UA Server [{instanceName}]"
        config.ApplicationUri <- $"urn:localhost:UA:DualsoftServer:{instanceName}"
        config.ProductUri <- $"uri:dualsoft.com:opc:{instanceName}"
        config.ApplicationType <- ApplicationType.Server

        //let configureSecurity (securityConfig: SecurityConfiguration) =
        //    securityConfig.MinimumCertificateKeySize <- 2048us
        //    securityConfig.AutoAcceptUntrustedCertificates <- true //기본 false
        //    securityConfig.RejectSHA1SignedCertificates <- false  //기본 true
        //    securityConfig.RejectUnknownRevocationStatus <- false //기본 false
        //    securityConfig.TrustedPeerCertificates.ValidationOptions <- CertificateValidationOptions.CheckRevocationStatusOnline
        //    securityConfig.TrustedIssuerCertificates.ValidationOptions <- CertificateValidationOptions.CheckRevocationStatusOnline
        //    securityConfig.RejectedCertificateStore <- null
        //    securityConfig.TrustedPeerCertificates <- null
        //    securityConfig.TrustedIssuerCertificates <- null



        // Security Configuration
        let securityConfig = SecurityConfiguration()
        let dt = $"{DateTime.Now:yyMMdd_HH_mm_ss}"
        let storePath = "%CommonApplicationData%\\OPC Foundation\\pki\\own\\"+dt
        securityConfig.ApplicationCertificate <- CertificateIdentifier(
            StoreType = "Directory",
            StorePath = storePath,
            SubjectName = $"CN=OPC UA Server {instanceName}, O=Dualsoft, DC=localhost"        )

        //configureSecurity securityConfig
        config.SecurityConfiguration <- securityConfig

        // Transport Quotas
        let transportQuotas = TransportQuotas()
        transportQuotas.OperationTimeout <- 120000
        transportQuotas.MaxStringLength <- 1048576
        transportQuotas.MaxByteStringLength <- 1048576
        transportQuotas.MaxArrayLength <- 65535
        transportQuotas.MaxMessageSize <- 4194304
        transportQuotas.MaxBufferSize <- 65535
        transportQuotas.ChannelLifetime <- 30000
        transportQuotas.SecurityTokenLifetime <- 0xFFFFFFF
        config.TransportQuotas <- transportQuotas

        // Server Configuration
        let serverConfig = ServerConfiguration()
        let serverPort = ServerConfigModule.GetOPCServerPort(mode, targetIp)

        serverConfig.BaseAddresses.Add($"opc.tcp://localhost:{serverPort}")
        serverConfig.AlternateBaseAddresses.Add($"https://ds:{serverPort}")
        serverConfig.AlternateBaseAddresses.Add($"https://localhost:{serverPort}")
        serverConfig.AlternateBaseAddresses.Add($"opc.tcp://ds:{serverPort}")
        serverConfig.AlternateBaseAddresses.Add($"opc.tcp://127.0.0.1:{serverPort}")

        serverConfig.MinRequestThreadCount <- 5
        serverConfig.MaxRequestThreadCount <- 100
        serverConfig.MaxQueuedRequestCount <- 2000
        
        
        
        config.ServerConfiguration <- serverConfig

        // 적용된 보안 정책 추가
        applySecurityPolicies serverConfig

        // Trace Configuration
        let traceConfig = TraceConfiguration()
        traceConfig.OutputFilePath <- "%CommonApplicationData%\\OPC Foundation\\Logs\\DualsoftServer.log.txt"
        traceConfig.DeleteOnLoad <- true
#if DEBUG
        traceConfig.TraceMasks <- 519     
#else
        traceConfig.TraceMasks <- 0
#endif
        //traceConfig.TraceMasks <- 0xFFFFFFFF  // 디버그 출력 활성화
        config.TraceConfiguration <- traceConfig

        config

type DsOPCServer(dsSys: DsSystem, mode: RuntimeMode) =
    inherit StandardServer()

    let mutable dsNodeManager = Unchecked.defaultof<DsNodeManager>

    // 생성자 실행 블록
    do
        DsTimeAnalysisMoudle.statsMap.Clear()
        let data = LoadStatisticsFromJson (dsSys.Name)
        data
        |> Seq.iter (fun kv -> DsTimeAnalysisMoudle.statsMap.TryAdd(kv.Key, getCalcStats(kv.Value)) |> ignore)


    /// NodeManager를 생성하여 주소 공간 관리
    override this.CreateMasterNodeManager(server: IServerInternal, configuration: ApplicationConfiguration) =
        dsNodeManager <- new DsNodeManager(server, configuration, dsSys, mode)
        new MasterNodeManager(server, configuration, null, [| dsNodeManager :> INodeManager |])

    /// 외부 저장소 변경
    member this.ChangeDSStorage(stg: Storages) =
        dsNodeManager.ChangeDSStorage stg

    /// Dualsoft 클라이언트 외의 세션들   
    member this.ClientDsSessions =
        let clients = 
            base.ServerInternal.SessionManager.GetSessions()
            |> Seq.filter (fun session ->
                not (String.IsNullOrWhiteSpace session.SessionDiagnostics.SessionName) &&
                not (session.SessionDiagnostics.SessionName.Contains "Dualsoft"))
        clients  
    
    /// Dualsoft 클라이언트 외의 세션이 접속되어 있는지 확인
    member this.IsConnectedNotDSClient =
        this.ClientDsSessions |> Seq.exists (fun session -> session.Activated)
          