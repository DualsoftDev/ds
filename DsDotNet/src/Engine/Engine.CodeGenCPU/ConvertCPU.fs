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
                
                yield! vm.M3_CallErrorTXMonitor() 
                yield! vm.M4_CallErrorRXMonitor() 
                yield vm.M6_CallErrorTotalMonitor() 

                yield! vm.C2_ActionOut()

            if IsSpec (v, CallInReal , AliasNotCare) then
                yield vm.C1_CallMemo() 

            if IsSpec (v, VertexAll, AliasNotCare) then
                yield vm.M2_PauseMonitor()
                yield! vm.S1_RGFH()
        ]

    let private applySystemSpec(s:DsSystem) =
        [
            yield! s.B1_HWButtonOutput()
            yield! s.B3_HWModeLamp()
            
            yield s.Y2_SystemPause()
            yield! s.Y3_SystemState()
            yield! s.Y4_SystemConditionError()
            yield! s.Y5_SystemEmgAlramError()
            

            if RuntimeDS.Package.IsPackagePLC() || RuntimeDS.Package.IsPackageEmulation() then
                yield! s.E1_PLCNotFunc(RuntimeDS.Package.IsPackageEmulation())
                yield! s.E2_LightPLCOnly()
            else  
                yield! s.B2_SWButtonOutput()
                yield! s.B4_SWModeLamp() 
        ]



    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow) =
        [
            yield f.O1_IdleOperationMode()
            yield f.O2_AutoOperationMode()
            yield f.O3_ManualOperationMode()
            yield f.ST1_originState()
            yield f.ST2_homingState()
            yield f.ST3_goingState()
            yield f.ST4_EmergencyState()
            yield f.ST5_ErrorState()
            yield f.ST6_DriveState()
            yield f.ST7_TestState()
            yield f.ST8_ReadyState()
        ]

    let private applyFlowMonitorSpec(f:Flow) =
        [
            yield f.F1_FlowError()
            yield f.F2_FlowConditionErr()
            yield f.F3_FlowPause()
            
        ]
        
    let private apiPlanSync(s:DsSystem) =
        [
            let apis = s.GetDistinctApis()
            let coinAll = s.GetVerticesOfCoins()  
            let apiCoinsSet =
                apis.Select(fun a->
                    a, 
                        coinAll.Where(fun f->
                        match f with
                        | :? Call as c->  c.TargetJob.ApiDefs.Contains(a)
                        | :? Alias as al->  al.TargetWrapper.CallTarget().Value.TargetJob.ApiDefs.Contains(a)
                        |_ -> false
                    )
                )
            
            for (api, coins) in apiCoinsSet do
                let am = api.TagManager :?> ApiItemManager
                yield am.A2_PlanReceive(s)

                if coins.any()
                then
                    yield am.A1_PlanSend(s, coins)
                    yield am.A3_SensorLinking(s, coins.OfType<Call>())
                    yield am.A4_SensorLinked(s, coins.OfType<Call>())
        ]

    let private emulationDevice(s:DsSystem) =
        [
            yield s.SetFlagForEmulation()

            let coins = s.GetVerticesOfCoins()  
            let jobs = coins.OfType<Call>().Select(fun c-> c.TargetJob).Distinct()
            for (notFunc, dts) in jobs.Select(fun j-> (j.Func |> hasNot), j.DeviceDefs) do
                for dt in dts do
                if dt.InTag.IsNonNull() then  
                    yield dt.SensorEmulation(s, notFunc)
        ]
     
    let private applyTimerCounterSpec(s:DsSystem) =
        [
            yield! s.T1_DelayCall()
        ]
     
            

    let convertSystem(sys:DsSystem, isActive:bool) =
        RuntimeDS.System <- sys

  
      
        if isActive //직접 제어하는 대상만 정렬(원위치) 정보 추출
        then 
             sys.GenerationOrigins()
             sys.GenerationMemory()
              //DsSystem 물리 IO 생성
             sys.GenerationIO()

             // Package 타입별 에러체크
             match   RuntimeDS.Package with
             | PC
             | PLC ->  checkErrNullAddress(sys)
             | Emulation ->  ()
             | Simulation -> () 
             | Developer ->  ()      
             
             checkErrHWItem(sys)
             checkErrApi(sys)


        else checkErrRealResetExist(sys)
       

        [
            match   RuntimeDS.Package with
            | PC ->  ()
            | PLC ->  ()
            | Emulation ->  
                            yield! emulationDevice sys
            | Simulation -> setSimulationAddress(sys) //시뮬레이션 주소 자동할당
                            yield! sys.Y1_SystemBitSetFlow()
            | Developer ->  ()


            //Active 시스템 적용 
            if isActive
            then   //test ahn loaded는 제외 성능 고려해서 다시 구현
                yield! applySystemSpec sys

            //Flow 적용
            for f in sys.Flows do
                yield! applyOperationModeSpec f
                yield! applyFlowMonitorSpec f

            //Vertex 적용
            for v in sys.GetVertices() do
                yield! applyVertexSpec v

            //Api 적용 
            yield! apiPlanSync sys

            //Timer Count 적용 
            yield! applyTimerCounterSpec sys
        ]
