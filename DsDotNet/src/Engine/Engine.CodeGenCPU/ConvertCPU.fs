namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module ConvertCPU =
    let private IsSpec (v:Vertex, vaild:ConvertType, alias:ConvertAlias)=
        let aliasSpec    = alias = AliasTure  || alias = AliasNotCare
        let aliasNoSpec  = alias = AliasFalse || alias = AliasNotCare
        let isValidVertex =
            match v with
            | :? Real            -> aliasNoSpec && vaild.HasFlag(RealInFlow)
            | :? RealExF         -> aliasNoSpec && vaild.HasFlag(RealExFlow)
            | :? Call as c  ->
                match c.Parent with
                | DuParentFlow _ -> aliasNoSpec && vaild.HasFlag(CallInFlow)
                | DuParentReal _ -> aliasNoSpec && vaild.HasFlag(CallInReal)

            | :? Alias as a  ->
                 match a.Parent with
                 | DuParentFlow _ ->
                     match a.TargetWrapper with
                     |  DuAliasTargetReal _         -> aliasSpec && vaild.HasFlag(RealInFlow)
                     |  DuAliasTargetRealExFlow _   -> aliasSpec && vaild.HasFlag(RealExFlow)
                     |  DuAliasTargetCall _         -> aliasSpec && vaild.HasFlag(CallInFlow)
                 | DuParentReal _ ->
                     match a.TargetWrapper with
                     | DuAliasTargetReal _         -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetRealExFlow _   -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetCall _         -> aliasSpec &&  vaild.HasFlag(CallInReal)
            |_ -> failwithlog $"Error {getFuncName()}"

        isValidVertex

    ///Vertex 타입이 Spec에 해당하면 적용
    let private applyVertexSpec(v:Vertex) =
        let vm = v.TagManager :?> VertexManager
        [
            if IsSpec (v, RealInFlow, AliasFalse) then

                yield vm.M1_OriginMonitor()
                yield vm.M5_RealErrorTXMonitor()
                yield vm.M6_RealErrorRXMonitor()

                yield vm.R1_RealInitialStart()
                yield! vm.R2_RealJobComplete()
                yield vm.R3_RealStartPoint()

                yield! vm.D1_DAGHeadStart()
                yield! vm.D2_DAGTailStart()
                yield! vm.D3_DAGCoinEnd()
                yield! vm.D4_DAGCoinReset()

                yield! vm.F1_RootStart()
                yield! vm.F2_RootReset()

            if IsSpec (v, CallInFlow ||| RealExSystem ||| RealExFlow, AliasNotCare) then
                yield vm.F3_VertexEndWithOutReal()

            if IsSpec (v, CallInReal , AliasFalse) then
                yield! vm.C1_CallPlanSend()
                yield! vm.C2_CallActionOut()
                yield! vm.C3_CallPlanReceive()
                yield! vm.M3_CallErrorTXMonitor() 
                yield! vm.M4_CallErrorRXMonitor() 
                yield! vm.M7_CallErrorTRXMonitor() 
                

            if IsSpec (v, VertexAll, AliasNotCare) then
                yield vm.M2_PauseMonitor()
                yield! vm.S1_RGFH()
            //test ahn
            if IsSpec (v, RealExSystem, AliasNotCare) then
                yield! vm.L1_LinkStart()

        ]

    let private applySystemSpec(s:DsSystem) =
        [
            yield! s.B1_ButtonOutput()
            yield! s.B2_ModeLamp()
            yield! s.Y1_SystemBitSetFlow()
            yield s.Y2_SystemError()
            yield s.Y3_SystemPause()
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

    let private applyFlowMonitorSpec(f:Flow) =
        [
            yield f.F1_FlowError()
            yield f.F2_FlowPause()
        ]

    let private applyTimerCounterSpec(s:DsSystem) =
        [
            yield! s.T1_DelayCall()
        ]




    let convertSystem(sys:DsSystem, isActive:bool) =
        RuntimeDS.System <- sys
        //DsSystem 물리 IO 생성
        sys.GenerationIO()

        let nullTagJobs = sys.Jobs
                             .Where(fun j-> j.DeviceDefs.Where(fun f-> 
                                            f.InTag.IsNull() && f.ApiItem.RXs.any()
                                            ||f.OutTag.IsNull() && f.ApiItem.TXs.any()
                                            ).any())
        if nullTagJobs.any()
        then 
            let errJobs = StringExt.JoinWith(nullTagJobs.Select(fun j -> j.Name), "\n")
            failwithlogf $"Device 주소가 없습니다. \n{errJobs}"


        if isActive //직접 제어하는 대상만 정렬(원위치) 정보 추출
        then sys.GenerationOrigins()

        [
            //시스템 적용
            yield! applySystemSpec sys

            //Flow 적용
            for f in sys.Flows do
                yield! applyOperationModeSpec f
                yield! applyFlowMonitorSpec f


            //Vertex 적용
            for v in sys.GetVertices() do
                yield! applyVertexSpec v

            yield! applyTimerCounterSpec sys
        ]
