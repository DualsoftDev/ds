namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module CpuLoader =
    let private IsSpec (v:Vertex) (vaild:ConvertType) =
        let isVaildVertex =
            match v with
            | :? Real   -> vaild.HasFlag(RealInFlow)
            | :? RealExF -> vaild.HasFlag(RealExFlow)
            | :? RealExS -> vaild.HasFlag(RealExSystem)
            | :? Call as c  ->
                match c.Parent with
                | DuParentFlow f-> vaild.HasFlag(CallInFlow)
                | DuParentReal r-> vaild.HasFlag(CallInReal)

            | :? Alias as a  ->
                 match a.Parent with
                 | DuParentFlow f->
                     match a.TargetWrapper with
                     |  DuAliasTargetReal   ar -> vaild.HasFlag(AliasRealInFlow)
                     |  DuAliasTargetRealExFlow ao -> vaild.HasFlag(AliasRealExInFlow)
                     |  DuAliasTargetRealExSystem ao -> vaild.HasFlag(AliasRealExInSystem)
                     |  DuAliasTargetCall   ac -> vaild.HasFlag(AliasCallInFlow)
                 | DuParentReal r->
                     match a.TargetWrapper with
                     | DuAliasTargetReal   ar       -> failwithlog $"Error {get_current_function_name()}"
                     | DuAliasTargetRealExFlow ao   -> failwithlog $"Error {get_current_function_name()}"
                     | DuAliasTargetRealExSystem ao -> failwithlog $"Error {get_current_function_name()}"
                     | DuAliasTargetCall   ac -> vaild.HasFlag(AliasCallInReal)
            |_ -> failwithlog $"Error {get_current_function_name()}"

        isVaildVertex

    ///Vertex 타입이 Spec에 해당하면 적용
    let private applyVertexSpec(v:Vertex) =
        let vm = v.TagManager :?> VertexManager
        [
            if IsSpec v RealInFlow then
                yield! vm.S1_RealRGFH()

                yield vm.P1_RealStartPort()
                yield vm.P2_RealResetPort()
                yield vm.P3_RealEndPort()

                yield vm.M1_OriginMonitor()
                yield vm.M5_RealErrorTXMonitor()
                yield vm.M6_RealErrorRXMonitor()

                yield vm.R1_RealInitialStart()
                yield vm.R2_RealJobComplete()
                yield vm.R3_RealStartPoint()

                yield! vm.D1_DAGHeadStart()
                yield! vm.D2_DAGTailStart()
                yield! vm.D3_DAGCoinComplete()

            if IsSpec v InFlowAll then
                yield! vm.F1_RootStart()
            //RealInFlow ||| RealExFlow ||| AliasRealInFlow ||| AliasRealExInFlow
            if IsSpec v RealNIndirectReal then
                yield! vm.F2_RootReset()

            if IsSpec v InFlowWithoutReal then
                yield vm.F3_RootCoinRelay()

            if IsSpec v (CallInReal ||| CallInFlow) then
                yield! vm.C1_CallPlanSend()
                yield! vm.C2_CallActionOut()
                yield! vm.C3_CallPlanReceive()
                yield! vm.C4_CallActionIn()
                yield! vm.M3_CallErrorTXMonitor()
                yield vm.M4_CallErrorRXMonitor()

            if IsSpec v CoinTypeAll then
                yield! vm.S2_CoinRGFH()

            if IsSpec v (RealInFlow ||| CoinTypeAll)  then
                yield vm.M2_PauseMonitor()

            if IsSpec v AliasRealExInSystem then
                yield! vm.L1_LinkStart()

        ]

    let private applySystemSpec(s:DsSystem) =
        [
            yield! s.B1_ButtonOutput()
            yield! s.B2_ModeLamp()
            yield! s.Y1_SystemBitSetFlow()
            yield! s.Y2_SystemConditionReady()
            yield! s.Y3_SystemConditionDrive()
        ]

    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow) =
        [
            yield f.O1_ReadyOperationState()
            yield f.O2_AutoOperationState()
            yield f.O3_ManualOperationState()
            yield f.O4_EmergencyOperationState()
            yield f.O5_StopOperationState()
            yield f.O6_DriveOperationMode()
            yield f.O7_TestOperationMode()
            yield f.O8_IdleOperationMode()
        ]

    let private applyTimerCounterSpec(s:DsSystem) =
        [
            yield! s.T1_DelayCall()
        ]



    let private convertSystem(sys:DsSystem) =
        Runtime.System <- sys

        //DsSystem 물리 IO 생성
        sys.GenerationIO()

        [
            //시스템 적용
            yield! applySystemSpec sys

            //Flow 적용
            for f in sys.Flows do
                yield! applyOperationModeSpec f


            //Vertex 적용
            for v in sys.GetVertices() do
                yield! applyVertexSpec v

            yield! applyTimerCounterSpec sys
        ]

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
                | (:? RealExS | :? RealExF | :? Call | :? Alias)
                    -> v.TagManager <-  VertexMCoin(v)
                | _ -> failwithlog (get_current_function_name()))

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
            | ActivePou    (s, p) -> s
            | DevicePou    (d, p) -> d.ReferenceSystem
            | ExternalPou  (e, p) -> e.ReferenceSystem
        member x.CommentedStatements() =
            match x with
            | ActivePou    (s, p) -> p
            | DevicePou    (d, p) -> p
            | ExternalPou  (e, p) -> p
        member x.TaskName() =
            match x with
            | ActivePou    (s, p) -> "Active"
            | DevicePou    (d, p) -> "Device"
            | ExternalPou  (e, p) -> "ExternalCpu"
        member x.IsActive   = match x with | ActivePou (s, p) -> true |_ -> false
        member x.IsDevice   = match x with | DevicePou (s, p) -> true |_ -> false
        member x.IsExternal = match x with | ExternalPou (s, p) -> true |_ -> false


    let testReadyAutoDrive(system:DsSystem) =
        system._auto.Value <- true
        system._ready.Value <- true
        system._drive.Value <- true


    [<Extension>]
    type Cpu =
        [<Extension>]
        static member LoadStatements (system:DsSystem, storages:Storages) =
            applyTagManager (system, storages)
            let result =
                //자신(Acitve)이 Loading 한 system을 재귀적으로 한번에 가져와 CPU 변환
                system.GetRecursiveLoadeds()
                |>Seq.map(fun s->
                    match s  with
                    | :? Device as d ->   DevicePou (d, convertSystem(d.ReferenceSystem))
                    | :? ExternalSystem as e ->  ExternalPou (e, convertSystem(e.ReferenceSystem))
                    | _ -> failwithlog (get_current_function_name())
                    )
                //자신(Acitve) system을  CPU 변환
                |>Seq.append [ActivePou (system, convertSystem(system))]

            result
