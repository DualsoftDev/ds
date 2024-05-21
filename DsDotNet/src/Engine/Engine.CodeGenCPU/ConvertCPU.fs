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
                yield! vm.R5_DummyDAGCoils() 
                //yield vm.R6_RealDataMove() 
                yield! vm.R7_RealGoingOriginError() 
                
                
                yield! vm.D1_DAGHeadStart()
                yield! vm.D2_DAGTailStart()
                yield! vm.D3_DAGCoinEnd()
                yield! vm.D4_DAGCoinReset()

                yield! vm.F1_RootStart()
                yield! vm.F2_RootReset()

            if IsSpec (v, RealExSystem ||| RealExFlow, AliasNotCare) then
                    yield vm.F3_VertexEndWithOutReal()    

            if IsSpec (v, CallInFlow, AliasNotCare) && v.GetPureCall().Value.IsOperator then
                    yield vm.F4_CallOperatorEnd()

            if IsSpec (v, CallInReal , AliasFalse) then
                
                yield! vm.M3_CallErrorTXMonitor() 
                yield! vm.M4_CallErrorRXMonitor() 
                yield vm.M6_CallErrorTotalMonitor() 
         

                
            if IsSpec (v, CallInReal, AliasNotCare) then
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
                //yield! s.E1_PLCNotFunc(RuntimeDS.Package.IsPackageEmulation()) //test ahn  param 동작확인 필요
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
            let apiCoinsSet =  s.GetApiCoinsSet()
            for (api, coins) in apiCoinsSet do
                let am = api.TagManager :?> ApiItemManager
                yield am.A2_PlanReceive(s)

                if coins.any()
                then
                    yield am.A1_PlanSend(s, coins)
                    yield am.A3_SensorLinking(s, coins.OfType<Call>())
                    yield am.A4_SensorLinked(s, coins.OfType<Call>())
        ]

    let private funcCall(s:DsSystem) =
        let pureOperatorFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsPureOperator)

        let flowOperatorFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsOperator && not (c.IsPureOperator))

        let pureCommandFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsPureCommand)
                          
        [

            for coin in pureOperatorFuncs do
                yield! coin.VC.C1_DoOperator()   //Operator 함수는 Call 수행후 연산결과를 PEFunc에 반영
                    
            for coin in pureCommandFuncs do
                yield! coin.VC.C2_DoCommand()  
                
            for coin in flowOperatorFuncs do
                yield coin.VC.C3_DoOperatorDevice()
        ]

    let private applyVariables(s:DsSystem) =
        [
            for v in s.Variables do
                if v.VariableType = Immutable
                then yield v.VM.V1_ConstMove(s)

            for v in s.ActionVariables do
                yield v.VM.V2_ActionVairableMove(s)
        ]   
            
    let private applyJob(s:DsSystem) =
        [
            let coins = s.GetVerticesOfCoins()  
            let jobs = coins.OfType<Call>()
                            .Where(fun c-> c.IsJob)
                            .Select(fun c-> c.TargetJob).Distinct()
            for j in jobs do
                yield! j.J1_JobActionOuts()
        ]
        
    let private emulationDevice(s:DsSystem) =
        [
            yield s.SetFlagForEmulation()

            let coins = s.GetVerticesOfCoins()  
            let jobs = coins.OfType<Call>()
                            .Select(fun c-> c.TargetJob).Distinct()
            for job, devs in jobs.Select(fun j-> j, j.DeviceDefs) do
                for dev in devs do
                    if dev.InTag.IsNonNull() then  
                        yield dev.SensorEmulation(s, job)
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
             | PC  -> ()
             | PLC 
             | Emulation ->  
                        checkDuplicatesNNullAddress(sys)
                        checkErrExternalStartRealExist(sys)

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
                           if isActive  then  yield! emulationDevice sys
            | Simulation -> setSimulationAddress(sys) //시뮬레이션 주소 자동할당
                            yield! sys.Y1_SystemBitSetFlow()
            | Developer ->  ()

            //Variables  적용 
            yield! applyVariables sys

            //Active 시스템 적용 
            if isActive
            then   
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
            
            //funcCall 적용 
            yield! funcCall sys

            //allpyJob 적용 
            yield! applyJob sys
        ]
