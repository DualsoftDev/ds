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

    let LoadConfig (path: FilePath) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

    let SaveConfig (path: FilePath) (modelConfig:ModelConfig) =
        let json = JsonConvert.SerializeObject(modelConfig, jsonSettings)
        File.WriteAllText(path, json)

    let SaveConfigWithPath (path: FilePath) (sysRunPaths: string seq) =
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
        FileManager.fileWriteAllText(absFilePath, s.ReferenceSystem.ToDsText(false))
        s.ReferenceSystem.Name <- refName

        absFilePath


[<Extension>]
type ModelLoaderExt =


    [<Extension>] 
    static member pptxToExportDS (sys:DsSystem, pptPath:string) = 

        let dsFile = PathManager.changeExtension (pptPath.ToFile()) ".ds" 
        let jsFile = PathManager.changeExtension (pptPath.ToFile()) ".json"
        let myDsFile = PathManager.getRelativePath (jsFile.ToFile()) (dsFile.ToFile())//   // 상대경로로 기본 저장

        for s in sys.GetRecursiveLoadeds() do
            match s with
            | :? Device -> exportLoadedSystem s |> ignore
            | :? ExternalSystem -> exportLoadedSystem s |> ignore
            | _ -> ()

        FileManager.fileWriteAllText(dsFile, sys.ToDsText(false))
        ModelLoader.SaveConfigWithPath jsFile [ myDsFile ]

        dsFile
