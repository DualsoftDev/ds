namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq

module SpitModuleHelper =
    type Alias(nameCompoents:string[]) =
        member x.NameComponents = nameCompoents

    type SpitResult = (obj * string[])[]
    let rec spitChild (child:Child) : SpitResult =
        [|
            yield child, child.NameComponents
        |]
    and spitSegment (segment:Segment) : SpitResult =
        [|
            yield segment, segment.NameComponents
            for ch in segment.Graph.Vertices do
                yield! spit(ch)
        |]
    and spitFlow (flow:Flow) : SpitResult =
        [|
            let fns = flow.NameComponents
            yield flow, fns
            for flowVertex in flow.Graph.Vertices do
                yield! spit(flowVertex)

            // A."+" = { Ap1; Ap2; }    : alias=A."+", mnemonics = [Ap1; Ap2;]
            // Main = { Main2; }
            for KeyValue(alias, mnemonics) in flow.AliasMap do
            for m in mnemonics do
                let alias2 =
                    match alias.Length with
                    | 2 -> alias            // A."+"
                    | 1 -> fns.Append(alias[0]).ToArray()   // My.Flow + Main
                    | _ -> failwith "ERROR"
                yield Alias([| yield! fns; m |]), alias2       // [ My.Flow.Ap1, A."+";  My.Flow.Main2, My.Flow.Main; ...]
        |]
    and spitSystem (system:DsSystem) : SpitResult =
        [|
            yield system, system.NameComponents
            for flow in system.Flows do
                yield! spit(flow)
            if system.Api <> null then
                for itf in system.Api.Items do
                    yield itf, itf.NameComponents
        |]
    and spitModel (model:Model) : SpitResult =
        [|
            yield model, [||]
            for sys in model.Systems do
                yield! spit(sys)
        |]
    and spit(obj:obj) : SpitResult =
        match obj with
        | :? Model as m -> spitModel m
        | :? DsSystem as s -> spitSystem s
        | :? Flow as f -> spitFlow f
        | :? Segment as s -> spitSegment s
        | :? Child as c -> spitChild c
    ()

open SpitModuleHelper

[<Extension>]
type SpitModule =

    [<Extension>] static member Spit (model:Model) = spitModel model
    [<Extension>] static member Spit (system:DsSystem) = spitSystem system
    [<Extension>] static member Spit (flow:Flow) = spitFlow flow
    [<Extension>] static member Spit (segment:Segment) = spitSegment segment

