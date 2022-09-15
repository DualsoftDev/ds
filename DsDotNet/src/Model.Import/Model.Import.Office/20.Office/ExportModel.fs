// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System
open System.Collections.Generic
open Engine.Base

[<AutoOpen>]
module ExportModel =

    let ToText(model:DsModel) =
                               
        let callText(seg:Seg) =
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
            let tx = if(tx = "") then "_" else tx
            let rx = if(rx = "") then "_" else rx
            sprintf "\t\t%s\t = {%s\t~\t%s}" callName tx rx

        let addressText(seg:Seg, index) =
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
            
        let subEdgeText(seg:Seg) =
            seq {
                let mergeEdges = mergeEdges  seg.MEdges
                for srcs, edge, tgt in mergeEdges do
                    yield sprintf "\t\t\t%s %s %s;"  (srcs |> String.concat ", ") (edge.Causal.ToText()) (tgt)
            }

        let subNodeText(seg:Seg) =
            seq {
                for segSub in seg.NoEdgeSegs do
                    yield sprintf "\t\t\t%s;" (segSub.ToTextInFlow())
            }

        let safetyText(flow:Flo) =
            seq {
                yield sprintf "\t\t[safety]  = {" 
                for safety in flow.Safeties do
                    let safeList = safety.Value |> Seq.map(fun seg -> seg.FullName) |> String.concat "; "
                    yield sprintf "\t\t\t%s = {%s}" safety.Key.Name safeList
                yield "\t\t}"
             }

        let edgeText(edges:MEdge seq) = 
            //src(s) -> tgt
            let mergeEdges = mergeEdges  edges

           
            seq {
                for srcs, edge, tgt in mergeEdges do
                    yield sprintf "\t\t%s %s %s;"  (srcs |> String.concat ", ") (edge.Causal.ToText()) (tgt)
 
            } 

        let segmentText(segs:Seg seq) = 
            seq {
                for seg in segs do
                    if(seg.MEdges.Any() || seg.NoEdgeSegs.Any())    
                    then 
                        yield sprintf "\t\t%s = {"(seg.ToTextInFlow())
                        yield! subEdgeText (seg) 
                        yield! subNodeText (seg) 
                        yield sprintf "\t\t}"
            } 
        let mySystem = 
            seq {
                yield sprintf "//////////////////////////////////////////////////////"
                yield sprintf "//DTS model auto generation from %s" model.Path 
              //  yield sprintf "//DTS model auto generation"
                yield sprintf "//////////////////////////////////////////////////////"
                for sys in  model.TotalSystems do
                    yield sprintf "[sys] %s = {"sys.Name
                    let flows = sys.RootFlow() 
                    for flow in flows do
                        //Flow 출력

                        yield sprintf "\t[flow] %s = { \t" (flow.ToText())
                        
                        yield! edgeText    (flow.Edges)
                        yield! segmentText (flow.ExportSegs)
                        //Task 출력
                        for callSeg in flow.CallSegs() do
                            yield callText(callSeg)
                        
                        //Alias 출력
                        if(flow.AliasSet.Any())
                        then 
                            yield sprintf "\t\t[alias] = {" 
                            for alias in flow.AliasSet do
                                yield sprintf "\t\t\t%s = { %s }" alias.Key (alias.Value |> String.concat "; ") 
                            yield "\t\t}"
                        //Safeties  출력
                        if(flow.Safeties.Any()) then yield! safetyText flow 

                        yield "\t}"

                    //EmgSet 출력
                    if(sys.EmgSet.Any())
                    then 
                        yield sprintf "\t[emg] = {" 
                        for emg in sys.EmgSet do
                            yield sprintf "\t\t%s = { %s };" emg.Key (emg.Value |>Seq.map(fun flo-> flo.ToText()) |> String.concat "; ") 
                        yield "\t}"
                    
                    //AutoSet 출력
                    if(sys.AutoSet.Any())
                    then 
                        yield sprintf "\t[auto] = {" 
                        for auto in sys.AutoSet do
                            yield sprintf "\t\t%s = { %s };" auto.Key (auto.Value |>Seq.map(fun flo-> flo.ToText()) |> String.concat "; ") 
                        yield "\t}"
                    
                    //StartSet 출력
                    if(sys.StartSet.Any())
                    then 
                        yield sprintf "\t[start] = {" 
                        for start in sys.StartSet do
                            yield sprintf "\t\t%s = { %s };" start.Key (start.Value |>Seq.map(fun flo-> flo.ToText()) |> String.concat "; ") 
                        yield "\t}"

                    //ResetSet 출력
                    if(sys.ResetSet.Any())
                    then 
                        yield sprintf "\t[reset] = {" 
                        for reset in sys.ResetSet do
                            yield sprintf "\t\t%s = { %s };" reset.Key (reset.Value |>Seq.map(fun flo-> flo.ToText()) |> String.concat "; ") 
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

                yield sprintf "}"  
                yield ""
            }

        let cpus = 
            seq {
                yield sprintf "[cpus] AllCpus = {" 
                for sys in model.TotalSystems do
                    yield sprintf "\t[cpu] Cpu_%s = {" sys.Name
                    let flows = sys.RootFlow() 
                    //my CPU
                    for flow in flows do    
                        yield sprintf "\t\t%s.%s;" sys.Name (flow.ToText())
                    yield "\t}"
                    //ex CPU
                    yield sprintf "\t[cpu] Cpu_EX = {" 
                    for flow in flows do  
                        for callSeg in (flow.CallSegs() |> Seq.append (flow.ExSegs())) do
                            yield sprintf "\t\tEX.%s;" (callSeg.ToCallText())
                    yield "\t}"
                yield "}"
            }  

        let address = 
            seq {
                for sys in model.TotalSystems do
                    yield sprintf "[addresses] = {" 
                    let flows = sys.RootFlow() 
                    for flow in flows do
                        for callSeg in flow.CallSegs() do
                            for index in [|1..callSeg.MaxCnt|] do
                            yield sprintf "\t%s" (addressText(callSeg, index))

                        for exSeg in flow.ExSegs() do
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
            let getTRXs (segs: Seg seq ,skip:NodeCausal, bReset:bool) = 
                seq {
                        for seg in segs do
                            for index in [|1..seg.MaxCnt|] do
                                let causal, text = seg.PrintfTRX(index)
                                if(causal = skip|>not)
                                then
                                    let text = (text.Replace("TR", (if(skip = TX) then "RX" else "TX")))
                                    if(bReset) 
                                    then yield sprintf "%s.%s" (seg.ToCallText()) text
                                    else yield sprintf "%s"  text
                }|> String.concat ", "
                
            seq {
                
                for sys in model.TotalSystems do
                    yield sprintf "//////////////////////////////////////////////////////"
                    yield sprintf "//DTS auto generation %s ExSegs"sys.Name
                    yield sprintf "//////////////////////////////////////////////////////"
                    yield sprintf "[sys] EX = {" 
                    for flow in sys.RootFlow()  do
                            
                        // Call InterLock
                        for calls in flow.Interlockedges do
                            for call in calls  do
                                let resets = calls |> Seq.filter(fun seg -> seg = call|>not)
                                let txs =  getTRXs ([call], RX ,false)
                                let rxs =  getTRXs ([call], TX, false)
                                let resetTxs =  getTRXs (resets, RX, true)
                                yield sprintf "\t[flow] %s = { %s > %s <| %s; }" (call.ToCallText()) txs rxs resetTxs
                        // Call Without InterLock
                        for call in flow.CallWithoutInterLock() do
                            yield sprintf "\t[flow] %s = { TX > RX }" (call.ToCallText()) 
                        //Ex 출력
                        for exSeg in flow.ExSegs() do
                            yield sprintf "\t[flow] %s = { TR; }"  (exSeg.ToCallText()) 

                    yield "}"
                    yield ""
            }

        layout
        |> Seq.append  address
        |> Seq.append  cpus
        |> Seq.append  exSystem
        |> Seq.append  mySystem
        |> String.concat "\r\n"







