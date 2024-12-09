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
module DsOPCServerConfig2 =

    let createApplicationConfiguration2() =
        // Create configuration object
        let config = ApplicationConfiguration()
    
        // General Application Settings
        config.ApplicationName <- "Quickstart SimpleEvents Server"
        config.ApplicationUri <- "http://localhost/Quickstarts/SimpleEventsServer"
        config.ProductUri <- "http://opcfoundation.org/Quickstarts/SimpleEventsServer"
        config.ApplicationType <- ApplicationType.Server

        // Security Configuration
        let securityConfig = SecurityConfiguration()
        securityConfig.ApplicationCertificate <- CertificateIdentifier(
            StoreType = "Directory",
            StorePath = "%CommonApplicationData%\\OPC Foundation\\pki\\own",
            SubjectName = "CN=Quickstart SimpleEvents Server, C=US, S=Arizona, O=OPC Foundation, DC=localhost"
        )
        securityConfig.TrustedIssuerCertificates.StoreType <- "Directory"
        securityConfig.TrustedIssuerCertificates.StorePath <- "%CommonApplicationData%\\OPC Foundation\\pki\\issuer"
        securityConfig.TrustedPeerCertificates.StoreType <- "Directory"
        securityConfig.TrustedPeerCertificates.StorePath <- "%CommonApplicationData%\\OPC Foundation\\pki\\trusted"

        securityConfig.RejectedCertificateStore <- CertificateStoreIdentifier()
        securityConfig.RejectedCertificateStore.StoreType <- "Directory"
        securityConfig.RejectedCertificateStore.StorePath <- "%CommonApplicationData%\\OPC Foundation\\pki\\rejected"
        securityConfig.AutoAcceptUntrustedCertificates <- true
        securityConfig.UseValidatedCertificates <- false
        config.SecurityConfiguration <- securityConfig

        // Transport Quotas
        let transportQuotas = TransportQuotas()
        transportQuotas.OperationTimeout <- 600000
        transportQuotas.MaxStringLength <- 1048576
        transportQuotas.MaxByteStringLength <- 1048576
        transportQuotas.MaxArrayLength <- 65535
        transportQuotas.MaxMessageSize <- 4194304
        transportQuotas.MaxBufferSize <- 65535
        transportQuotas.ChannelLifetime <- 300000
        transportQuotas.SecurityTokenLifetime <- 3600000
        config.TransportQuotas <- transportQuotas

        // Server Configuration
        let serverConfig = ServerConfiguration()
        serverConfig.BaseAddresses.Add("https://localhost:62562/Quickstarts/SimpleEventsServer")
        serverConfig.BaseAddresses.Add("opc.tcp://localhost:62563/Quickstarts/SimpleEventsServer")

        // Security Policies
        let securityPolicies = [
            ServerSecurityPolicy(
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            )
            ServerSecurityPolicy(
                SecurityMode = MessageSecurityMode.None,
                SecurityPolicyUri = SecurityPolicies.None
            )
            ServerSecurityPolicy(
                SecurityMode = MessageSecurityMode.Sign,
                SecurityPolicyUri = SecurityPolicies.Basic256Sha256
            )
            ServerSecurityPolicy(
                SecurityMode = MessageSecurityMode.Sign,
                SecurityPolicyUri = ""
            )
            ServerSecurityPolicy(
                SecurityMode = MessageSecurityMode.SignAndEncrypt,
                SecurityPolicyUri = ""
            )
        ]
        securityPolicies |> List.iter serverConfig.SecurityPolicies.Add

        // User Token Policies
        serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.Anonymous))
        serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.UserName))
        serverConfig.UserTokenPolicies.Add(UserTokenPolicy(UserTokenType.Certificate))

        serverConfig.DiagnosticsEnabled <- false
        serverConfig.MaxSessionCount <- 100
        serverConfig.MinSessionTimeout <- 10000
        serverConfig.MaxSessionTimeout <- 3600000
        serverConfig.MaxBrowseContinuationPoints <- 10
        serverConfig.MaxQueryContinuationPoints <- 10
        serverConfig.MaxHistoryContinuationPoints <- 100
        serverConfig.MaxRequestAge <- 600000
        serverConfig.MinPublishingInterval <- 100
        serverConfig.MaxPublishingInterval <- 3600000
        serverConfig.PublishingResolution <- 50
        serverConfig.MaxSubscriptionLifetime <- 3600000
        serverConfig.MaxMessageQueueSize <- 10
        serverConfig.MaxNotificationQueueSize <- 100
        serverConfig.MaxNotificationsPerPublish <- 1000
        serverConfig.MinMetadataSamplingInterval <- 1000

        // Sampling Rates
        serverConfig.AvailableSamplingRates.Add(SamplingRateGroup(Start = 5, Increment = 5, Count = 20))
        serverConfig.AvailableSamplingRates.Add(SamplingRateGroup(Start = 100, Increment = 100, Count = 4))
        serverConfig.AvailableSamplingRates.Add(SamplingRateGroup(Start = 500, Increment = 250, Count = 2))
        serverConfig.AvailableSamplingRates.Add(SamplingRateGroup(Start = 1000, Increment = 500, Count = 20))

        serverConfig.MaxRegistrationInterval <- 30000
        serverConfig.NodeManagerSaveFile <- "Quickstarts.SimpleEventsServer.nodes.xml"
        config.ServerConfiguration <- serverConfig

        // Trace Configuration
        let traceConfig = TraceConfiguration()
        traceConfig.OutputFilePath <- "%CommonApplicationData%\\OPC Foundation\\Logs\\Quickstarts.SimpleEventsServer.log.txt"
        traceConfig.DeleteOnLoad <- true
        traceConfig.TraceMasks <- 515
        config.TraceConfiguration <- traceConfig

        config



