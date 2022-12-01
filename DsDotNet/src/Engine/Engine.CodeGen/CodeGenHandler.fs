namespace Engine.CodeGen
open Engine.Core

[<AutoOpen>]
module CodeGenHandler =
    type ParseModel(modelConfig:string) =
        let model = ModelLoader.LoadFromConfig(modelConfig)
        let cpuRes   = GenCpuCode(model)
        let hmiRes   = GenPilotCode(model)
        let pilotRes = GenHmiCode(model)
        let SelectGet target = 
            match target with
            | "cpu"      -> cpuRes
            | "ds-pilot" -> hmiRes
            | "hmi"      -> pilotRes
            | _ -> { 
                    from = null; 
                    succeed = false; 
                    body = null; 
                    error = "target error" 
                }
        member x.CpuResult   = cpuRes
        member x.HmiResult   = hmiRes
        member x.PilotResult = pilotRes
        member x.SelectedResult(target) = SelectGet target