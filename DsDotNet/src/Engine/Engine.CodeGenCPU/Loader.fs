namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices
open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module CpuLoader =
    let private IsSpec (v:Vertex) (vaild:ConvertType) =
        let isVaildVertex =
            match v with
            | :? Real   -> vaild.HasFlag(RealInFlow)
            | :? RealExF -> vaild.HasFlag(RealExFlow)
            | :? CallSys -> vaild.HasFlag(RealExSystem)
            | :? CallDev as c  ->
                match c.Parent with
                | DuParentFlow _ -> vaild.HasFlag(CallInFlow)
                | DuParentReal _ -> vaild.HasFlag(CallInReal)

            | :? Alias as a  ->
                 match a.Parent with
                 | DuParentFlow _ ->
                     match a.TargetWrapper with
                     |  DuAliasTargetReal _         -> vaild.HasFlag(AliasRealInFlow)
                     |  DuAliasTargetRealExFlow _   -> vaild.HasFlag(AliasRealExInFlow)
                     |  DuAliasTargetRealExSystem _ -> vaild.HasFlag(AliasRealExInSystem)
                     |  DuAliasTargetCall _         -> vaild.HasFlag(AliasCallInFlow)
                 | DuParentReal _ ->
                     match a.TargetWrapper with
                     | DuAliasTargetReal _         -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetRealExFlow _   -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetRealExSystem _ -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetCall _         -> vaild.HasFlag(AliasCallInReal)
            |_ -> failwithlog $"Error {getFuncName()}"

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




    let private convertSystem(sys:DsSystem, isActive:bool) =
        Runtime.System <- sys
        sys._on.Value <- true
        //DsSystem 물리 IO 생성
        sys.GenerationIO()

        if isActive //직잡 제어하는 대상만 정렬(원위치) 정보 추출
        then sys.GenerationOrigins()

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
                | (:? CallSys | :? RealExF | :? CallDev | :? Alias)
                    -> v.TagManager <-  VertexMCoin(v)
                | _ -> failwithlog (getFuncName()))

        let rec tagManagerBuild(sys:DsSystem)  =
            createTagM (sys)
            sys.LoadedSystems
                  .Iter(fun s->  tagManagerBuild (s.ReferenceSystem))

        tagManagerBuild (system)



    let testAddressSetting (sys:DsSystem) =
        for j in sys.Jobs do
            for dev in j.DeviceDefs  do
            if dev.ApiItem.RXs.any() then  dev.InAddress <- "%MX777"
            if dev.ApiItem.TXs.any() then  dev.OutAddress <- "%MX888"

        for b in sys.Buttons do
            b.InAddress <- "%MX777"
            b.OutAddress <- "%MX888"

        for l in sys.Lamps do
            l.OutAddress <- "%MX888"

        for c in sys.Conditions do
            c.InAddress <- "%MX777"


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
                    | :? Device as d ->   DevicePou (d, convertSystem(d.ReferenceSystem, false))
                    | :? ExternalSystem as e ->  ExternalPou (e, convertSystem(e.ReferenceSystem, false))
                    | _ -> failwithlog (getFuncName())
                    )
                //자신(Acitve) system을  CPU 변환
                |>Seq.append [ActivePou (system, convertSystem(system, true))]

            result


