namespace Engine.Runner

open System
open System.Linq

open Engine.Common.FS
open Engine.Core
open Engine.Core
open System.Collections
open System.Collections.Generic


[<AutoOpen>]
module internal ModelSerializerModule =
    //let serializeChild (child:IVertex) =
    //    match child with
    //    | :? Segment as seg ->
    //        seg.Name
    //    ()

    //let serializeCoin (coin:ICoin) =
    //    match coin with
    //    | :? Segment
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"

    let serializeTxRx(x:ITxRx) =
        match x with
        | :? Segment as s -> s.QualifiedName
        | _ -> failwith "ERROR"

    let serializeCallPrototype (callPrototype:CallPrototype) (indent:int) =
        let tab = getTab indent
        let underscore x = if x = "" then "_" else x
        let txs = String.Join(", ", callPrototype.TXs.Select(serializeTxRx))
        let rxs = String.Join(", ", callPrototype.RXs.Select(serializeTxRx))
        $"{tab}{callPrototype.Name} = {lb} {underscore txs} ~ {underscore rxs} {rb}"

    let serializeEdge (edge:Edge) (indent:int) =
        let tab = getTab indent
        let sources = String.Join(", ", edge.Sources.Cast<Named>().select(name).ToArray())
        let target = name (edge.Target :?> Named)
        $"{tab}{sources} {edge.GetOperator()} {target};"

    let rec serializeFlow (flow:Flow) (indent:int) =
        let tab = getTab indent
        let indent = indent + 1
        [
            match flow with
            | :? RootFlow as rf ->
                yield $"{tab}[flow] {flow.Name} = {lb}"
                for cp in rf.CallPrototypes do
                    yield serializeCallPrototype cp indent
            | _ -> yield $"{tab}{flow.Name} = {lb}"

            for iso in flow.IsolatedCoins do
                match iso with
                | :? ChildFlow as cf ->
                    yield! serializeFlow cf indent
                | :? Child as child ->
                    yield $"{tab}/* Child={child.QualifiedName} */"
                    ()
                | _ -> failwithlog "ERROR"
            for edge in flow.Edges do
                yield serializeEdge edge indent
            //for child in flow.ChildVertices do
            //    serializeChild child
            yield tab + "}"
        ]

    type ButtonDic = Dictionary<string, RootFlow[]>
    let serializeButtons (category:string, dic:ButtonDic) (indent:int) =
        let tab = getTab indent
        [
            yield $"{tab}[{category}] = {lb}"
            for kv in dic do
                let buttonName = kv.Key
                let flowNames =
                    let flows = kv.Value
                    String.Join("; ", flows.Select(name))
                let tab = getTab (indent+1)
                yield $"{tab}{buttonName} = {lb} {flowNames} {rb};"
            yield $"{tab}{rb}"
        ]
    let serializeSystem (system:DsSystem) =
        [
            yield $"[sys] {system.Name} = " + "{"
            for flow in system.RootFlows do
                yield! serializeFlow flow 1

            let kvs =
                [
                    ("emg", system.EmergencyButtons);
                    ("auto", system.AutoButtons);
                    ("start", system.StartButtons);
                    ("reset", system.ResetButtons);
                ] |> List.filter(fun (_, dic) -> dic.Count > 0)

            for kv in kvs do
                yield! serializeButtons kv 1

            yield "}"
        ]

    let serializeModel (model:Model) =
        let xs = [
            for sys in model.Systems do
                yield! serializeSystem sys
        ]
        String.Join("\r\n", xs)
