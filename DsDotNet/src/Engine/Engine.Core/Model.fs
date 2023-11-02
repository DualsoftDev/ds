namespace Engine.Core


[<AutoOpen>]
module ModelLoaderModule =
    type FilePath = string
    type ModelConfig = {
        DsFilePaths: FilePath list
    }
    type Model = {
        Config: ModelConfig
        Systems : DsSystem list
        LoadingPaths : string list
    }