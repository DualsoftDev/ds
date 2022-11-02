namespace Engine.CodeGen
open Engine.Parser

[<AutoOpen>]
module CodeGenHandler =
    type ParseModel(dsCode:string) = 
        let parsedDsCode =
            ModelParser.ParseFromString2(
                dsCode, 
                ParserOptions.Create4Simulation()
            )
        let model = parsedDsCode.Model
        member x.CpuCode   = GenCpuCode(model)
        member x.PilotCode = GenPilotCode(model)
        member x.HmiCode   = GenHmiCode(model)