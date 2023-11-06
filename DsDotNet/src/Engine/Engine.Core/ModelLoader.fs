namespace rec Engine.Core

open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices



[<AutoOpen>]
[<RequireQualifiedAccess>]
module ModelLoader =
    let private jsonSettings = JsonSerializerSettings()

    let LoadConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

    let SaveConfig (path: string) (modelConfig:ModelConfig) =
        let json = JsonConvert.SerializeObject(modelConfig, jsonSettings)
        File.WriteAllText(path, json)

    let SaveConfigWithPath (path: string) (sysRunPaths: string seq) =
        let cfg =
            {
                DsFilePaths = 
                    sysRunPaths
                    |> Seq.map(fun path -> path.Replace("\\", "/"))
                    |> Seq.toList
            }
        SaveConfig path cfg 

    let exportLoadedSystem (s: LoadedSystem) =
       
        let absFilePath =  PathManager.changeExtension (s.AbsoluteFilePath.ToFile())  "ds"
        let dsFile = absFilePath.ToFile()
        createDirectory dsFile

        let refName = s.ReferenceSystem.Name
        let libName = PathManager.getFileNameWithoutExtension(dsFile)

        s.ReferenceSystem.Name <- libName
        let txt =s.ReferenceSystem.ToDsText(false)
        FileManager.fileWriteAllText(absFilePath, txt)
        s.ReferenceSystem.Name <- refName

        absFilePath


[<Extension>]
type ModelLoaderExt =


    [<Extension>] 
    static member pptxToExportDS (sys:DsSystem, pptPath:string) = 

        let dsFilePath = PathManager.changeExtension (pptPath.ToFile()) ".ds" 
        
        for s in sys.GetRecursiveLoadeds() do
            match s with
            | :? Device -> exportLoadedSystem s |> ignore
            | :? ExternalSystem -> exportLoadedSystem s |> ignore
            | _ -> ()

        FileManager.fileWriteAllText(dsFilePath, sys.ToDsText(false))

        dsFilePath
