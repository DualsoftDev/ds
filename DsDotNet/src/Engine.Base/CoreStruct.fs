// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CoreStruct =

    [<DebuggerDisplay("{name}")>]
    type DsSystem(name:string, model:DsModel)  =
        inherit SysBase(name)
        let rootFlows = HashSet<RootFlow>()

        member x.Model = model
        member x.RootFlows = rootFlows

    and
        [<DebuggerDisplay("{name}")>]
        DsModel(name:string) =
            let systems = HashSet<DsSystem>()
            let cpus = HashSet<DsCpu>()
            let vpss = HashSet<RootFlow>()
            member x.Systems = systems
            member x.Cpus = cpus
            /// <summary> 가상 부모 목록.  debugging 용 </summary>
            member x.VPSs = vpss
            


            
