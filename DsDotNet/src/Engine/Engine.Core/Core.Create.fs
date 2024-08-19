// Copyright (c) Dualsoft  All Rights Reserved.
namespace rec Engine.Core

open System.Linq
open Dual.Common.Core.FS

[<AutoOpen>]
module CoreCreateModule =

    let createDsSystem(systemName:string) =
        let system = DsSystem.Create(systemName)
        system

    let createTaskDevUsingApiName (sys: DsSystem) (jobName:string) (devName: string) (apiName: string) (taskDevParamIO:TaskDevParamIO): TaskDev =
        let apis = sys.ApiItems.Where(fun w -> w.Name = apiName).ToFSharpList()

        let api =
            // Check if the API already exists
            match apis with
            | [] ->
                // Add a default flow if no flows exist
                if sys.Flows.IsEmpty then
                    sys.Flows.Add(Flow.Create("genFlow", sys)) |> ignore

                let realName = $"gen{apiName}"
                let flow = sys.Flows.Head()
                let reals = flow.Graph.Vertices.OfType<Real>().ToArray()
                if reals.Any(fun w -> w.Name = realName) then
                    failwithf $"real {realName} 중복 생성에러"

                // Create a new Real
                let newReal = Real.Create(realName, flow)


                flow.Graph.Vertices.OfType<Real>().Iter(fun r->r.Finished <- false)  //기존 Real이 원위치 취소
                newReal.Finished <- true    //마지막 Real이 원위치


                  // Create and add a new ApiItem
                let newApi = ApiItem.Create(apiName, sys, newReal, newReal)
                sys.ApiItems.Add newApi |> ignore

                if flow.Graph.Vertices.OfType<Real>().Count() > 1 then  //2개 부터 인터락 리셋처리
                    // Iterate over reals up to newReal
                    reals
                        .TakeWhile(fun r -> r <> newReal)
                        .Iter(fun r ->
                            let exAliasName = $"{r.Name}Alias_{newReal.Name}"
                            let myAliasName = $"{newReal.Name}Alias_{r.Name}"
                            let exAlias = Alias.Create(exAliasName, DuAliasTargetReal r, DuParentFlow flow, false)
                            let myAlias = Alias.Create(myAliasName, DuAliasTargetReal newReal, DuParentFlow flow, false)

                            // Create an edge between myAlias and exAlias
                            flow.CreateEdge(ModelingEdgeInfo<Vertex>(myAlias, "<|>", exAlias)) |> ignore)

                    // Potentially update other ApiItems based on the new ApiItem
                    //sys.ApiItems.TakeWhile(fun a -> a <> newApi)  autoGenByFlow 처리로 인해 필요없음
                    //     .Iter(fun a -> ApiResetInfo.Create(sys, a.Name, ModelingEdgeType.Interlock, newApi.Name) |> ignore)

                newApi
            | api::[] -> api
            | _ ->
                failwithf $"system {sys.Name} api {apiName} 중복 존재"

        let apiParam = {TaskDevParamIO =  taskDevParamIO; ApiItem = api}
        TaskDev(apiParam, jobName, devName, sys)


    let updateSystemForSingleApi(sys:DsSystem) =
        let updateSingleApi (refSys: DsSystem)  (api: ApiItem)  =
            let flow = refSys.Flows.Head()
            let oldReal = flow.Graph.Vertices.OfType<Real>().Head()
            let realName = $"genClearReal{api.Name}"
            let clearReal = Real.Create(realName, flow)
            oldReal.Finished <- false
            clearReal.Finished <- true
            flow.CreateEdge(ModelingEdgeInfo<Vertex>(oldReal , "<|>", clearReal)) |> ignore
            flow.CreateEdge(ModelingEdgeInfo<Vertex>(oldReal , ">", clearReal)) |> ignore

        let autoGenDevs =
            sys.LoadedSystems
                .Select(fun d->d.ReferenceSystem)

        autoGenDevs
            .Where(fun s->(s.GetVertices().OfType<Real>().Count()) = 1)
            .Iter(fun refSys-> updateSingleApi refSys (refSys.ApiItems.Head()))

    let loadedSystemsToDsFile(sys:DsSystem) =

        sys.LoadedSystems
            .Iter(fun refSys->
                let text = refSys.ReferenceSystem.ToDsText(false, true)
                fileWriteAllText (refSys.AbsoluteFilePath, text) )
