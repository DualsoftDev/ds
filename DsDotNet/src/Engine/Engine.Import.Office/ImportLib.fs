// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open Engine.Core
open System.Runtime.CompilerServices
open System
open Engine.Parser.FS

[<AutoOpen>]
module ImportLib =

   
    [<Extension>]
    type ImportPPTWithLib =

        [<Extension>]
        static member GetDSFromPPTWithLib (fullName: string) =
                pptRepo.Clear()
                let runDir = System.Reflection.Assembly.GetEntryAssembly().Location|>DsFile |> PathManager.getDirectoryName
                let libFilePath =  PathManager.getFullPath ($"{TextLibrary}.pptx"|>DsFile) (runDir|>DsDirectory)
                
                // 파일 loadingfromPPTs 시 DS_Library.ds 만드는 용도
                let libSys = loadingfromPPTs ([| libFilePath |]) |> fun(m,_,_) -> m.Systems.Head    
                let loadedlibFilePath = PathManager.getFullPath  ($"{TextLibrary}.pptx"|>DsFile) (PathManager.getDirectoryName(fullName|>DsFile)|>DsDirectory)
                libSys.pptxToExportDS (loadedlibFilePath) |> ignore
                
        
             
                let pptResults = loadingfromPPTs ([fullName]) |> fun (model, views, pptRepo) -> model
                let sys = pptResults.Systems.[0]

                let exportPath = sys.pptxToExportDS fullName
                let systems, loadingPaths = ParserLoader.LoadFromActivePath exportPath
                {
                    Systems =  systems
                    ActivePaths = [exportPath]
                    LoadingPaths = loadingPaths
                }
            