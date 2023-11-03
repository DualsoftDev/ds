namespace Engine.Core


[<AutoOpen>]
module ModelLoaderModule =
    type ModelConfig = {
        DsFilePaths: string list
    }
    type Model = {
        Config: ModelConfig
        Systems : DsSystem list
        LoadingPaths : string list
    }