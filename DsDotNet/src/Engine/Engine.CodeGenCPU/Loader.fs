namespace Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Engine.Common.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module CpuLoader =
    ///Vertex 타입이 Spec에 해당하면 적용
    let private applyVertexSpec(v:Vertex) = 
        let vm = v.VertexManager :?> VertexManager
        [
           
            if IsSpec v CallInFlow then
                yield! vm.F3_RootStartCoin()
                yield vm.F4_RootCoinRelay()

            if IsSpec v RealInFlow then
                yield! vm.S1_RealRGFH()
                yield! vm.F1_RootStartReal()
                yield! vm.F2_RootResetReal()

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
                yield! vm.D3_DAGComplete()

            if IsSpec v (CallInReal ||| CallInFlow) then
                yield! vm.C1_CallActionOut()
            
            if IsSpec v CoinTypeAll then
                yield! vm.S2_CoinRGFH()
                yield vm.P4_CallStartPort()
                yield vm.P5_CallResetPort()
                yield vm.P6_CallEndPort()

            if IsSpec v (RealInFlow ||| CoinTypeAll)  then
                yield vm.M2_PauseMonitor()

            if IsSpec v (CallInReal ||| AliasCallInReal) then
                yield! vm.C1_CallActionOut()
                yield! vm.C2_CallTx()
                yield vm.C3_CallRx()

                yield! vm.M3_CallErrorTXMonitor()
                yield vm.M4_CallErrorRXMonitor()
        ]

    let private applyBtnLampSpec(s:DsSystem) =
        [
            yield! s.B1_ButtonOutput()
            yield! s.B2_ModeLamp()
        ]   
        
    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow) = 
        [
            yield f.O1_EmergencyOperationMode()
            yield f.O2_StopOperationMode()
            yield f.O3_ManualOperationMode()
            yield f.O4_RunOperationMode()
            yield f.O5_DryRunOperationMode()
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
            yield! applyBtnLampSpec sys
           
            //Flow 적용
            for f in sys.Flows
             do yield! applyOperationModeSpec f


            //Vertex 적용
            for v in sys.GetVertices()
             do yield! applyVertexSpec v
             
            yield! applyTimerCounterSpec sys
        ]

    [<Extension>]
    type Cpu =

        [<Extension>]
        static member LoadStatements (system:DsSystem) = convertSystem(system)
        
