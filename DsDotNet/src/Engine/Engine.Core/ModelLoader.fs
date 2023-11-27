namespace rec Engine.Core

open System.IO
open System.Linq
open Newtonsoft.Json
open Dual.Common.Core.FS
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

    let SaveConfigWithPath (path: string) (sysRunPaths: string) =
        let cfg =
            {
                DsFilePath =  sysRunPaths.Replace("\\", "/")
                HWIP =  RuntimeDS.IP
            }
        SaveConfig path cfg 
        path

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


[<AutoOpen>]
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

    [<Extension>] 
    static member saveModelZip (loadingPaths:string seq, activeFilePath:string) = 

        let zipPathDS  = (loadingPaths @ [activeFilePath]).ToZip()
        let zipPathPPT = loadingPaths.Where(fun f-> f <> $"{TextLibrary}.ds")
                              .Select(fun f-> changeExtension (f|> DsFile)  ".pptx")
                              .ToZipPPT()

        let zipDir    = PathManager.getDirectoryName (zipPathDS|>DsFile)   
        let zipFile   = PathManager.getFileNameWithoutExtension (zipPathDS|>DsFile)   

        let jsFilePath = $"{zipDir}{TextDSJson}" |> getValidFile


        let baseTempFilePath = $"{zipDir}{zipFile}/base.ext"  //상대 경로 구하기 위한 임시경로
        let activeRelaPath = getRelativePath(baseTempFilePath|>DsFile) (activeFilePath|>DsFile);//   // 상대경로로 기본 저장

        let config = SaveConfigWithPath jsFilePath activeRelaPath

        addFilesToExistingZipAndDeleteFiles zipPathDS [zipPathPPT;config]

        zipPathDS




   