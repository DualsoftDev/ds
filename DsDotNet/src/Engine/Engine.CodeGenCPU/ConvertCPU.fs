namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Linq

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
    let private applyVertexSpec(v:Vertex) isActive =
        [
            if IsSpec (v, RealInFlow, AliasFalse) then
                let vr = v.TagManager :?> RealVertexTagManager
                yield vr.M1_OriginMonitor()
                yield vr.E4_RealErrorTotalMonitor()

                yield  vr.R1_RealInitialStart()
                yield! vr.R2_RealJobComplete()
                yield  vr.R3_RealStartPoint()
                yield  vr.R4_RealLink()
                yield! vr.R5_DummyDAGCoils()
                yield! vr.R7_RealGoingOriginError()
                yield! vr.R8_RealGoingPulse()
                yield! vr.R10_RealGoingTime()
                yield! vr.R11_RealGoingMotion()
                yield! vr.R12_RealGoingScript()

                yield vr.F1_RootStart()
                yield vr.F2_RootReset()
                yield vr.F7_HomeCommand()

                yield! vr.D1_DAGHeadStart()
                yield! vr.D2_DAGTailStart()
                yield! vr.D3_DAGCoinEnd()
                yield! vr.D4_DAGCoinReset()

                if isActive then
                    yield! vr.R6_RealTokenMoveNSink()



            if IsSpec (v, RealExSystem ||| RealExFlow, AliasNotCare) then
                let vm = v.TagManager :?> RealVertexTagManager
                yield vm.M2_PauseMonitor()
                yield vm.F3_RealEndInFlow()

            if IsSpec (v, CallInFlow, AliasNotCare) then
                let vc = v.TagManager :?> CoinVertexTagManager
                yield  vc.F4_CallEndInFlow()

            if IsSpec (v, CallInFlow, AliasFalse) then
                let vc = v.TagManager :?> CoinVertexTagManager
                yield! vc.F5_SourceTokenNumGeneration()

            if IsSpec (v, CallInReal , AliasFalse) then
                let vc = v.TagManager :?> CoinVertexTagManager
                yield! vc.E2_CallErrorTXMonitor()
                yield! vc.E3_CallErrorRXMonitor()
                yield  vc.E5_CallErrorTotalMonitor()

            if IsSpec (v, CallInReal, AliasNotCare) then
                let vc = v.TagManager :?> CoinVertexTagManager
                yield vc.C1_CallMemo()

            if IsSpec (v, VertexAll, AliasNotCare) then
                let vm = v.TagManager :?> VertexTagManager
                yield! vm.S1_RGFH()

        ]

    let private applySystemSpec(s:DsSystem) =
        [
            yield! s.B1_HWButtonOutput()
            yield! s.B3_HWModeLamp()

            yield  s.Y2_SystemPause()
            yield! s.Y3_SystemState()
            yield! s.Y4_SystemConditionError()
            yield! s.Y5_SystemEmgAlramError()

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
            yield! f.F3_FlowReadyCondition()
            yield! f.F4_FlowDriveCondition()
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

    let private applyTaskDev(s:DsSystem) =
        [|
            let devCallSet =  s.GetTaskDevCalls()
            for (td, calls) in devCallSet do
                let tm = td.TagManager :?> TaskDevManager
                yield! tm.TD1_PlanSend(s, calls)
                yield! tm.TD2_PlanReceive(s)
                yield! tm.TD3_PlanOutput(s)
        |]

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
                yield! tm.TD4_SensorLinking(call)
                yield! tm.TD5_SensorLinked(call)
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

    let private applyJob(s:DsSystem) =
        [|
            let callDevices =s.GetDevicesHasOutput()

            for _, call in callDevices do
                yield! call.TargetJob.J1_JobActionOuts(call)


            for j in s.Jobs do
                yield! j.J2_InputDetected()
                yield! j.J3_OutputDetected()
        |]


    let private emulationDevice(s:DsSystem) =
        [|
            yield s.SetFlagForEmulation()

            let devCallSet =  s.GetTaskDevCalls()
            for (td, calls) in devCallSet do
                if (*not(td.IsRootOnlyDevice) &&*) td.InTag.IsNonNull() then
                    yield! td.SensorEmulation(s, calls)
        |]

    let private updateRealParentExpr(x:DsSystem) =
        for dev, call in x.GetTaskDevsCall() do
            let sensorExpr =
                match call.GetEndAction() with
                | Some e -> e
                | _ -> call._on.Expr

            dev.GetApiItem(call.TargetJob).RX.ParentApiSensorExpr <-sensorExpr


    type DsSystem with
        /// DsSystem 으로부터 CommentedStatement list 생성.
        member sys.GenerateStatements(isActive:bool) : CommentedStatement list =
            RuntimeDS.System <- Some sys

            sys.GenerationOrigins()

            if isActive then //직접 제어하는 대상만 정렬(원위치) 정보 추출
                sys.GenerationMemory()
                sys.GenerationIO()

                updateSourceTokenOrder sys

                match RuntimeDS.Package with
                | PCSIM ->
                    setSimulationEmptyAddress(sys) //시뮬레이션 주소를 위해 주소 지우기
                | _->
                    updateDuplicateAddress sys
                    CheckNullAddress sys
                    checkJobs sys
                    checkErrHWItem(sys)
                    checkErrApi(sys)

                checkMultiDevPair(sys)

            else
                CheckRealReset(sys)
                updateRealParentExpr(sys)
                sys.GenerationRealActionMemory()

            [
                //Active 시스템 적용
                if isActive then
                    yield! applySystemSpec sys
                    yield! sys.B2_SWButtonOutput()
                    yield! sys.B4_SWModeLamp()

                    if RuntimeDS.Package.IsPLCorPLCSIM() then
                        yield! sys.E2_PLCOnly()

                    if RuntimeDS.Package.IsPackageSIM() then
                        yield! emulationDevice sys

                    yield! sys.Y1_SystemBtnForFlow(sys)

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
                    yield! applyVertexSpec v isActive

                //TaskDev 적용
                yield! applyTaskDev sys
                //TaskDev Sensor Link 적용
                yield! applyTaskDevSensorLink sys
                //ApiItem 적용
                yield! applyApiItem sys
                //funcCall 적용
                yield! funcCall sys
                //allpyJob 적용
                yield! applyJob sys
                ///CallOnDelay 적용
                yield! sys.T1_DelayCall()
            ]
