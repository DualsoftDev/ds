namespace Engine.CodeGenCPU

open Engine.Core
open Dual.Common.Core.FS
open System.Linq
open Engine.Common

[<AutoOpen>]
module ConvertRuntimeModeModule =

    let applyModeControl(sys:DsSystem)  =
        [
            yield! sys.RealActive()   
            yield! sys.CallActive()   
            yield! sys.JobActionOut()   
            yield! applyVertexToken sys
        ]

    let applyModeMonitor(sys:DsSystem) (isSubSys:bool)  =
        [
            yield! sys.RealPassive(isSubSys)   
            yield! sys.CallPassive()   
        ]    
    
    let applyModeVirtualPlant(sys:DsSystem)(isSubSys:bool)  =
        [       
            yield! sys.RealPassive(isSubSys)   
            yield! sys.CallPassive()   
            yield! sys.SensorEmulation()   
        ]


    let applyModeSimulation(sys:DsSystem)  =
        [       
            yield! sys.RealActive()   
            yield! sys.CallActive()   
            yield! sys.SensorEmulation()   
            yield! applyVertexToken sys
        ]

    let applyModeVirtualLogic(sys:DsSystem)  =
        [
            yield! sys.RealActive()   
            yield! sys.CallActive()   
            yield! sys.SensorEmulation()   
            yield! applyVertexToken sys
        ]


    let mode = RuntimeDS.ModelConfig.RuntimePackage
    let applyRuntimeMode(sys:DsSystem)(isSubSys:bool)  =
        [
            yield!
                sys.GetRealVertices().Select(getVMReal).Collect(fun vr -> 
                    vr.R10_GoingTime(mode)@
                    vr.R11_GoingMotion(mode)
                    )

            match mode with
            | Control      -> yield! applyModeControl sys
            | Monitoring   -> yield! applyModeMonitor sys isSubSys
            | Simulation   -> yield! applyModeSimulation sys
            | VirtualLogic -> yield! applyModeVirtualLogic sys
            | VirtualPlant -> yield! applyModeVirtualPlant sys isSubSys
        ]