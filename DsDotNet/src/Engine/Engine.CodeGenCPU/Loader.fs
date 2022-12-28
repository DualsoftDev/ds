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
            if IsSpec v (RealPure ||| CallPure ||| AliasForCall)
            then
                yield! vm.S1_Ready_Going_Finish_Homing()
                yield vm.M2_PauseMonitor()
                yield vm.M3_ErrorTXMonitor()
                yield vm.M4_ErrorRXMonitor()

            if IsSpec v (CallPure ||| AliasForCall)
            then
                yield vm.C1_CallActionOut()
                yield vm.C2_CallInitialComplete()
                yield vm.C3_CallTailComplete()
                yield vm.C4_CallTx()
                yield vm.C5_CallRx()

            if IsSpec v VertexAll
            then
                yield! vm.F1_RootStart() |> Option.toList
                yield! vm.F2_RootReset()

            if IsSpec v RealPure
            then 
                yield vm.M1_OriginMonitor()
                yield vm.P1_RealStartPort()
                yield vm.P2_RealResetPort()
                yield vm.P3_RealEndPort()

                for coin in (v :?> Real).Graph.Vertices.Select(getVM) do
                yield coin.D1_DAGInitialStart()
                yield coin.D2_DAGTailStart()


            if IsSpec v CallPure
            then
                yield vm.P4_CallStartPort()
                yield vm.P5_CallResetPort()
                yield vm.P6_CallEndPort()

        ]
    let private applyBtnLampSpec(s:DsSystem) = []
    ///flow 별 운영모드 적용
    let private applyOperationModeSpec(f:Flow) = 
        [
            yield f.O1_EmergencyOperationMode()
            yield f.O2_StopOperationMode()
            yield f.O3_ManualOperationMode()
            yield f.O4_RunOperationMode()
            yield f.O5_DryRunOperationMode()
        ]
    let private applyTimerCounterSpec(s:DsSystem) = []
        

    let private convertSystem(sys:DsSystem) =

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
        static member LoadStatements         (system:DsSystem) = convertSystem(system)
        
