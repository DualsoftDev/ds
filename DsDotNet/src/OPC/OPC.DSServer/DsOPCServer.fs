namespace OPC.DSServer

open System
open System.Collections.Generic
open Opc.Ua
open Opc.Ua.Configuration
open Opc.Ua.Server
open Engine.Core.Interface
open Engine.Core

type DsOPCServer(dsSys: DsSystem) =
    inherit StandardServer()

    // NodeManager를 생성하여 주소 공간 관리
    override this.CreateMasterNodeManager(server: IServerInternal, configuration: ApplicationConfiguration) =
        let nodeManager = new DsNodeManager(server, configuration, dsSys)
        new MasterNodeManager(server, configuration, null, [|nodeManager:> INodeManager|])
