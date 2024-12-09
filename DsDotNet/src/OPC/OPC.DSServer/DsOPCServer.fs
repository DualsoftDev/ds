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
    let createApplicationConfiguration () =
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
        config.ApplicationName <- "Dualsoft OPC UA Server"
        config.ApplicationUri <- "urn:localhost:UA:DualsoftServer"
        config.ProductUri <- "uri:dualsoft.com:opc:server"
        config.ApplicationType <- ApplicationType.Server

        let configureSecurity (securityConfig: SecurityConfiguration) =
            securityConfig.MinimumCertificateKeySize <- 2048us
            securityConfig.AutoAcceptUntrustedCertificates <- true //기본 false
            securityConfig.RejectSHA1SignedCertificates <- false  //기본 true
            securityConfig.RejectUnknownRevocationStatus <- false //기본 false
            securityConfig.TrustedPeerCertificates.ValidationOptions <- CertificateValidationOptions.CheckRevocationStatusOnline
            securityConfig.TrustedIssuerCertificates.ValidationOptions <- CertificateValidationOptions.CheckRevocationStatusOnline
            securityConfig.RejectedCertificateStore <- null
            securityConfig.TrustedPeerCertificates <- null
            securityConfig.TrustedIssuerCertificates <- null



        // Security Configuration
        let securityConfig = SecurityConfiguration()
        securityConfig.ApplicationCertificate <- CertificateIdentifier(
            StoreType = "Directory",
            StorePath = "%CommonApplicationData%\\OPC Foundation\\pki\\own",
            SubjectName = "CN=OPC UA Server, C=US, S=Arizona, O=OPC Foundation, DC=localhost"
        )

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
        transportQuotas.SecurityTokenLifetime <- 3600000
        config.TransportQuotas <- transportQuotas

        // Server Configuration
        let serverConfig = ServerConfiguration()
        serverConfig.BaseAddresses.Add("https://ahn:2747")
        serverConfig.BaseAddresses.Add("https://localhost:2747")
        serverConfig.BaseAddresses.Add("opc.tcp://localhost:2747/ds")
        serverConfig.AlternateBaseAddresses.Add("opc.tcp://ahn:2747")
        serverConfig.AlternateBaseAddresses.Add("opc.tcp://127.0.0.1:2747")
        serverConfig.AlternateBaseAddresses.Add("opc.tcp://127.0.0.1:55555")
        serverConfig.MinRequestThreadCount <- 5
        serverConfig.MaxRequestThreadCount <- 100
        serverConfig.MaxQueuedRequestCount <- 2000

        // 익명 인증만 활성화
        //serverConfig.UserTokenPolicies.Clear()
        //serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.Anonymous))



        config.ServerConfiguration <- serverConfig

        // 적용된 보안 정책 추가
        applySecurityPolicies serverConfig

        // Trace Configuration
        let traceConfig = TraceConfiguration()
        traceConfig.OutputFilePath <- "%CommonApplicationData%\\OPC Foundation\\Logs\\DualsoftServer.log.txt"
        traceConfig.DeleteOnLoad <- true
        traceConfig.TraceMasks <- 519     
        //traceConfig.TraceMasks <- 0xFFFFFFFF  // 디버그 출력 활성화
        config.TraceConfiguration <- traceConfig

        config




type DsOPCServer(dsSys: DsSystem) =
    inherit StandardServer()

    // NodeManager를 생성하여 주소 공간 관리
    override this.CreateMasterNodeManager(server: IServerInternal, configuration: ApplicationConfiguration) =
        let nodeManager = new DsNodeManager(server, configuration, dsSys)
        new MasterNodeManager(server, configuration, null, [|nodeManager:> INodeManager|])
