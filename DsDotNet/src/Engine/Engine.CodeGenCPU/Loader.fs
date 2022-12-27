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

            if IsSpec v VertexAll
            then
                yield! vm.F1_RootStart() |> Option.toList
          //      yield! vm.F2_RootReset()

            if IsSpec v RealPure
            then 
                yield vm.P1_RealStartPort()
                yield vm.P2_RealResetPort()
                yield vm.P3_RealEndPort()

            if IsSpec v CallPure
            then
                yield vm.P4_CallStartPort()
                yield vm.P5_CallResetPort()
                yield vm.P6_CallEndPort()

        ]
    let private applyBtnLampSpec(s:DsSystem) = []
    let private applyOperationModeSpec(s:DsSystem) = []
    let private applyTimerCounterSpec(s:DsSystem) = []
        

    let private convertSystem(sys:DsSystem) =
        [
            for v in sys.GetVertices()
             do yield! applyVertexSpec v


            yield! applyBtnLampSpec sys
            yield! applyOperationModeSpec sys
            yield! applyTimerCounterSpec sys
        ]

    [<Extension>]
    type Cpu =

        [<Extension>]
        static member LoadStatements         (system:DsSystem) = convertSystem(system)
        
