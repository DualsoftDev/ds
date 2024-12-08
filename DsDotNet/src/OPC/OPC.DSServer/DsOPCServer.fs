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
            let policy4 = ServerSecurityPolicy(
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None
            )
            serverConfig.SecurityPolicies.Add(policy4)

        let config = ApplicationConfiguration()
        config.ApplicationName <- "Dualsoft OPC UA Server"
        config.ApplicationUri <- "urn:localhost:UA:DualsoftServer"
        config.ProductUri <- "uri:dualsoft.com:opc:server"
        config.ApplicationType <- ApplicationType.ClientAndServer

        // Security Configuration
        let securityConfig = SecurityConfiguration()
        securityConfig.ApplicationCertificate <- CertificateIdentifier(
            StoreType = "Directory",
            StorePath = "%CommonApplicationData%\\OPC Foundation\\pki\\own",
            SubjectName = "CN=Dualsoft OPC UA Server, C=US, S=Arizona, O=OPC Foundation, DC=localhost"
        )

        let configureSecurity (securityConfig: SecurityConfiguration) =
            securityConfig.AutoAcceptUntrustedCertificates <- true
            securityConfig.RejectSHA1SignedCertificates <- false
            securityConfig.RejectUnknownRevocationStatus <- false
            securityConfig.TrustedPeerCertificates.ValidationOptions <- CertificateValidationOptions.CheckRevocationStatusOnline
            securityConfig.TrustedIssuerCertificates.ValidationOptions <- CertificateValidationOptions.CheckRevocationStatusOnline
            securityConfig.RejectedCertificateStore <- null
            securityConfig.TrustedPeerCertificates <- null
            securityConfig.TrustedIssuerCertificates <- null

        configureSecurity securityConfig
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
        serverConfig.BaseAddresses.Add("opc.tcp://localhost:2747")
        serverConfig.MinRequestThreadCount <- 5
        serverConfig.MaxRequestThreadCount <- 100
        serverConfig.MaxQueuedRequestCount <- 2000

        // 익명 인증만 활성화
        serverConfig.UserTokenPolicies.Clear()
        serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.Anonymous))
        config.ServerConfiguration <- serverConfig

        // 적용된 보안 정책 추가
        applySecurityPolicies serverConfig

        // Trace Configuration
        let traceConfig = TraceConfiguration()
        traceConfig.OutputFilePath <- "%CommonApplicationData%\\OPC Foundation\\Logs\\DualsoftServer.log.txt"
        traceConfig.DeleteOnLoad <- true
        traceConfig.TraceMasks <- 519
        config.TraceConfiguration <- traceConfig

        config




type DsOPCServer(dsSys: DsSystem) =
    inherit StandardServer()

    // NodeManager를 생성하여 주소 공간 관리
    override this.CreateMasterNodeManager(server: IServerInternal, configuration: ApplicationConfiguration) =
        let nodeManager = new DsNodeManager(server, configuration, dsSys)
        new MasterNodeManager(server, configuration, null, [|nodeManager:> INodeManager|])
