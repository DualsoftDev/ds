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
                     | DuAliasTargetReal   ar -> failwithlog "Error IsSpec"
                     | DuAliasTargetRealExFlow ao -> failwithlog "Error IsSpec"
                     | DuAliasTargetRealExSystem ao -> failwithlog "Error IsSpec"
                     | DuAliasTargetCall   ac -> vaild.HasFlag(AliasCallInReal)
            |_ -> failwithlog "Error"

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

           // if IsSpec v (CallInReal ||| AliasCallInReal) then

        ]

    let private applySystemSpec(s:DsSystem) =
        [
            yield! s.B1_ButtonOutput()
            yield! s.B2_ModeLamp()
            yield! s.Y1_SystemBitSetFlow()
        ]

    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow) =
        [
            yield f.O1_AutoOperationMode()
            yield f.O2_ManualOperationMode()
            yield f.O3_DriveOperationMode()
            yield f.O4_TestRunOperationMode()
            yield f.O5_EmergencyMode()
            yield f.O6_StopMode()
            yield f.O7_ReadyMode()
        ]

    let private applyTimerCounterSpec(s:DsSystem) =
        [
            yield! s.T1_DelayCall()
        ]



    let private convertSystem(sys:DsSystem) =

        //DsSystem 물리 IO 생성
        sys.GenerationButtonIO()
        sys.GenerationLampIO()
        sys.GenerationJobIO()

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

    [<Extension>]
    type Cpu =

        [<Extension>]
        static member LoadStatements (system:DsSystem) =
            let statements = convertSystem(system)

            //test debug
            system._auto.Value <- true
            system._ready.Value <- true
            system._drive.Value <- true
            statements.Iter(fun f->f.Statement.Do())
            //test debug

            statements
