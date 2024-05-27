// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open System.Collections.Generic
open Engine.Core
open DocumentFormat.OpenXml.Packaging
open System.Linq
open System.IO
open Dual.Common.Core.FS
open Engine.Parser.FS
open System.Reflection
open LibraryLoaderModule

[<AutoOpen>]
module ImportUtilForLib =

    let addLibraryNCall
        (
            libFilePath,
            loadedName,
            apiName,
            mySys: DsSystem,
            parentWrapper:ParentWrapper,
            node: pptNode,
            autoTaskDev: TaskDev option
        ) =

        let parent =parentWrapper

        let job =
            if autoTaskDev.IsSome
            then
                let tasks = HashSet<TaskDev>()
                autoTaskDev.Value.InAddress <- ("")
                autoTaskDev.Value.OutAddress<- ("")
                tasks.Add(autoTaskDev.Value)|>ignore
                Job(node.JobName, mySys, tasks |> Seq.toList)
            else 
                let libRelPath =
                    PathManager.getRelativePath (currentFileName |> DsFile) (libFilePath |> DsFile)

                let getDevParams(name) =
                    getParams (libFilePath, libRelPath, name, mySys, DuDevice, ShareableSystemRepository())

                let apiPureName = GetBracketsRemoveName(apiName).Trim()
                
                let devOrg, _ = ParserLoader.LoadFromActivePath libFilePath Util.runtimeTarget
                if not (devOrg.ApiItems.any (fun f -> f.Name = apiPureName)) then
                    node.Shape.ErrorName(ErrID._49, node.PageNum)

                let tasks = HashSet<TaskDev>()
                match getJobActionType apiName with
                | MultiAction (_, cnt) ->  
                    for i in [1..cnt] do
                        let devOrg = if i = 1 then devOrg
                                        else ParserLoader.LoadFromActivePath libFilePath Util.runtimeTarget |> fst

                        let mutiName = getDummyDeviceName loadedName i
                        let devParams = getDevParams mutiName

                        tasks.Add(getLoadedTasks mySys devOrg mutiName apiPureName devParams node)|>ignore
                | _->
                    tasks.Add(getLoadedTasks mySys devOrg loadedName apiPureName (getDevParams(loadedName)) node)|>ignore
                Job(node.JobName, mySys, tasks |> Seq.toList)

        let jobForCall =
            let tempJob = mySys.Jobs.FirstOrDefault(fun f->f.Name = job.Name)
            if tempJob.IsNull()
            then mySys.Jobs.Add(job);job
            else tempJob
        
        Call.Create(jobForCall, parent)

                
        
         