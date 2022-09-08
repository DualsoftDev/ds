// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System
open System.Collections.Generic

[<AutoOpen>]
module ExportModel =

    let ToText(model:DsModel) =
                               
        let callText(seg:Segment) =
            let callName =  seg.ToText()
            let tx, rx =
                let txs = HashSet<string>()
                let rxs = HashSet<string>()
                for index in [|1..seg.MaxCnt|] do
                    let causal, text = seg.PrintfTRX(index)
                    match causal with
                    |TR -> txs.Add(sprintf "EX.%s.%s" (seg.ToCallText()) (text.Replace("TR", "TX"))) |> ignore
                           rxs.Add(sprintf "EX.%s.%s" (seg.ToCallText()) (text.Replace("TR", "RX"))) |> ignore
                    |TX -> txs.Add(sprintf "EX.%s.%s" (seg.ToCallText()) text) |> ignore
                    |RX -> rxs.Add(sprintf "EX.%s.%s" (seg.ToCallText()) text) |> ignore
                    |_ -> failwithf "ERR";

                (txs |> String.concat ", " ), (rxs |> String.concat ", ") 

            sprintf "\t\t%s\t = {%s\t~\t%s}" callName tx rx

        let addressText(seg:Segment, index) =
            let callPath = seg.ToCallText()
            if(seg.NodeCausal = EX)
            then 
                let ex = sprintf "EX.%s.EX" callPath
                sprintf "%-40s \t= (%s,%s,%s)" ex seg.S seg.R seg.E
            else
                let causal, text = seg.PrintfTRX(index)
                match causal with
                |TR ->  let tx = sprintf "EX.%s.%s" callPath (text.Replace("TR", "TX"))
                        let rx = sprintf "EX.%s.%s" callPath (text.Replace("TR", "RX"))
                        sprintf "%-40s \t= (%s, , )\r\n\t%-40s \t= (, ,%s)"  tx seg.S rx seg.E 
                |TX ->  let tx = sprintf "EX.%s.%s" callPath text
                        sprintf "%-40s \t= (%s, , )"  tx seg.S
                |RX ->  let rx = sprintf "EX.%s.%s" callPath text
                        sprintf "%-40s \t= (, ,%s)"  rx seg.E
                |_ -> failwithf "ERR";

        let mergeEdges(edges:MEdge seq) =
            let tgtSegs = edges    |> Seq.map(fun edge -> edge, edge.Target) |> Seq.distinctBy(fun (edge, tgtSeg) -> edge.Causal.ToText()+tgtSeg.Key)
            //src(s) -> tgt
            let mixEdges = tgtSegs |> Seq.map(fun (edge, tgtSeg) -> edges  |> Seq.filter(fun findEdge -> findEdge.Target.Key = tgtSeg.Key)  
                                                                           |> Seq.filter(fun findEdge -> findEdge.Causal = edge.Causal)  
                                                                           |> Seq.map(fun edge -> edge.Source.ToTextInFlow())
                                                                   ,edge , tgtSeg.ToTextInFlow())
            
            mixEdges 
            
        let subEdgeText(seg:Segment) =
            seq {
                yield sprintf "\t\t%s = {"(seg.ToTextInFlow())
                let mergeEdges = mergeEdges  seg.MEdges
                for srcs, edge, tgt in mergeEdges do
                    yield sprintf "\t\t\t%s %s %s;"  (srcs |> String.concat ", ") (edge.Causal.ToText()) (tgt)
                yield sprintf "\t\t}"
            }

        let subNodeText(seg:Segment) =
            seq {
                yield sprintf "\t\t%s = {"(seg.ToTextInFlow())
                for segSub in seg.NoEdgeSegs do
                    yield sprintf "\t\t\t%s;" (segSub.ToTextInFlow())
                yield sprintf "\t\t}"
            }

        //let checkDrawEdge(seg, drawSubs:Segment seq) = 
        //    if(drawSubs.Contains(seg) && seg.MEdges.Any()) then true else false
        //let checkDrawNode(seg, drawSubs:Segment seq) = 
        //    if(drawSubs.Contains(seg) && seg.NoEdgeSegs.Any()) then true else false


        let edgeText(edges:MEdge seq) = 
            //src(s) -> tgt
            let mergeEdges = mergeEdges  edges

           
            seq {
                for srcs, edge, tgt in mergeEdges do
                    yield sprintf "\t\t%s %s %s;"  (srcs |> String.concat ", ") (edge.Causal.ToText()) (tgt)
 
            } 

        let segmentText(segs:Segment seq) = 
            seq {
                for seg in segs do
                    if(seg.MEdges.Any())     then yield! subEdgeText (seg) 
                    if(seg.NoEdgeSegs.Any()) then yield! subNodeText (seg) 
            } 
        let mySystem = 
            seq {
                for sys in  model.TotalSystems do
                    yield sprintf "[sys] %s = {"sys.Name
                    let flows = sys.RootFlow() |> Seq.filter(fun flow -> (flow.Page = Int32.MaxValue)|>not)
                    for flow in flows do
                        //Flow 출력
                        yield sprintf "\t[flow] %s = { \t" flow.Name 
                        yield! edgeText    (flow.Edges)
                        yield! segmentText (flow.ExportSegs)
                        yield "\t}"
                        //Task 출력
                        yield sprintf "\t[task] %s = {" flow.Name
                        for callSeg in flow.CallSegments() do
                            yield callText(callSeg)

                        yield "\t}"


                    //Alias 출력
                    if(sys.AliasSet.Any())
                    then 
                        yield sprintf "\t[alias] = {" 
                        for alias in sys.AliasSet do
                            yield sprintf "\t\t%s = { %s }" alias.Key (alias.Value |> String.concat "; ") 
                        yield "\t}"

                  
                    //Variable 출력
                    if(sys.VariableSet.Any())
                    then 
                        yield sprintf "\t[variable] = {" 
                        for variable in sys.VariableSet do
                            yield sprintf "\t\t%s(%s);" variable.Key (variable.Value.ToString())
                        yield "\t}"

                    //Command 출력
                    if(sys.CommandSet.Any())
                    then 
                        yield sprintf "\t[command] = {" 
                        for cmd in sys.CommandSet do
                            yield sprintf "\t\t%s = {%s};" cmd.Key cmd.Value
                        yield "\t}"

                    //Command 출력
                    if(sys.ObserveSet.Any())
                    then 
                        yield sprintf "\t[observe] = {" 
                        for obs in sys.ObserveSet do
                            yield sprintf "\t\t%s = {%s};" obs.Key obs.Value
                        yield "\t}"

                yield sprintf "} //%s" model.Path 
                yield ""
            }
            
        let address = 
            seq {
                for sys in model.TotalSystems do
                    yield sprintf "[address] = {" 
                    let flows = sys.RootFlow() |> Seq.filter(fun flow -> (flow.Page = Int32.MaxValue)|>not)
                    for flow in flows do
                        for callSeg in flow.CallSegments() do
                            for index in [|1..callSeg.MaxCnt|] do
                            yield sprintf "\t%s" (addressText(callSeg, index))

                        for exSeg in flow.ExSegments() do
                            for index in [|1..exSeg.MaxCnt|] do
                            yield sprintf "\t%s" (addressText(exSeg, index))
                    yield "}"
            }

        let layout = 
            seq {
                for sys in model.TotalSystems do
                    if(sys.LocationSet.Any())
                    then 
                        yield sprintf "[layouts] = {" 
                        for rect in sys.LocationSet do
                            let x = rect.Value.X
                            let y = rect.Value.Y
                            let w = rect.Value.Width
                            let h = rect.Value.Height
                            yield sprintf "\t%s = (%d,%d,%d,%d)" rect.Key x y w h
                        yield "}"
            }
      
      
        let exSystem = 
            let getTXs (segs: Segment seq) = 
                seq {
                        for seg in segs do
                        yield sprintf "%s.TX" (seg.ToCallText()) 
                }|> String.concat ", "
                
            seq {
                
                for sys in model.TotalSystems do
                    yield sprintf "//////////////////////////////////////////////////////"
                    yield sprintf "//%s DS system auto generation ExSegments"sys.Name
                    yield sprintf "//////////////////////////////////////////////////////"
                    yield sprintf "[sys] EX = {" 
                    for flow in sys.RootFlow()  do
                            
                        // Call InterLock
                        for calls in flow.Interlockedges do
                            for call in calls  do
                                let resets = calls |> Seq.filter(fun seg -> seg = call|>not)
                                yield sprintf "\t[flow] %s = { TX > RX <| %s; }" (call.ToCallText()) (getTXs resets)
                        // Call Without InterLock
                        for call in flow.CallWithoutInterLock() do
                            yield sprintf "\t[flow] %s = { TX > RX }" (call.ToCallText()) 
                        //Ex 출력
                        for exSeg in flow.ExSegments() do
                            yield sprintf "\t[flow] %s = { TR; }"  (exSeg.ToCallText()) 

                    yield "}"
                    yield ""
            }

        layout
        |> Seq.append  address
        |> Seq.append  exSystem
        |> Seq.append  mySystem
        |> String.concat "\r\n"







