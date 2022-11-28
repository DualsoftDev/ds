namespace Engine.Cpu

open System.Collections.Concurrent
open System.Linq
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module CpuConvertModule =

    //Dictionary memory TAG 관리
    let dicM = ConcurrentDictionary<Vertex, DsTag>()

    let createReal(realTag:DsTag, graph:DsGraph) =
        let cv = graph.Vertices
        
        //R1.Real 초기시작 Statement 만들기
        let r1 = realTag.InitStart()
        //R2.Real 작업완료 Statement 만들기
        let r2 = realTag.TaskEnd(cv.Select(fun f->dicM[f]))
        
        //C1 Call 시작조건 Statement 만들기
        let c1 = cv.Select(fun c -> 
                 dicM[c].StartRung(c.IncomingStart(graph, dicM), realTag))
        //C2 Call 작업완료 Statement 만들기
        let c2 = cv.Select(fun c ->
                 dicM[c].RelayRung(c.IncomingStart(graph, dicM), [PlcTag.Create(c.Name, false)], realTag))
        //C3 Call 시작출력 Statement 만들기
        let c3 = cv.Select(fun c -> 
                 dicM[c].OutputRung(PlcTag.Create(c.Name, false)))

        //C4 Call Start to Api TX.Start Statement 만들기
        let c4 = cv.Select(fun c -> 
                 dicM[c].LinkTx())  //구현필요
        //C5 Call End from  Api RX.End  Statement 만들기
        let c5 = cv.Select(fun c -> 
                 dicM[c].LinkRx())  //구현필요

        [r1; r2] 
        |>Seq.append c1 
        |>Seq.append c2 
        |>Seq.append c3 
        |>Seq.append c4 
        |>Seq.append c5 

       
    let createRoot(real:Real, graph:DsGraph) =
        let parentTag = SegmentTag<byte>.Create($"{real.Name}(p)")
        
        //F1. child Real Start Statement 만들기
        let parentGraph = DsGraph()
        parentGraph.AddVertex(real) |> ignore
        let f1 = createReal(parentTag, parentGraph)

        //F2. Real 자신의 Reset going relay  Statement 만들기
        let srcs = real.IncomingReset(graph, dicM)
        let goingRelays = srcs.Select(fun c -> c, SegmentTag<byte>.Create($"{c.Name}(gr)")) |> dict
        let f2 = srcs.Select(fun c -> 
                 c.ResetGoingRelay(dicM[real], goingRelays[c]))

        //F3. Real 자신의    Reset Statement 만들기
        let f3 = dicM[real].ResetCondition(goingRelays.Values)
        //F4. Real 부모의 시작조건 Statement 만들기
        let srcs = real.IncomingStart(graph, dicM)
        let f4 = parentTag.StartCondition(srcs)
        //F5. Real 부모의 셀프리셋 Statement 만들기
        let f5 = parentTag.ResetSelf()
        
        [f3;f4;f5] 
        |>Seq.append f1 
        |>Seq.append f2 

   
    let ConvertSystem(sys:DsSystem) =
        dicM.Clear()

        let aliasSet = ConcurrentDictionary<AliasDef, Vertex>() //Alias, target
        let vertices = sys.GetVertices()

        //모든 인과 대상 Node 메모리화
        vertices.ForEach(fun v -> 
            match v with
                | :? Real as r -> dicM.TryAdd(v, SegmentTag<byte>.Create($"{r.QualifiedName}")) |> ignore
                | :? VertexCall as c -> dicM.TryAdd(c, SegmentTag<byte>.Create($"{c.QualifiedName}")) |> ignore
                //| :? VertexAlias as a ->    //ahn
                //            match a.Target with
                //            | RealTarget r -> aliasSet.TryAdd(a, r) |>ignore
                //            | CallTarget c -> aliasSet.TryAdd(a, c) |>ignore
                | _-> ()  
        )


        //Alias 원본 메모리 매칭
        //aliasSet.ForEach(fun alias->  //ahn
        //    let vertex = vertices.First(fun f->f  =alias.Value)
        //    dicM.TryAdd(alias.Key, dicM.[vertex]) |> ignore
        //)

        sys.Flows.SelectMany(fun flow->
        flow.Graph.Vertices
            .Where(fun w->w :? Real).Cast<Real>() 
            .SelectMany(fun r-> 
                createReal(dicM[r], r.Graph)
                |>Seq.append (createRoot(r, flow.Graph)) 
            )
        )
