// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Collections.Generic
open Dual.Common.Core.FS
open Dual.Common.Base.FS
open System.IO
open System.Runtime.CompilerServices
open System.Reflection
open LibraryLoaderModule
open Engine.Core
open Engine.Core.MapperDataModule

[<AutoOpen>]
[<Extension>]
type LibraryPptLoaderExt =

    [<Extension>]
        static member saveLibraryConfig(directoryPath:string) =
            let files = Directory.GetFiles(directoryPath, "*.pptx", SearchOption.AllDirectories)
            let informationalLib = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description
            let infos = Dictionary<string, string>()
            let configPath = Path.Combine(directoryPath, "Library.config");
            let pptParms:PptParams = {HwTarget = getDefaltHwTarget(); AutoIOM = true; CreateFromPpt = false; CreateBtnLamp = false; StartMemory = 1000; OpMemory = 100}
            let modelConfig = createDefaultModelConfig()    
            for file in files do
                if not (file.Contains("~$")) then //pptx 사용중 임시파일 무시 ~$HelloDS.pptx
                    
                    let sys = ImportPpt.GetDSFromPptWithLib(file, true, pptParms).System
                    let relPath = Net48Path.GetRelativePath(directoryPath, Path.ChangeExtension(file, ".ds"))
                    //let relPathAddLibDirPath = Path.Combine("dsLib", relPath)
                    for item in sys.ApiItems do
                        if infos.ContainsKey item.Name
                        then
                            failwithf $"{sys.Name}에 해당하는 [{item.Name}] 인터페이스 이름은 중복({infos[item.Name]}) 됩니다."
                        else
                            infos.Add(item.Name, PathManager.getValidFile relPath)

            { Version = informationalLib; LibraryInfos = infos }
            |> SaveLibraryConfig configPath


    [<Extension>]
        static member getLibraryConfig(path:string) = LoadLibraryConfig(path)

    [<Extension>]
        static member getLibraryPath(libraryConfig:LibraryConfig, apiName:string) =
                if libraryConfig.LibraryInfos.ContainsKey(apiName)
                then libraryConfig.LibraryInfos[apiName]
                else
                    failwithf $"{apiName}에 해당하는 Library file은 존재 하지 않습니다."
