// Copyright (c) Dualsoft  All Rights Reserved.
namespace rec Engine.Core

open System.Linq
open Dual.Common.Core.FS

[<AutoOpen>]
module CoreCreateModule =
    let updateSystemForSingleApi(sys:DsSystem) =
        let updateSingleApi (refSys: DsSystem)  (api: ApiItem)  =
            let flow = refSys.Flows.Head()
            let oldReal = flow.Graph.Vertices.OfType<Real>().Head()
            let realName = $"genClearReal{api.Name}"
            let clearReal = flow.CreateReal(realName)
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
