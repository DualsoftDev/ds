namespace Engine.CodeGen
open Engine.Parser.FS

[<AutoOpen>]
module CodeGenHandler =
    type ParseModel(dsCode:string) =
        let parsedDsCode =
            ModelParser.ParseFromString2(
                dsCode,
                ParserOptions.Create4Simulation("SomeActiveCpuName")
            )
        let model    = parsedDsCode.Model
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