namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices
open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module CpuLoader =

    let checkCausalModel(system:DsSystem) =
        //root Edge target에 Call/AliasCall 허용 금지(Real로 반드시 부모설정후 인과처리)
        let rootEdges  = system.Flows.Collect(fun f->f.Graph.Edges)
        for edge in rootEdges do
            match edge.Target with
            | :? Real            -> ()
            | :? RealExF         -> ()
            | :? CallSys         -> ()
            | :? CallDev as c  ->
                match c.Parent with
                | DuParentReal _ -> ()
                | DuParentFlow _ -> failwithlog $"Call vertex can't using Target [check : {edge.ToText()}]"

            | :? Alias as a  ->
                    match a.Parent with
                    | DuParentReal _ -> ()
                    | DuParentFlow _ ->
                        match a.TargetWrapper with
                        | DuAliasTargetReal _         -> ()
                        | DuAliasTargetRealExFlow _   -> ()
                        | DuAliasTargetRealExSystem _ -> ()
                        | DuAliasTargetCall _ -> failwithlog $"AliasCall vertex can't using Target [check : {edge.ToText()}]"

            |_ -> failwithlog $"Error {getFuncName()}"

    let applyTagManager(system:DsSystem, storages:Storages) =
        let createTagM (sys:DsSystem) =
            Runtime.System <- sys

            sys.TagManager <- SystemManager(sys, storages)
            sys.Flows.Iter(fun f->f.TagManager <- FlowManager(f))
            sys.ApiItems.Iter(fun a->a.TagManager <- ApiItemManager(a))
            sys.GetVertices().Iter(fun v->
                match v with
                | :? Real
                    ->  v.TagManager <- VertexMReal(v)
                | (:? CallSys | :? RealExF | :? CallDev | :? Alias)
                    -> v.TagManager <-  VertexMCoin(v)
                | _ -> failwithlog (getFuncName()))

        let rec tagManagerBuild(sys:DsSystem)  =
            createTagM (sys)
            sys.LoadedSystems
                  .Iter(fun s->  tagManagerBuild (s.ReferenceSystem))

        tagManagerBuild (system)



    //프로그램 내려가는 그룹
    type PouGen =
    | ActivePou    of DsSystem * CommentedStatement list
    | DevicePou    of Device * CommentedStatement list
    | ExternalPou  of ExternalSystem * CommentedStatement list
        member x.ToSystem() =
            match x with
            | ActivePou    (s, _p) -> s
            | DevicePou    (d, _p) -> d.ReferenceSystem
            | ExternalPou  (e, _p) -> e.ReferenceSystem
        member x.CommentedStatements() =
            match x with
            | ActivePou    (_s, p) -> p
            | DevicePou    (_d, p) -> p
            | ExternalPou  (_e, p) -> p
        member x.TaskName() =
            match x with
            | ActivePou   _ -> "Active"
            | DevicePou   _ -> "Devices"
            | ExternalPou _ -> "ExternalCpu"
        member x.IsActive   = match x with | ActivePou   _ -> true | _ -> false
        member x.IsDevice   = match x with | DevicePou   _ -> true | _ -> false
        member x.IsExternal = match x with | ExternalPou _ -> true | _ -> false



    [<Extension>]
    type Cpu =
        [<Extension>]
        static member LoadStatements (system:DsSystem, storages:Storages) =
            UniqueName.resetAll()
            applyTagManager (system, storages)
            let result =
                //자신(Acitve)이 Loading 한 system을 재귀적으로 한번에 가져와 CPU 변환
                system.GetRecursiveLoadeds()
                |>Seq.map(fun s->
                    match s  with
                    | :? Device as d         -> DevicePou   (d, convertSystem(d.ReferenceSystem, false))
                    | :? ExternalSystem as e -> ExternalPou (e, convertSystem(e.ReferenceSystem, false))
                    | _ -> failwithlog (getFuncName())
                    )
                //자신(Acitve) system을  CPU 변환
                |>Seq.append [ActivePou (system, convertSystem(system, true))]

            result


