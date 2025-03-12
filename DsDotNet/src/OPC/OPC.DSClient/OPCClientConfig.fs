namespace OPC.DSClient

open System
open Opc.Ua
open Opc.Ua.Configuration

module OPCClientConfig =

    let createClientConfiguration() =
        // 애플리케이션 구성 생성
        let config = ApplicationConfiguration()
        config.ApplicationName <- "UA Reference Client"
        config.ApplicationUri <- "urn:localhost:Quickstarts:ReferenceClient"
        config.ProductUri <- "uri:opcfoundation.org:Quickstarts:ReferenceClient"
        config.ApplicationType <- ApplicationType.Client

        // 보안 구성
        let securityConfig = SecurityConfiguration()
        securityConfig.ApplicationCertificate <- CertificateIdentifier(
            StoreType = "Directory",
            StorePath = "%CommonApplicationData%\\OPC Foundation\\pki\\own",
            SubjectName = "CN=Quickstart Reference Client, C=US, S=Arizona, O=OPC Foundation, DC=localhost"
        )
        securityConfig.TrustedIssuerCertificates <- CertificateTrustList(
            StoreType = "Directory",
            StorePath = "%CommonApplicationData%\\OPC Foundation\\pki\\issuer"
        )
        securityConfig.TrustedPeerCertificates <- CertificateTrustList(
            StoreType = "Directory",
            StorePath = "%CommonApplicationData%\\OPC Foundation\\pki\\trusted"
        )
        securityConfig.RejectedCertificateStore <- CertificateStoreIdentifier(
            StoreType = "Directory",
            StorePath = "%CommonApplicationData%\\OPC Foundation\\pki\\rejected"
        )
        securityConfig.AutoAcceptUntrustedCertificates <- true
        config.SecurityConfiguration <- securityConfig

        // Transport Quotas 설정
        let transportQuotas = TransportQuotas()
        transportQuotas.OperationTimeout <- 600000
        transportQuotas.MaxStringLength <- 1048576
        transportQuotas.MaxByteStringLength <- 1048576
        transportQuotas.MaxArrayLength <- 65535
        transportQuotas.MaxMessageSize <- 4194304
        transportQuotas.MaxBufferSize <- 65535
        transportQuotas.ChannelLifetime <- 300000
        transportQuotas.SecurityTokenLifetime <- 0xFFFFFFF
        config.TransportQuotas <- transportQuotas

        // 클라이언트 구성
        let clientConfig = ClientConfiguration()
        clientConfig.DefaultSessionTimeout <- 60000
        clientConfig.WellKnownDiscoveryUrls.Add("opc.tcp://localhost:2747/DS")
        clientConfig.WellKnownDiscoveryUrls.Add("opc.tcp://{0}:4840")
        clientConfig.WellKnownDiscoveryUrls.Add("http://{0}:52601/UADiscovery")
        clientConfig.WellKnownDiscoveryUrls.Add("http://{0}/UADiscovery/Default.svc")
        clientConfig.EndpointCacheFilePath <- "Quickstarts.ReferenceClient.Endpoints.xml"
        clientConfig.MinSubscriptionLifetime <- 20000
        config.ClientConfiguration <- clientConfig

        // Trace 설정
        let traceConfig = TraceConfiguration()
        traceConfig.OutputFilePath <- "%CommonApplicationData%\\OPC Foundation\\Logs\\Quickstarts.ReferenceClient.log.txt"
        traceConfig.DeleteOnLoad <- true
        traceConfig.TraceMasks <- 519
        config.TraceConfiguration <- traceConfig

        config
