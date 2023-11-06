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

    let loadingLibray(pathPPT:string)=
        libDevOrgSys.Clear()
        let doc = pptDoc(pathPPT, None, Office.Open(pathPPT))
        let loadNodes = doc.GetLoadNodes()
        let loadingDeviceFromLib(devInfo:DevInfo seq)  = 
            let runDir = System.Reflection.Assembly.GetEntryAssembly().Location|>DsFile |> PathManager.getDirectoryName
            let libFilePath =  PathManager.getFullPath ("DS_Library.pptx"|>DsFile) (runDir|>DsDirectory)
            let systems = ImportPPTModule.loadingfromPPTs ([| libFilePath |])|> fun (model, views, pptRepo) -> model.Systems
            let libApisNSys= systems.GetlibApisNSys()
            devInfo.DistinctBy(fun di->di.DevName)
                   .Iter(fun di -> libDevOrgSys.Add(di.DevName, libApisNSys[di.ApiName]))
        
        loadingDeviceFromLib(loadNodes)
    
         
[<Extension>]
type ImportPPTWithLib =

    [<Extension>]
    static member GetDSFromPPTWithLib (fullName: string) =
            pptRepo.Clear()
            loadingLibray(fullName)
             
            let pptResults = loadingfromPPTs ([fullName]) |> fun (model, views, pptRepo) -> model
            let sys = pptResults.Systems.[0]

            let exportPath = sys.pptxToExportDS fullName
            let systems, loadingPaths = ParserLoader.LoadFromActivePath exportPath
            {
                Systems =  systems
                ActivePaths = [exportPath]
                LoadingPaths = loadingPaths
            }
            