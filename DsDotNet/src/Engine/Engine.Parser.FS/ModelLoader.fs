namespace rec Engine.Core

open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open Engine.Parser.FS
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

    let private loadSystemFromDsFile (systemRepo:ShareableSystemRepository) (dsFilePath) =
        let text = File.ReadAllText(dsFilePath)
        let dir = Path.GetDirectoryName(dsFilePath)
        let option = ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone)
        let system = ModelParser.ParseFromString(text, option)
        system

    let LoadFromConfig(config: FilePath) =
        let systemRepo = ShareableSystemRepository()
        let envPaths = collectEnvironmentVariablePaths()
        let cfg = LoadConfig config
        let dirs = config.Split('/') |> List.ofArray
        let dir = StringExt.JoinWith(dirs.RemoveAt(dirs.Length - 1), "/")
        let systems =
            [
                for dsFile in cfg.DsFilePaths do 
                    [
                        dsFile;
                        $"{dir}/{dsFile}";
                        for path in envPaths do
                            $"{path}/{dsFile}"
                    ] 
                    |> fileExistChecker
                    |> loadSystemFromDsFile systemRepo
            ]
        { Config = cfg; Systems = systems}

    let getNewFileName (path: string) (ftype: string) =
        let directory = Path.GetDirectoryName(path)
        let fileName = Path.GetFileNameWithoutExtension(path)
        let fileExtension = Path.GetExtension(path)
        let dt = System.DateTime.Now.ToString("yyMMdd_HH_mm_ss")
        let newDirectory = sprintf "%s/%s_%s_autogen/%s" directory fileName ftype dt
        Directory.CreateDirectory(newDirectory) |> ignore
        let fileNamePost = fileName
        Path.Combine(newDirectory, sprintf "%s%s" fileNamePost fileExtension)

    let exportLoadedSystem (s: LoadedSystem) (dirNew: string) =
        let mutable commonDir = ""
        let fileNew = FileInfo(dirNew).FullName.ToLower().Replace("\\", "/")
        let fileAbs = FileInfo(s.AbsoluteFilePath).FullName.ToLower().Replace("\\", "/")
        
        let lib = fileNew.Split('/')
        let abs = fileAbs.Split('/')
        let di = DirectoryInfo(dirNew)
        let mutable shouldBreak = false

        for i in 0 .. abs.Length - 1 do
            if not shouldBreak then
                if i >= lib.Length || abs.[i] <> lib.[i] then
                    if i <> lib.Length - 2 then   //abs/s_autogen/date/...    2 레벨 하위에 생성
                        failwithf  $"{s.AbsoluteFilePath}.pptx \r\nSystem Library호출은 {di.Parent.FullName} 동일/하위 폴더야 합니다." 
                    shouldBreak <- true
                else
                    commonDir <- commonDir + abs.[i] + "/"

        let relativePath = fileAbs.Replace(commonDir.ToLower(), "")
        let absPath = sprintf "%s/%s.ds" dirNew relativePath

        if not (File.Exists(absPath)) then
            Directory.CreateDirectory(Path.GetDirectoryName(absPath)) |> ignore
            let refName = s.ReferenceSystem.Name
            let libName = Path.GetFileNameWithoutExtension(absPath)

            s.ReferenceSystem.Name <- libName
            File.WriteAllText(absPath, s.ReferenceSystem.ToDsText(false))
            s.ReferenceSystem.Name <- refName

        absPath

        
[<Extension>]
type ModelLoaderExt =
    [<Extension>] 
    static member PPTToDSExport (sys:DsSystem, pptPath:string) = 

        let newFile = getNewFileName pptPath  "DS" 
        let directory = Path.GetDirectoryName(newFile)

        let dsFile = Path.ChangeExtension(newFile, ".ds")
        let confFile = Path.ChangeExtension(newFile, ".json")

        let mutable dsCpuSys = [dsFile.Replace(Path.GetDirectoryName(dsFile) + "/", "")]

        for s in sys.GetRecursiveLoadeds() do
            match s with
            | :? Device -> ignore(exportLoadedSystem s  directory)
            | :? ExternalSystem ->
                let path = exportLoadedSystem s  directory 
                if path <> "" then
                    dsCpuSys <- path :: dsCpuSys
            | _ -> ()

        File.WriteAllText(dsFile, sys.ToDsText(false))
        ModelLoader.SaveConfigWithPath confFile  dsCpuSys 

        dsFile


module private TestLoadConfig =
    let testme() =
        let cfg =
            {   DsFilePaths = [
                    @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\cylinder.ds"
                    @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\station.ds" ] }

        let fp = @"F:\tmp\a.tmp"
        ModelLoader.SaveConfig fp cfg

        let cfg2 = ModelLoader.LoadConfig fp

        verify (cfg = cfg2)