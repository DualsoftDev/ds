namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Linq
open Engine.Common

[<AutoOpen>]
module ConvertCPU =
    

    ///Vertex 타입이 Spec에 해당하면 적용
    let private applyVertexSpec(v:Vertex)  =
        [
            if IsSpec (v, RealInFlow, AliasFalse) then
                let vr = v.TagManager :?> RealVertexTagManager
                yield vr.M1_OriginMonitor()
                yield vr.E5_RealErrTotalMonitor()

                yield  vr.R1_RealInitialStart()
                
                yield  vr.R3_RealStartPoint()
                yield  vr.R4_RealLink()
                yield! vr.R5_DummyDAGCoils()
                yield! vr.R8_RealGoingPulse()
                yield! vr.R12_RealGoingScript()


                yield vr.F7_HomeCommand()

                
                yield! vr.D3_CoinReset()
                yield! vr.R6_SourceTokenNumGeneration()
            

            if IsSpec (v, RealExSystem ||| RealExFlow, AliasNotCare) then
                let vm = v.TagManager :?> RealVertexTagManager
                yield vm.M2_PauseMonitor()
                yield vm.F3_RealEndInFlow()

            if IsSpec (v, CallInFlow, AliasNotCare) then
                let vc = v.TagManager :?> CoinVertexTagManager
                yield  vc.F4_CallEndInFlow()




            if IsSpec (v, CallInReal, AliasNotCare) then
                let vc = v.TagManager :?> CoinVertexTagManager
                
                yield vc.C2_CallPlanEnd()
                yield! vc.C3_InputDetected()
                yield! vc.C4_OutputDetected()
                yield vc.C5_StatActionFinish()
                
                
            if IsSpec (v, VertexAll, AliasNotCare) then
                let vm = v.TagManager :?> VertexTagManager
                yield! vm.S1_RGFH()
                yield! vm.H1_HmiPulse()
                
        ]



    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow)  =
        [
            yield f.O1_IdleOperationMode()
            yield f.O2_AutoOperationMode()
            yield f.O3_ManualOperationMode()
            yield f.ST1_OriginState()
            yield f.ST2_ReadyState()
            yield f.ST3_GoingState()
            yield f.ST4_EmergencyState()
            yield f.ST5_ErrorState()
            yield f.ST6_DriveState()
            yield f.ST7_TestState()
        ]

    let private applyFlowMonitorSpec(f:Flow) =
        [
            yield f.F1_FlowError()
            yield f.F2_FlowPause()

            yield! f.F5_FlowPauseAnalogAction()
            yield! f.F6_FlowPauseDigitalAction()
            yield! f.F7_FlowEmergencyAnalogAction()
            yield! f.F8_FlowEmergencyDigitalAction()
            
        ]

    let getTryMasterCall(calls : Vertex seq) =
        let pureCalls =
            calls.OfType<Call>()@calls.OfType<Alias>().Choose(fun a->a.TryGetPureCall())

        if pureCalls.Select(fun c->c.TargetJob).Distinct().Count() > 1 then
            failwithlog $"Error : {getFuncName()} {pureCalls.Select(fun c->c.TargetJob).Distinct().Count()}"

          // 동일 job으로 선정해서 coin중에서 아무거나 가져옴
        pureCalls.TryFind(fun f->not(f.IsFlowCall))


    let private applyApiItem(s:DsSystem) =
        [|
            let apiDevSet = s.GetDistinctApisWithDeviceCall()
            for (api, td, calls) in apiDevSet do
                let am = api.TagManager :?> ApiItemManager
                yield! am.A1_ApiSet(td, calls)
                yield  am.A2_ApiEnd()
        |]

    let private applyTaskDevSensorLink(s:DsSystem) =
        [|
            let devCallSet =  s.GetTaskDevsCoin()
            for (td, call) in devCallSet do
                let tm = td.TagManager :?> TaskDevManager
                yield! tm.TD1_SensorLinking(call)
                yield! tm.TD2_SensorLinked(call)
        |]

    let private funcCall(s:DsSystem) =
        let pureOperatorFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsOperator)

        let flowOperatorFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsOperator && not (c.IsOperator))

        let pureCommandFuncs =
            s.GetVertices().OfType<Call>().Where(fun c->c.IsCommand)

        [|

            for coin in pureOperatorFuncs do
                yield! coin.VC.C1_DoOperator()   //Operator 함수는 Call 수행후 연산결과를 PEFunc에 반영

            for coin in pureCommandFuncs do
                yield! coin.VC.C2_DoCommand()

            for coin in flowOperatorFuncs do
                yield coin.VC.C3_DoOperatorDevice()
        |]

    let private applyVariables(s:DsSystem) =
        [|
            for v in s.Variables do
                if v.VariableType = Immutable then
                    yield v.VM.V1_ConstMove(s)

            for v in s.ActionVariables do
                yield v.VM.V2_ActionVairableMove(s)
        |]




    let private updateRealParentExpr(x:DsSystem) =
        for dev, call in x.GetTaskDevsCall() do
            let sensorExpr =
                match call.ActionInExpr with
                | Some e -> e
                | _ -> call._on.Expr

            dev.ApiItem.RX.ParentApiSensorExpr <-sensorExpr


    type DsSystem with
        /// DsSystem 으로부터 CommentedStatement list 생성.
        member sys.GenerateStatements(activeSys:DsSystem) : CommentedStatement list =
            RuntimeDS.System <- Some sys

            let isActive = activeSys = sys
            sys.GenerationOrigins()
            sys.ClearExteralTags()


            if isActive then //직접 제어하는 대상만 정렬(원위치) 정보 추출
                sys.GenerationMemory()
                sys.GenerationIO()
                let mode = RuntimeDS.ModelConfig.RuntimePackage 
                updateSourceTokenOrder sys
                if mode = Control || mode = Monitoring
                then
                    checkNullAddress sys (mode = Monitoring) //모니터링 모드에서는 버튼램프 주소체크 안함
                    //setSimulationEmptyAddress(sys) //시뮬레이션 주소를 위해 주소 지우기
             
                updateDuplicateAddress sys
                checkJobs sys
                checkErrApi(sys)

                checkMultiDevPair(sys)

            else
                CheckRealReset(sys)
                updateRealParentExpr(sys)

            sys.GenerationRealActionMemory()

            [
                yield! applyRuntimeMode sys (not(isActive))

                //Active 시스템 적용
                if isActive then
                    yield! sys.B1_HWButtonOutput()
                    yield! sys.B3_HWModeLamp()

                    yield! [sys.Y2_SystemPause()]
                    yield! sys.Y3_SystemState()
                    yield! sys.Y4_SystemConditionError()
                    yield! sys.Y5_SystemEmgAlramError()
                    yield! sys.B2_SWButtonOutput()
                    yield! sys.B4_SWModeLamp()

                 

                else 
                    yield! sys.Y1_SystemActiveBtnForPassiveFlow(activeSys)
                    

                if RuntimeDS.ModelConfig.PlatformTarget.IsPLC then 
                    yield! sys.Y6_SystemClearBtnForFlow()

                //Variables  적용
                yield! applyVariables sys

                //Flow 적용

                for f in sys.Flows do
                    if isActive
                    then
                        yield! applyOperationModeSpec f
                        yield! applyFlowMonitorSpec f

                //Vertex 적용
                for v in sys.GetVertices() do
                    yield! applyVertexSpec v 

                //TaskDev Sensor Link 적용
                yield! applyTaskDevSensorLink sys
                //ApiItem 적용
                yield! applyApiItem sys
                //funcCall 적용
                yield! funcCall sys
          
                ///CallOnDelay 적용
                yield! sys.T1_DelayCall()
            ]
