namespace Engine.CodeGenCPU

open System.Collections.Concurrent
open System.Linq
open Engine.Core
open Engine.Common.FS

[<AutoOpen>]
module CpuConvertModule =

    //Dictionary memory TAG 관리
    let dicM = ConcurrentDictionary<Vertex, DsMemory>()

    let getSrcMemorys(v:Vertex, graph:DsGraph, edgeType:ModelingEdgeType) =
        v.FindEdgeSources(graph, edgeType).Select(fun v-> dicM[v])

    let createRungsForReal(realTag:DsMemory, graph:DsGraph) =
        let cv = graph.Vertices

        //R1.Real 초기시작 Statement 만들기
        let r1 = realTag.CreateInitStart()
        //R2.Real 작업완료 Statement 만들기
        let r2 = realTag.CreateRealEnd(cv.Select(fun v->dicM[v]))

        //C1 Call 시작조건 Statement 만들기
        let c1s = cv.Select(fun coin ->
                 let srcs = getSrcMemorys(coin, graph, StartEdge)
                 dicM[coin].CreateCallStart(srcs, realTag))
        //C2 Call 작업완료 Statement 만들기
        let c2s = cv.Select(fun coin ->
                  let srcs = getSrcMemorys(coin, graph, StartEdge)
                  dicM[coin].CreateCallRelay(srcs, coin.GetCoinTags(dicM[coin], true), realTag))

        //C3 Call 시작출력 Statement 만들기
        let c3s = cv.SelectMany(fun coin ->
                  dicM[coin].CreateOutputs(coin.GetCoinTags(dicM[coin], false)))

        //C4 Call Start to Api TX.Start Statement 만들기
        let c4s = cv.SelectMany(fun coin ->
                  let txTags = coin.GetTxRxTags(true, dicM)
                  dicM[coin].CreateLinkTxs(txTags))

        //C5 Call End from  Api RX.End  Statement 만들기
        let c5s = cv.SelectMany(fun coin ->
                  let rxTags = coin.GetTxRxTags(false, dicM)
                  dicM[coin].CreateLinkRx(rxTags) |> Option.toList |> List.toSeq
                  )
        //C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
        let c6s  = if c4s.IsEmpty() && c5s.IsEmpty()
                   then cv.Select(fun coin ->dicM[coin].CreateDirectLink())
                   else []

        //description, statement List 출력
        ["Real-r1",r1; "Real-r2",r2] 
        @ c1s.Select(fun s-> "Real-c1", s)
        @ c2s.Select(fun s-> "Real-c2", s)
        @ c3s.Select(fun s-> "Real-c3", s)
        @ c4s.Select(fun s-> "Real-c4", s)
        @ c5s.Select(fun s-> "Real-c5", s)
        @ c6s.Select(fun s-> "Real-c6", s)


    /// real 의 가상 부모 만들어서 가상 부모를 통한 제어 statement 생성
    let createRungsForRoot(real:Real, graph:DsGraph) =
        if getSrcMemorys(real, graph, StartEdge) @ getSrcMemorys(real, graph, ResetEdge) |> Seq.any
        then 
            let parentTag = DsMemory($"{real.QualifiedName}(p)")

            //F1. child Real Start Statement 만들기
            let parentGraph = DsGraph()
            parentGraph.AddVertex(real) |> ignore
            let f1s = createRungsForReal(parentTag, parentGraph)

            //F2. Real 자신의 Reset going relay  Statement 만들기
            let srcs = getSrcMemorys(real, graph, ResetEdge)
            let goingRelays = srcs.Select(fun c -> c, DsMemory($"{c.Name}(gr)")) |> dict
            let f2s = srcs.Select(fun c ->
                     c.CreateResetGoing(dicM[real], goingRelays[c]))

            //F3. Real 자신의    Reset Statement 만들기
            let f3 = dicM[real].TryGetRealResetStatement(goingRelays.Values)
            //F4. Real 부모의 시작조건 Statement 만들기
            let srcs = getSrcMemorys(real, graph, StartEdge)
            let f4 = parentTag.TryCreateRealStart(srcs)
            //F5. Real 부모의 셀프리셋 Statement 만들기
            let f5 = parentTag.CreateResetSelf()

            //description, statement List 출력
            f1s.Select(fun (s, f1)-> $"Root:{s}", f1)
            @ f2s.Select(fun s-> "Flow-f2", s)
            @ (f3 |> Option.toList).Select(fun s-> "Flow-f3", s)
            @ (f4 |> Option.toList).Select(fun s-> "Flow-f4", s)
            @ ["Flow-f5", f5]
        
        else Seq.empty

    let ConvertSystem(sys:DsSystem) =
        dicM.Clear()

        let aliasSet = ConcurrentDictionary<Alias, Vertex>() //Alias, target
        let vertices = sys.GetVertices() @ sys.ReferenceSystems.SelectMany(fun s-> s.GetVertices())

        //모든 인과 대상 Node 메모리화
        vertices.ForEach(fun v ->
            match v with
                | :? Real as r -> dicM.TryAdd(v, DsMemory($"{r.QualifiedName}")) |> verifyM $"Duplicated system name [{v.QualifiedName}]"
                | :? Call as c -> dicM.TryAdd(c, DsMemory($"{c.QualifiedName}")) |> verifyM $"Duplicated system name [{v.QualifiedName}]"
                | :? Alias as a ->
                            match a.TargetVertex with
                            | AliasTargetReal r -> aliasSet.TryAdd(a, r)        |> verifyM $"Duplicated system name [{v.QualifiedName}]"
                            | AliasTargetRealEx rEx -> aliasSet.TryAdd(a, rEx)  |> verifyM $"Duplicated system name [{v.QualifiedName}]"
                            | AliasTargetCall c -> aliasSet.TryAdd(a, c)        |> verifyM $"Duplicated system name [{v.QualifiedName}]"
                | _-> ()
        )

        //Alias 원본 메모리 매칭
        aliasSet.ForEach(fun alias->
            let vertex = vertices.First(fun f->f  =alias.Value)
            dicM.TryAdd(alias.Key, dicM.[vertex]) |> ignore
        )
        let statements = 
            sys.Flows.SelectMany(fun flow->
            flow.Graph.Vertices
                .Where(fun w->w :? Real).Cast<Real>()
                .SelectMany(fun r->
                    createRungsForReal(dicM[r], r.Graph)
                    @ 
                    createRungsForRoot(r, flow.Graph)
                )
            )

        statements, dicM
