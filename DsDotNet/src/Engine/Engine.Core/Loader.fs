namespace Engine.Core

open System.IO
open System.Linq
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System


[<AutoOpen>]
module LoaderModule =


    type LibraryConfig = {
        ///parent 시스템에서 사용한 Lib버전과 현재 설치된 Lib 버전은 항상 같아야 한다
        Version: string
        ///Api 이름 중복을 막기위해 Dictionary 처리
        LibraryInfos: Dictionary<string, string> //Api, filePath
    }

    let private jsonSettings = JsonSerializerSettings()

    let LoadLibraryConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<LibraryConfig>(json, jsonSettings)

    let SaveLibraryConfig (path: string) (libraryConfig:LibraryConfig) =
        let json = JsonConvert.SerializeObject(libraryConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)


    let LoadConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

    let SaveConfig (path: string) (modelConfig:ModelConfig) =
        let json = JsonConvert.SerializeObject(modelConfig, jsonSettings)
        File.WriteAllText(path, json)


    let SaveConfigWithPath (path: string) (cfg: ModelConfig) =
        SaveConfig path cfg
        path
        
        
    type UserTag() =
        member val Name      = ""  with get, set
        member val DataType  = ""  with get, set
        member val Address   = ""  with get, set

    type UserTagConfig = {
        UserTags: UserTag array
    }

        
    let LoadUserTagConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<UserTagConfig>(json, jsonSettings)

    let SaveUserTagConfigWithPath (path: string) (cfg: UserTagConfig) =
        let json = JsonConvert.SerializeObject(cfg, jsonSettings)
        File.WriteAllText(path, json)
        path

    let createDefaultUserTagConfig() =
        { 
           UserTags = [||]
        }

    let ExportLoadedSystem (s: LoadedSystem) =

        let absFilePath =  PathManager.changeExtension (s.AbsoluteFilePath.ToFile())  "ds"
        let dsFile = absFilePath.ToFile()
        createDirectory dsFile

        let refName = s.ReferenceSystem.Name
        let libName = PathManager.getFileNameWithoutExtension(dsFile)

        s.ReferenceSystem.Name <- libName
        let txt =s.ReferenceSystem.ToDsText(false, true)
        FileManager.fileWriteAllText(absFilePath, txt)
        s.ReferenceSystem.Name <- refName

        absFilePath

    type Model = {
        Config: ModelConfig
        UserTagConfig: UserTagConfig
        System : DsSystem
        LoadingPaths : string list
    }


[<AutoOpen>]
[<Extension>]
type LoaderExt =

  

    [<Extension>]
    static member ExportToDS (sys:DsSystem, dsFilePath:string) =
        //sys.CheckValidInterfaceNchageParsingAddress() //test ahn ppt로부터 가져오면 체크시 주소가 없다.

        for s in sys.GetRecursiveLoadeds() do
            match s with
            | :? Device -> ExportLoadedSystem s |> ignore
            | :? ExternalSystem -> ExportLoadedSystem s |> ignore
            | _ -> ()

        FileManager.fileWriteAllText(dsFilePath, sys.ToDsText(false, true))

    [<Extension>]
    static member saveModelZip (loadingPaths:string seq, activeFilePath:string, layoutImgFiles:string seq, cfg:ModelConfig, userTagConfig:UserTagConfig) =
        let targetPaths = (loadingPaths @ [activeFilePath])
        let zipPathDS  = targetPaths.ToDsZip(changeExtension (activeFilePath|> DsFile)  "dsz")

        let zipPathPpt = targetPaths.Where(fun f-> f <> $"{TextLibrary}.ds")
                              .Select(fun f-> changeExtension (f|> DsFile)  "pptx")
                              .ToDsZip(changeExtension (activeFilePath|> DsFile)  "7z")

        let zipDir    = PathManager.getDirectoryName (zipPathDS|>DsFile)

        let topLevel = getTopLevelDirectory (loadingPaths@[|activeFilePath|] |> Seq.toList)

        let jsFilePath = $"{zipDir}{TextDSJson}" |> getValidFile
        let jsUserTagFilePath = $"{zipDir}{TextUserTag}" |> getValidFile


        let baseTempFilePath = $"{topLevel}base.ext"  //상대 경로 구하기 위한 임시경로
        let activeRelaPath = getRelativePath(baseTempFilePath|>DsFile) (activeFilePath|>DsFile);//   // 상대경로로 기본 저장
        let newCfg = createModelConfigReplacePath (cfg, activeRelaPath)
        let configPath = SaveConfigWithPath jsFilePath newCfg
        let userTagConfigPath = SaveUserTagConfigWithPath jsUserTagFilePath userTagConfig
        
        addFilesToExistingZipAndDeleteFiles zipPathDS ([zipPathPpt;configPath;userTagConfigPath]@layoutImgFiles.ToList())

        zipPathDS




