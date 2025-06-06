namespace Engine.Core

open Dual.Common.Core.FS
open System.Linq
open System.Collections.Generic

[<AutoOpen>]
module FqdnExplorer =
    type FqdnDictionary = Dictionary<string, FqdnObject>

    let rec private visit (seed:FqdnObject) : (string * FqdnObject) seq =
        seq {
            match seed with
            | :? DsSystem as system ->
                yield (system.QualifiedName, system)
                for f in system.Flows do
                    yield! visit f
                for j in system.Jobs do
                    yield! visit j

                for d in system.Devices do
                    yield! visit d.ReferenceSystem

            | :? Flow as flow ->
                yield (flow.QualifiedName, flow)
                let vs = flow.GetVerticesOfFlow().ToArray()     // Real, Call
                yield! vs.Collect visit

            | :? Real as real ->
                yield (real.QualifiedName, real)
                for c in real.Graph.Vertices.OfType<Call>() do
                    yield! visit c

            | :? Call as call ->
                yield (call.QualifiedName, call)

            | :? Job as job ->
                yield (job.QualifiedName, job)
                for td in job.TaskDefs do
                    yield! visit td

            | :? TaskDev as taskDev ->
                yield (taskDev.QualifiedName, taskDev)

            | :? Alias as alias ->
                yield (alias.QualifiedName, alias)

            | _ -> failwith "ERROR"
        }
    let collectFqdnObjects (seed:FqdnObject) : FqdnDictionary = visit seed |> Tuple.toDictionary
