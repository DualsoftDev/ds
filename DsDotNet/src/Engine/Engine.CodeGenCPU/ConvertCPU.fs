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
                yield! vm.M5_RealErrorTotalMonitor() 

                yield vm.R1_RealInitialStart()
                yield! vm.R2_RealJobComplete()
                yield vm.R3_RealStartPoint()
                yield vm.R4_RealSync() 
                //yield vm.R5_RealDataMove() 
                
                yield! vm.D1_DAGHeadStart()
                yield! vm.D2_DAGTailStart()
                yield! vm.D3_DAGCoinEnd()
                yield! vm.D4_DAGCoinReset()

                yield! vm.F1_RootStart()
                yield! vm.F2_RootReset()

            if IsSpec (v, CallInFlow ||| RealExSystem ||| RealExFlow, AliasNotCare) then
                yield vm.F3_VertexEndWithOutReal()

            if IsSpec (v, CallInReal , AliasFalse) then
                
                yield vm.C1_CallMemo() 
                yield! vm.M3_CallErrorTXMonitor() 
                yield! vm.M4_CallErrorRXMonitor() 
                yield vm.M6_CallErrorTotalMonitor() 
                

            if IsSpec (v, VertexAll, AliasNotCare) then
                yield vm.M2_PauseMonitor()
                yield! vm.S1_RGFH()
       

        ]

    let private applySystemSpec(s:DsSystem) =
        [
            yield! s.B1_HWButtonOutput()
            yield! s.B3_HWModeLamp()
            
            if RuntimeDS.Package <> RuntimePackage.LightPLC 
            then
                yield! s.B2_SWButtonOutput()
                yield! s.B4_SWModeLamp()

            yield s.Y2_SystemPause()
            yield! s.Y3_SystemState()

            if RuntimeDS.Package.IsPackagePLC() then
                yield! s.E1_PLCNotFunc()
            if RuntimeDS.Package = RuntimePackage.LightPLC then
                yield! s.E2_LightPLCOnly()

        ]



    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow) =
        [
            yield f.O1_IdleOperationMode()
            yield f.O2_AutoOperationMode()
            yield f.O3_ManualOperationMode()
            yield f.O4_EmergencyOperationState()
            yield f.O5_StopOperationState()
            yield f.O6_DriveOperationMode()
            yield f.O7_TestOperationMode()
            yield f.O8_ReadyOperationState()
            yield f.O9_originOperationMode()
            yield f.O10_homingOperationMode()
            yield f.O11_goingOperationMode()
        ]

    let private applyFlowMonitorSpec(f:Flow) =
        [
            yield f.F1_FlowError()
            yield f.F2_FlowConditionErr()
            yield f.F3_FlowPause()
            
        ]

    let private callPlanAction(s:DsSystem) =
        [
            let apis = s.GetDistinctApis()
            let coinAll = s.GetVerticesOfCoins()  
            let apiCoinsSet = apis.Select(fun a-> a, coinAll.Where(fun c->c.TargetJob.ApiDefs.Contains(a)))
            
            for (api, coins) in apiCoinsSet do
                let am = api.TagManager :?> ApiItemManager
                yield am.A2_PlanReceive(s)

                if coins.any()
                then
                    yield am.A1_PlanSend(s, coins)
                    yield am.A3_SensorLinking(s, coins)
                    yield am.A4_SensorLinked(s, coins)
                    yield! am.A5_ActionOut(coins)
        ]
     
    let private applyTimerCounterSpec(s:DsSystem) =
        [
            yield! s.T1_DelayCall()
        ]
     
            

    let convertSystem(sys:DsSystem, isActive:bool) =
        RuntimeDS.System <- sys

        //시뮬레이션 주소 자동할당
        if RuntimeDS.Package.IsPackageSIM()  then setSimulationAddress(sys)
        //DsSystem 물리 IO 생성
        sys.GenerationIO()

        checkErrNullAddress(sys)
        checkErrHWItem(sys)
        checkErrApi(sys)


        if isActive //직접 제어하는 대상만 정렬(원위치) 정보 추출
        then sys.GenerationOrigins()
        [
                //Active 시스템 적용  //test ahn loaded는 제외 성능 고려해서 다시 구현
            if isActive
            then 
                yield! applySystemSpec sys

            if RuntimeDS.Package = RuntimePackage.LightPLC 
            then
                checkErrLightPLC(sys)
            else
                yield! sys.Y1_SystemBitSetFlow()

                //Flow 적용
            for f in sys.Flows do
                yield! applyOperationModeSpec f
                yield! applyFlowMonitorSpec f


                //Vertex 적용
            for v in sys.GetVertices() do
                yield! applyVertexSpec v

            yield! callPlanAction sys

            yield! applyTimerCounterSpec sys
        ]
