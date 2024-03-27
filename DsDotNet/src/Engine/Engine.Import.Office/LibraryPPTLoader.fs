// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Reflection
open LibraryLoaderModule
open Engine.Core

[<AutoOpen>]
[<Extension>]
type LibraryPPTLoaderExt =

    [<Extension>] 
        static member saveLibraryConfig(directoryPath:string) =
            let files = Directory.GetFiles(directoryPath, "*.pptx", SearchOption.AllDirectories)
            let informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion
            let infos = Dictionary<string, string>()
            let configPath = Path.Combine(directoryPath, "Library.config");

            for file in files do
                if not (file.Contains("~$")) then //pptx ����� �ӽ����� ����
                    let sys = ImportPPT.GetDSFromPPTWithLib(file, true).System
                    let relPath = Path.GetRelativePath(directoryPath, Path.ChangeExtension(file, ".ds"))
                    let relPathAddLibDirPath = Path.Combine("dsLib", relPath)
                    for item in sys.ApiItems do
                        if infos.ContainsKey item.Name
                        then 
                            failwithf $"{sys.Name}�� �ش��ϴ� [{item.Name}] �������̽� �̸��� �ߺ�({infos[item.Name]}) �˴ϴ�."
                        else 
                            infos.Add(item.Name, PathManager.getValidFile relPathAddLibDirPath)
        
            { Version = informationalVersion; LibraryInfos = infos } 
            |> SaveLibraryConfig configPath


    [<Extension>] 
        static member getLibraryConfig(path:string) = LoadLibraryConfig(path)

     [<Extension>]  
        static member getLibraryPath(libraryConfig:LibraryConfig, apiName:string) = 
                if libraryConfig.LibraryInfos.ContainsKey(apiName)
                then libraryConfig.LibraryInfos[apiName]
                else 
                    failwithf $"{apiName}�� �ش��ϴ� Library file�� ���� ���� �ʽ��ϴ�."
           