namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq

[<AutoOpen>]
module CpuLoader =
    //프로그램 내려가는 그룹.  *P*rogram *O*rganization *U*nit Gen
    type PouGen =
    | ActivePou    of DsSystem       * CommentedStatement list
    | DevicePou    of Device         * CommentedStatement list
    | ExternalPou  of ExternalSystem * CommentedStatement list
        member x.ToSystem() =
            match x with
            | ActivePou    (s, _p) -> s
            | DevicePou    (d, _p) -> d.ReferenceSystem
            | ExternalPou  (e, _p) -> e.ReferenceSystem

        member x.ToExternalSystem() =
            match x with
            | ActivePou    (_, _p) -> None
            | DevicePou    (_, _p) -> None
            | ExternalPou  (e, _p) -> Some e

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


    let checkCausalModel(system:DsSystem) =
        //root Edge target에 Call/AliasCall 허용 금지(Real로 반드시 부모설정후 인과처리)
        let rootEdges  = system.Flows.Collect(fun f->f.Graph.Edges)
        for edge in rootEdges do
            match edge.Target with
            | :? Real -> ()
            | :? Call as c  ->
                match c.Parent with
                | DuParentReal _ -> ()
                | DuParentFlow _ -> failwithlog $"Call vertex can't using Target [check : {edge.ToText()}]"

            | :? Alias as a  ->
                match a.Parent with
                | DuParentReal _ -> ()
                | DuParentFlow _ ->
                    match a.TargetWrapper with
                    | DuAliasTargetReal _ -> ()
                    | DuAliasTargetCall _ -> ()//failwithlog $"AliasCall vertex can't using Target [check : {edge.ToText()}]"

            |_ -> failwithlog $"Error {getFuncName()}"



    let applyTagManager(activeSys:DsSystem, storages:Storages, modelCnf:ModelConfig) =
        let createTagM  (sys:DsSystem) =
            debugfn($"createTagM System: {sys.Name}")

            RuntimeDS.ReplaceSystem(sys)
            let isActive = sys = activeSys
            sys.TagManager <- SystemManager(sys, storages, modelCnf.HwTarget, modelCnf.TimeoutCall)
            sys.Variables.Iter(fun v-> v.TagManager <- VariableManager(v, sys))
            sys.ActionVariables.Iter(fun a-> a.TagManager <- ActionVariableManager(a, sys))
            sys.Flows.Iter(fun f->f.TagManager <- FlowManager(f, isActive, activeSys))
            sys.ApiItems.Iter(fun a->a.TagManager <- ApiItemManager(a))
            sys.TaskDevs.Iter(fun td->td.TagManager <- TaskDevManager(td, sys))
            sys.GetVertices().Iter(fun v->
                match v with
                | :? Real ->
                    v.TagManager <- RealVertexTagManager(v, isActive, modelCnf.RuntimeMode)
                | (:? Call | :? Alias) ->
                    v.TagManager <-  CoinVertexTagManager(v, isActive, modelCnf.RuntimeMode)
                | _ ->
                    failwithlog (getFuncName()))


        createTagM activeSys //  root와 본인과 같음
        activeSys.GetRecursiveLoadedSystems()
            .Distinct()
            .Iter(createTagM)
        RuntimeDS.ReplaceSystem activeSys //active로 원위치


    type DsSystem with
        /// DsSystem 으로부터 PouGen seq 생성
        member sys.GeneratePOUs (storages:Storages) (modelCnf:ModelConfig) : PouGen seq =
            UniqueName.resetAll()
            applyTagManager (sys, storages,  modelCnf)

            let pous =
                //자신(Acitve)이 Loading 한 system을 재귀적으로 한번에 가져와 CPU 변환
                let passiveSystems = sys.GetRecursiveLoadeds()
                let activeSys = sys
                passiveSystems
                |> Seq.distinctBy(fun f->f.ReferenceSystem)
                |> Seq.map(fun s ->
                    try
                        match s with
                        | :? Device as d         -> DevicePou   (d, d.ReferenceSystem.GenerateStatements(activeSys, modelCnf))
                        | :? ExternalSystem as e -> ExternalPou (e, e.ReferenceSystem.GenerateStatements(activeSys, modelCnf))
                        | _ -> failwithlog (getFuncName())
                    with e -> failwithlog $"{e.Message}\r\n\r\n{s.AbsoluteFilePath}"
                    )
                //자신(Acitve) system을  CPU 변환
                |> Seq.append [ActivePou (sys, sys.GenerateStatements(activeSys, modelCnf))]

            pous

