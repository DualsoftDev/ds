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
            | :? Call as c  ->
                match c.Parent with
                | DuParentFlow _ -> aliasNoSpec && vaild.HasFlag(CallInFlow)
                | DuParentReal _ -> aliasNoSpec && vaild.HasFlag(CallInReal)

            | :? Alias as a  ->
                 match a.Parent with
                 | DuParentFlow _ ->
                     match a.TargetWrapper with
                     |  DuAliasTargetReal _         -> aliasSpec && vaild.HasFlag(RealInFlow)
                     |  DuAliasTargetCall _         -> aliasSpec && vaild.HasFlag(CallInFlow)
                 | DuParentReal _ ->
                     match a.TargetWrapper with
                     | DuAliasTargetReal _         -> failwithlog $"Error {getFuncName()}"
                     | DuAliasTargetCall _         -> aliasSpec &&  vaild.HasFlag(CallInReal)
            |_ -> failwithlog $"Error {getFuncName()}"

        isValidVertex

    ///Vertex 타입이 Spec에 해당하면 적용
    let private applyVertexSpec(v:Vertex) =
        [
            if IsSpec (v, RealInFlow, AliasFalse) then
                let vr = v.TagManager :?> VertexMReal
                yield vr.M1_OriginMonitor()
                yield vr.E4_RealErrorTotalMonitor() 

                yield vr.R1_RealInitialStart()
                yield! vr.R2_RealJobComplete()
                yield vr.R3_RealStartPoint()
                yield vr.R4_RealLink() 
                yield! vr.R5_DummyDAGCoils() 
                //yield vm.R6_RealDataMove() 
                yield! vr.R7_RealGoingOriginError() 
                yield! vr.R8_RealGoingPulse() 
                yield! vr.R10_RealGoingTime() 
                yield! vr.R11_RealGoingMotion() 
                yield! vr.R12_RealGoingScript() 

                yield vr.F1_RootStart()
                yield vr.F2_RootReset()
                yield vr.F5_HomeCommand()


                yield! vr.D1_DAGHeadStart()
                yield! vr.D2_DAGTailStart()
                yield! vr.D3_DAGCoinEnd()
                yield! vr.D4_DAGCoinReset()



            if IsSpec (v, RealExSystem ||| RealExFlow, AliasNotCare) then
                let vm = v.TagManager :?> VertexMReal
                yield vm.F3_RealEndInFlow()    

            if IsSpec (v, CallInFlow, AliasNotCare) then
                let vc = v.TagManager :?> VertexMCall
                yield vc.F4_CallEndInFlow()

            if IsSpec (v, CallInReal , AliasFalse) then
                let vc = v.TagManager :?> VertexMCall
                yield! vc.E2_CallErrorTXMonitor() 
                yield! vc.E3_CallErrorRXMonitor() 
                yield vc.E5_CallErrorTotalMonitor() 
                
            if IsSpec (v, CallInReal, AliasNotCare) then
                let vc = v.TagManager :?> VertexMCall
                yield vc.C1_CallMemo() 
                
            if IsSpec (v, VertexAll, AliasNotCare) then
                let vm = v.TagManager :?> VertexManager
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

        ]



    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow) isActive =
        [
            yield f.O1_IdleOperationMode()
            yield f.O2_AutoOperationMode(isActive)
            yield f.O3_ManualOperationMode()
            yield f.ST1_OriginState()
            yield f.ST2_ReadyState(isActive)
            yield f.ST3_GoingState()
            yield f.ST4_EmergencyState()
            yield f.ST5_ErrorState()
            yield f.ST6_DriveState(isActive)
            yield f.ST7_TestState()
        ]

    let private applyFlowMonitorSpec(f:Flow) =
        [
            yield f.F1_FlowError()
            yield f.F2_FlowPause()
            yield f.F3_FlowReadyCondition()
            yield f.F4_FlowDriveCondition()
            
        ]
        
    //let private applyApiItem(s:DsSystem) = 
    //    [
    //        let devCallSet =  s.GetTaskDevCallSet()
    //        for (td, coins) in devCallSet do
    //            yield! td.A1_ApiSet(coins)
    //            yield! td.A2_ApiEnd(coins)
    //    ]

                        
        //let input = calls.First().GetEndAction(a.ApiItem)
        
        //let sets =
        //    if input.IsSome
        //    then
        //        linkExpr
        //        <&&>  
        //        (input.Value <&&> !@a.APIEND.Expr <&&> !@a.SL2.Expr)
        //    else 
        //        (activeSys._off.Expr)

        //(sets, activeSys._off.Expr) --| (a.SL1, getFuncName())

                        //let input = coins.OfType<Call>().First().GetEndAction(a.ApiItem)     


                        //yield am.A3_SensorLinking(s, coins.OfType<Call>())
                        //yield am.A4_SensorLinked(s, coins.OfType<Call>())
       // ]

    let getMasterJob (calls : Vertex seq) = 
        let pureCalls = 
            calls.OfType<Call>()@calls.OfType<Alias>().Choose(fun a->a.GetPureCall())
        
        if pureCalls.Select(fun c->c.TargetJob).Distinct().Count() > 1
        then failwithlog $"Error : {getFuncName()} {pureCalls.Select(fun c->c.TargetJob).Distinct().Count()}"

        pureCalls.First()  // 동일 job으로 선정해서 아무거나 가져옴

    let private applyTaskDev(s:DsSystem) = 
        [
            let devCallSet =  s.GetTaskDevCalls() //api는 job 기준으로 중복제거 
            for (td, calls) in devCallSet do
                let tm = td.TagManager :?> TaskDevManager
                let masterCall= getMasterJob(calls)
                yield! tm.TD1_PlanSend(s, calls)
                yield! tm.TD2_PlanReceive(s)
                yield! tm.TD3_PlanOutput(s)
                
                yield! tm.A1_ApiSet(masterCall)
                yield! tm.A2_ApiEnd()

            let devCallSet =  devCallSet.DistinctBy(fun (td, _c)-> td) //SensorLink는 taskDev 단위로 중복제거
            for (td, calls) in devCallSet do
                let tm = td.TagManager :?> TaskDevManager
                let masterCall= getMasterJob(calls) 
                yield! tm.A3_SensorLinking(masterCall)
                yield! tm.A4_SensorLinked(masterCall)
        ]


    let private funcCall(s:DsSystem) =
        let pureOperatorFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsOperator)

        let flowOperatorFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsOperator && not (c.IsOperator))

        let pureCommandFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsCommand)
                          
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
            let callDevices =s.GetDevicesHasOutput()
            
            for _, call in callDevices do
                yield! call.TargetJob.J1_JobActionOuts(call)
        ]

    let private applyCallOnDelay(s:DsSystem) =
        [
           yield!  s.T1_DelayCall()  
        ]
        
    let private emulationDevice(s:DsSystem) =
        [
            yield s.SetFlagForEmulation()
            let devsCall =  s.GetTaskDevsCall().DistinctBy(fun (td, c) -> (td, c.TargetJob))
            for td, call in devsCall do
                if not(td.IsRootOnlyDevice)
                then
                    if td.InTag.IsNonNull() then  
                        yield td.SensorEmulation(s, call.TargetJob)
        ]
 
    let private updateRealParentExpr(x:DsSystem) =
        for dev, call in x.GetTaskDevsCall() do
            let sensorExpr = 
                match call.GetEndAction() with
                | Some e -> e
                | _ -> call._on.Expr
            
            dev.GetApiItem(call.TargetJob).RX.ParentApiSensorExpr <-sensorExpr
               
    let convertSystem(sys:DsSystem, isActive:bool) =
        RuntimeDS.System <- sys

        sys.GenerationOrigins()

        if isActive //직접 제어하는 대상만 정렬(원위치) 정보 추출
        then 
           
            sys.GenerationMemory()
            sys.GenerationIO()

            match RuntimeDS.Package with
            | PCSIM -> 
                setSimulationEmptyAddress(sys) //시뮬레이션 주소를 위해 주소 지우기
            | _->  
                updateDuplicateAddress sys
                checkNullAddress sys
                checkJobs sys
                checkErrHWItem(sys)
                checkErrApi(sys)

            checkMultiDevPair(sys)

        else 
            checkErrRealResetExist(sys)
            updateRealParentExpr(sys)
            sys.GenerationRealActionMemory()

            
        [
            //Active 시스템 적용 
            if isActive
            then   
                yield! applySystemSpec sys
                yield! sys.B2_SWButtonOutput()
                yield! sys.B4_SWModeLamp() 

                yield! sys.E2_PLCOnly()

                if RuntimeDS.Package.IsPackageSIM()
                then 
                    yield! sys.Y1_SystemSimulationForFlow(sys)

                    for subSys in sys.GetRecursiveLoadedSystems() do
                        yield! subSys.Y1_SystemSimulationForFlow(sys) 

                    yield! emulationDevice sys

            //Variables  적용 
            yield! applyVariables sys
        
            //Flow 적용
            for f in sys.Flows do
                yield! applyOperationModeSpec f isActive
                yield! applyFlowMonitorSpec f

            //Vertex 적용
            for v in sys.GetVertices() do
                yield! applyVertexSpec v

            //Api 적용 
            //yield! applyApiItem sys
            //TaskDev 적용 
            yield! applyTaskDev sys
            
            //funcCall 적용 
            yield! funcCall sys

            //allpyJob 적용 
            yield! applyJob sys
            ///CallOnDelay 적용  
            yield! applyCallOnDelay sys
        ]
