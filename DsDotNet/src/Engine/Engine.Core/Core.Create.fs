// Copyright (c) Dual Inc.  All Rights Reserved.
namespace rec Engine.Core

open System.Collections.Generic
open System.Linq
open System.Diagnostics
open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module CoreCreateModule =

    let createDsSystem(systemName:string) =
        let system = DsSystem(systemName)
        system

    let genClearRealAddForSingleReal (mySys: DsSystem) =
        let updateSingleReal (flow:Flow) (real: Real) =
            let oldReal = flow.Graph.Vertices.OfType<Real>().Head()
            let realName = $"genClearReal{real.Name}"
            let clearReal = Real.Create(realName, flow)
            flow.CreateEdge(ModelingEdgeInfo<Vertex>(oldReal , "<|>", clearReal)) |> ignore
            flow.CreateEdge(ModelingEdgeInfo<Vertex>(oldReal , ">", clearReal)) |> ignore

        mySys.Flows.Iter(fun flow ->
            let reals = flow.Graph.Vertices.OfType<Real>()

            if reals.length() = 1
            then updateSingleReal flow (reals.First())
        )

    let createTaskDevUsingApiName (sys: DsSystem) (devName: string) (apiName: string): TaskDev =

        let api = 
        
            // Check if the API already exists
            if not (sys.ApiItems.Any(fun w -> w.Name = apiName)) 
            then
                // Add a default flow if no flows exist
                if sys.Flows.IsEmpty() then
                    sys.Flows.Add(Flow.Create("genFlow", sys)) |> ignore

                let realName = $"genReal{apiName}"
                let flow = sys.Flows.Head()
                let reals = flow.Graph.Vertices.OfType<Real>().ToArray()
                if reals.Any(fun w -> w.Name = realName) then
                    failwithf $"real {realName} 중복 생성에러"

                // Create a new Real
                let newReal = Real.Create(realName, flow)
                  // Create and add a new ApiItem
                let newApi = ApiItem.Create(apiName, sys, [newReal], [newReal])
                sys.ApiItems.Add newApi |> ignore
             
                if flow.Graph.Vertices.OfType<Real>().Count() > 1   //2개 부터 인터락 리셋처리
                then
                    // Iterate over reals up to newReal
                    reals.TakeWhile(fun r -> r <> newReal)
                         .Iter(fun r -> 
                                let exAliasName = $"{r.Name}Alias_{newReal.Name}"
                                let myAliasName = $"{newReal.Name}Alias_{r.Name}"
                                let exAlias = Alias.Create(exAliasName, DuAliasTargetReal r, DuParentFlow flow)
                                let myAlias = Alias.Create(myAliasName, DuAliasTargetReal newReal, DuParentFlow flow)
                    
                                // Create an edge between myAlias and exAlias
                                flow.CreateEdge(ModelingEdgeInfo<Vertex>(myAlias, "<|>", exAlias)) |> ignore)
              
                    // Potentially update other ApiItems based on the new ApiItem
                    sys.ApiItems.TakeWhile(fun a -> a <> newApi)
                         .Iter(fun a -> ApiResetInfo.Create(sys, a.Name, ModelingEdgeType.InterlockWeak, newApi.Name) |> ignore)
                
                newApi
            else
                failwithf $"api {apiName} 중복 생성에러"

        TaskDev(api, TextAddrEmpty |> defaultDevParam, TextAddrEmpty  |> defaultDevParam, devName)


    let apiAutoGenUpdateSystem(sys:DsSystem) =
        let updateSingleApi (refSys: DsSystem)  (api: ApiItem)  =
            let flow = refSys.Flows.Head()
            let oldReal = flow.Graph.Vertices.OfType<Real>().Head()
            let realName = $"genClearReal{api.Name}"
            let clearReal = Real.Create(realName, flow)
            flow.CreateEdge(ModelingEdgeInfo<Vertex>(oldReal , "<|>", clearReal)) |> ignore
            flow.CreateEdge(ModelingEdgeInfo<Vertex>(oldReal , ">", clearReal)) |> ignore

        let autoGenDevs = sys.LoadedSystems
                             .Where(fun d->d.AutoGenFromParentSystem)
                             .Select(fun d->d.ReferenceSystem)

        autoGenDevs.Where(fun s->s.ApiItems.Count = 1)
                   .Iter(fun refSys-> updateSingleApi refSys (refSys.ApiItems.Head()))

    let loadedSystemsToDsFile(sys:DsSystem) =

        sys.LoadedSystems
                   .Iter(fun refSys-> 
                        let text = refSys.ReferenceSystem.ToDsText(false)
                        fileWriteAllText (refSys.AbsoluteFilePath, text)
                        )
