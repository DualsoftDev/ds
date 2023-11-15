namespace Engine.Core


[<AutoOpen>]
module ModelLoaderModule =
    type ModelConfig = {
        DsFilePath: string 
    }
    type Model = {
        Config: ModelConfig
        System : DsSystem 
        LoadingPaths : string list
    }