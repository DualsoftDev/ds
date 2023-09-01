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
        let isVaildVertex =
            match v with
            | :? Real            -> aliasNoSpec && vaild.HasFlag(RealInFlow)
            | :? RealExF         -> aliasNoSpec && vaild.HasFlag(RealExFlow)
            | :? CallSys         -> aliasNoSpec && vaild.HasFlag(RealExSystem)
            | :? CallDev as c  ->
                match c.Parent with
                | DuParentFlow _ -> aliasNoSpec && vaild.HasFlag(CallInFlow)
                | DuParentReal _ -> aliasNoSpec && vaild.HasFlag(CallInReal)

            | :? Alias as a  ->
                 match a.Parent with
                 | DuParentFlow _ ->
                     match a.TargetWrapper with
                     |  DuAliasTargetReal _         -> aliasSpec && vaild.HasFlag(RealInFlow)
                     |  DuAliasTargetRealExFlow _   -> aliasSpec && vaild.HasFlag(RealExFlow)
                     |  DuAliasTargetRealExSystem _ -> aliasSpec && vaild.HasFlag(RealExSystem)
                     |  DuAliasTargetCall _         -> aliasSpec && vaild.HasFlag(CallInFlow)
                 | DuParentReal _ ->
                     match a.TargetWrapper with
                     | DuAliasTargetReal _         -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetRealExFlow _   -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetRealExSystem _ -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetCall _         -> aliasSpec &&  vaild.HasFlag(CallInReal)
            |_ -> failwithlog $"Error {getFuncName()}"

        isVaildVertex

    ///Vertex 타입이 Spec에 해당하면 적용
    let private applyVertexSpec(v:Vertex) =
        let vm = v.TagManager :?> VertexManager
        [
            if IsSpec (v, RealInFlow, AliasFalse) then
                yield! vm.S1_RealRGFH()

                yield vm.P1_RealStartPort()
                yield vm.P2_RealResetPort()
                yield vm.P3_RealEndPort()

                yield vm.M1_OriginMonitor()
                yield vm.M5_RealErrorTXMonitor()
                yield vm.M6_RealErrorRXMonitor()

                yield vm.R1_RealInitialStart()
                yield vm.R2_RealJobComplete()
                yield vm.R2_1_GoingRelayGroup();
                yield vm.R3_RealStartPoint()

                yield! vm.D1_DAGHeadStart()
                yield! vm.D2_DAGTailStart()
                yield! vm.D3_DAGCoinRelay()
                yield! vm.D4_DAGCoinReset()

            if IsSpec (v, RealInFlow, AliasFalse) then
                yield! vm.F1_RootStart()
                yield! vm.F2_RootReset()
              //  yield vm.F3_RootGoingPulse()
                yield! vm.F4_RootGoingRelay()

            if IsSpec (v, CallInFlow ||| RealExSystem ||| RealExFlow, AliasNotCare) then
                yield vm.F5_RootCoinRelay()

            if IsSpec (v, CallInFlow , AliasFalse) then
                yield! vm.C5_CallActionInRoot()

            if IsSpec (v, CallInReal , AliasFalse) then
                yield! vm.C1_CallPlanSend()
                yield! vm.C2_CallActionOut()
                yield! vm.C3_CallPlanReceive()
                yield! vm.C4_CallActionIn()
                //yield! vm.M3_CallErrorTXMonitor() //test ahn Real 기준으로 Coin 대상으로 다시 작성 필요
                //yield vm.M4_CallErrorRXMonitor()  //test ahn Real 기준으로 Coin 대상으로 다시 작성 필요

            if IsSpec (v, CallInReal ||| CallInFlow ||| RealExSystem ||| RealExFlow, AliasNotCare) then
                yield! vm.S2_CoinRGFH()

            if IsSpec (v, VertexAll, AliasNotCare) then
                yield vm.M2_PauseMonitor()
            //test ahn
            if IsSpec (v, RealExSystem, AliasNotCare) then
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




    let convertSystem(sys:DsSystem, isActive:bool) =
        RuntimeDS.System <- sys
        //DsSystem 물리 IO 생성
        sys.GenerationIO()

        if isActive //직접 제어하는 대상만 정렬(원위치) 정보 추출
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
