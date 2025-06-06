[<AutoOpen>]
module Engine.CodeGenCPU.ConvertRuntimeMode

open System
open System.Linq
open Engine.Core
open Engine.CodeGenCPU
open Dual.Common.Core.FS

type DsSystem with
        
    

    member sys.CallActive() =
        [
            for vc in sys.GetVerticesOfCoins().Select(getVM) do
                yield vc.CallPlanStartActive()

                if vc.Vertex :? Call then
                    if (vc.Vertex:?>Call).IsJob then
                        yield! vc.E1_CallErrTimeOver()
                        yield! vc.E2_CallErrRXMonitor()
                        yield! vc.E3_CallErrRXInterlockMonitor()
                        yield  vc.E4_CallErrTotalMonitor()


            for real in sys.GetRealVertices().Select(getVMReal) do
                yield! real.CoinStartActive()
                yield! real.CoinEndActive()
        ]

    member sys.CallPassive() =
        [
            for vc in sys.GetVerticesOfCoins().Select(getVM) do
                yield vc.CallPlanStartPassive()

            for real in sys.GetRealVertices().Select(getVMReal) do
                yield! real.CoinStartPassive()
                yield! real.CoinEndPassive()
        ]

    member sys.CallVirtualPlant() =
        [
            let f = getFuncName()
            for vc in sys.GetVerticesOfCoins().Select(getVMCall) do
                let call = vc.Vertex :?> Call       
                let tempRising  = getSM(call.System).GetTempBoolTag("tempFallingOutput")
                yield! (call.Start, call.System) --^ (tempRising, f)

                yield (tempRising.Expr, call.End) ==| (vc.ST, f)
                yield (vc.ST.Expr     , call.End) ==| (vc.PS, f)


        ]
        

    member sys.RealActive() =
        [
            for real in sys.GetRealVertices().Select(getVMReal) do
                yield! real.RealEndActive()
                yield real.R1_RealInitialStart()
                yield real.F1_RootStartActive()
                yield real.F2_RootResetActive()
                yield real.R7_RealGoingOriginError()
        ]   

    member sys.RealPassive(isSubSystem:bool) (bVirtaulPlant:bool) =
        [
            for real in sys.GetRealVertices().Select(getVMReal) do
                if isSubSystem 
                then 
                    yield! real.RealEndActive()
                    yield real.F1_RootStartActive()
                    yield real.F2_RootResetActive()
                else 
                    yield! real.RealEndPassive()
                    yield real.F1_RootStartPassive(bVirtaulPlant)
                    yield real.F2_RootResetPassive()
        ]

    member sys.SensorEmulation() =
        [
            let devCallSet =  sys.GetTaskDevCalls()
            for (td, calls) in devCallSet do
                if td.InTag.IsNonNull() then
                    yield! td.SensorEmulation(calls)
        ]

        

                

    member sys.JobActionOut(isSimMode) =
        [|
            let devCallSet = sys.GetTaskDevCalls()
            for (td, coins) in devCallSet do
                let tm = td.TagManager :?> TaskDevManager
                yield! tm.J1_JobActionOuts(coins, isSimMode)
        |]


                