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
        let mutable closeBrace = true
        [
            match flow with
            | :? RootFlow as rf ->
                yield $"{tab}[flow] {flow.Name.QuoteOnDemand()} = {lb}"

                let segWithSafety =
                    rf.RootSegments
                        .Where(fun seg -> seg.SafetyConditions <> null)
                        .ToArray()
                if segWithSafety.Any() then
                    let tab = getTab indent
                    yield $"{tab}[safety] = {lb}"
                    for seg in segWithSafety do
                        let tab = getTab (indent+1)
                        let safeties = String.Join("; ", seg.SafetyConditions.Select(fun sc -> sc.QualifiedName))
                        yield $"{tab}{seg.Name} = {lb} {safeties} {rb}"
                    yield $"{tab}{rb}"


                let bwd = rf.BackwardAliasMaps
                if bwd.Count > 0 then
                    let tab = getTab indent
                    yield $"{tab}[alias] = {lb}"
                    for KeyValue(k, v) in bwd do
                        let tab = getTab (indent+1)
                        let mnemonics = String.Join("; ", v)
                        yield $"{tab}{k.Last()} = {lb} {mnemonics} {rb}"
                    yield $"{tab}{rb}"

                for cp in rf.CallPrototypes do
                    yield serializeCallPrototype cp indent
            | :? Segment as seg ->
                let covered = seg.Edges.SelectMany(fun e -> e.Vertices)
                let xs = seg.Children.Select(fun ch -> ch.Coin).Cast<IVertex>().Except(covered).Cast<Coin>().ToArray()
                if xs.IsEmpty() then
                    closeBrace <- false

                yield $"{tab}{flow.Name}" + if xs.IsEmpty() then ";" else $" = {lb}"
                for x in xs do
                    yield $"{tab}  {x.Name};"

            | _ ->
                yield $"{tab}{flow.Name};"


            for edge in flow.Edges do
                yield serializeEdge edge indent

            let covered = flow.Edges.SelectMany(fun e -> e.Vertices).Distinct().ToArray()
            for cf in flow.ChildVertices.OfType<SegmentBase>() do
                if cf.ChildVertices.Any() || not <| covered.Contains(cf) then
                    yield! serializeFlow cf indent

            //for child in flow.ChildVertices do
            //    serializeChild child
            if closeBrace then
                yield tab + $"{rb}"
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
            yield $"[sys] {system.Name.QuoteOnDemand()} = " + "{"
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
