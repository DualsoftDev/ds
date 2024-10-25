namespace rec Engine.Core

open System.IO
open System.Linq
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module ModelLoaderModule =
    type ModelConfig = {
        DsFilePath: string
        HwIP: string
        RuntimePackage: RuntimePackage
        HwDriver: string //LS-XGI, LS-XGK, Paix hw drive 이름
        RuntimeMotionMode: RuntimeMotionMode
        TimeSimutionMode : TimeSimutionMode
        TimeoutCall : uint32
    }

    type Model = {
        Config: ModelConfig
        System : DsSystem
        LoadingPaths : string list
    }

    let createDefaultModelConfig() =
        { 
            DsFilePath = ""
            HwIP = "127.0.0.1"
            RuntimePackage = PCSIM //unit test를 위해 PCSIM으로 설정
            HwDriver = "LS_XGK_IO"
            RuntimeMotionMode = MotionAsync
            TimeSimutionMode = TimeX1
            TimeoutCall = 15000u
        }
    let createModelConfig(path:string, hwIP:string, runtimePackage:RuntimePackage,
            hwDriver:string, 
            runtimeMotionMode:RuntimeMotionMode, 
            timeSimutionMode:TimeSimutionMode, 
            timeoutCall:uint32) =
        { 
            DsFilePath = path
            HwIP = hwIP
            RuntimePackage = runtimePackage
            HwDriver = hwDriver
            RuntimeMotionMode = runtimeMotionMode
            TimeSimutionMode = timeSimutionMode
            TimeoutCall = timeoutCall
        }
    let createModelConfigReplacePath (cfg:ModelConfig, path:string) =
        { cfg with DsFilePath = path }

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

    let SaveConfigWithPath (path: string) (cfg: ModelConfig) =
        SaveConfig path cfg
        path

    let exportLoadedSystem (s: LoadedSystem) =

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


[<AutoOpen>]
[<Extension>]
type ModelLoaderExt =


    [<Extension>]
    static member ExportToDS (sys:DsSystem, dsFilePath:string) =
        //sys.CheckValidInterfaceNchageParsingAddress() //test ahn ppt로부터 가져오면 체크시 주소가 없다.

        for s in sys.GetRecursiveLoadeds() do
            match s with
            | :? Device -> exportLoadedSystem s |> ignore
            | :? ExternalSystem -> exportLoadedSystem s |> ignore
            | _ -> ()

        FileManager.fileWriteAllText(dsFilePath, sys.ToDsText(false, true))

    [<Extension>]
    static member saveModelZip (loadingPaths:string seq, activeFilePath:string, layoutImgFiles:string seq, cfg:ModelConfig) =
        let targetPaths = (loadingPaths @ [activeFilePath])
        let zipPathDS  = targetPaths.ToDsZip(changeExtension (activeFilePath|> DsFile)  ".dsz")

        let zipPathPpt = targetPaths.Where(fun f-> f <> $"{TextLibrary}.ds")
                              .Select(fun f-> changeExtension (f|> DsFile)  ".pptx")
                              .ToDsZip(changeExtension (activeFilePath|> DsFile)  ".7z")

        let zipDir    = PathManager.getDirectoryName (zipPathDS|>DsFile)

        let topLevel = getTopLevelDirectory (loadingPaths@[|activeFilePath|] |> Seq.toList)

        let jsFilePath = $"{zipDir}{TextDSJson}" |> getValidFile


        let baseTempFilePath = $"{topLevel}base.ext"  //상대 경로 구하기 위한 임시경로
        let activeRelaPath = getRelativePath(baseTempFilePath|>DsFile) (activeFilePath|>DsFile);//   // 상대경로로 기본 저장
        let newCfg = createModelConfigReplacePath (cfg, activeRelaPath)
        let config = SaveConfigWithPath jsFilePath newCfg

        addFilesToExistingZipAndDeleteFiles zipPathDS ([zipPathPpt;config]@layoutImgFiles.ToList())

        zipPathDS




