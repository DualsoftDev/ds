// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Linq
open System
open System.Collections.Generic
open Engine.Base
open System.Collections.Concurrent

[<AutoOpen>]
module ExportModel =

    let ToText(model:DsModel) =
                               
        let callText(seg:Seg) =
            let callName =  seg.SegName
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
            let callPath =  if(seg.Bound = ExBtn) then seg.SegName else seg.ToCallText()
            if(seg.NodeCausal = EX)
            then 
                let ex = sprintf "EX.%s.EX" callPath
                sprintf "%-40s \t= (%s,%s,%s)" ex seg.TextStart seg.TextReset seg.TextEnd
            else
                let causal, text = seg.PrintfTRX(index)
                match causal with
                |TR ->  let tx = sprintf "EX.%s.%s" callPath (text.Replace("TR", "TX"))
                        let rx = sprintf "EX.%s.%s" callPath (text.Replace("TR", "RX"))
                        sprintf "%-40s \t= (%s, , )\r\n\t%-40s \t= (, ,%s)"  tx seg.TextStart rx seg.TextEnd 
                |TX ->  let tx = sprintf "EX.%s.%s" callPath text
                        sprintf "%-40s \t= (%s, , )"  tx seg.TextStart
                |RX ->  let rx = sprintf "EX.%s.%s" callPath text
                        sprintf "%-40s \t= (, ,%s)"  rx seg.TextEnd
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

        let btnText(propName:string, set: ConcurrentDictionary<string, List<Flo>>) = 
            seq {
                        yield sprintf "\t[%s] = {"  propName
                        for emg in set do
                            yield sprintf "\t\t%s = { %s };" emg.Key (emg.Value |>Seq.map(fun flo-> flo.ToText()) |> String.concat "; ") 
                        yield "\t}"
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
                        yield! segmentText (flow.UsedSegs)
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

                    //EmgSet AutoSet StartSet ResetSet 출력
                    if(sys.EmgSet.Any()) then yield! btnText(TextEmergencyBTN, sys.GetBtnSet(EmergencyBTN))
                    if(sys.AutoSet.Any()) then yield! btnText(TextAutoBTN, sys.GetBtnSet(AutoBTN))
                    if(sys.StartSet.Any())then yield! btnText(TextStartBTN, sys.GetBtnSet(StartBTN))
                    if(sys.ResetSet.Any())then yield! btnText(TextResetBTN, sys.GetBtnSet(ResetBTN))
                    
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
                        for callSeg in flow.NotMySegs() do
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
                        for callSeg in flow.NotMySegs() do
                            for index in [|1..callSeg.MaxCnt|] do
                            yield sprintf "\t%s" (addressText(callSeg, index))
                    for exSeg in sys.BtnSegs() do
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
                }
                
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
                                let txs =  getTRXs ([call], RX ,false)|> String.concat ", "
                                let rxs =  getTRXs ([call], TX, false)|> String.concat ", "
                                let resetTxs =  getTRXs (resets, RX, true)|> String.concat ", "
                                yield sprintf "\t[flow] %s = { %s > %s <| %s; }" (call.ToCallText()) txs rxs resetTxs
                        // Call Without InterLock
                        let noInterLocks =flow.CallWithoutInterLock()
                        for call in noInterLocks do
                            yield sprintf "\t[flow] %s = { TX > RX; }" (call.ToCallText()) 
                        //Ex 출력
                        for exSeg in flow.ExRealSegs() do
                            yield sprintf "\t[flow] %s = { EX; }"  (exSeg.ToCallText()) 
                    //Ex 버튼 출력
                    for exSeg in sys.BtnSegs() do
                        yield sprintf "\t[flow] %s = { %s; }"  exSeg.SegName  (getTRXs ([exSeg], TX ,false)|> String.concat "; ")

                    yield "}"
                    yield ""
            }

        layout
        |> Seq.append  address
        |> Seq.append  cpus
        |> Seq.append  exSystem
        |> Seq.append  mySystem
        |> String.concat "\r\n"







