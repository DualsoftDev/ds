namespace Engine.Cpu

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
        let r1 = realTag.CreateRungForInitStart()
        //R2.Real 작업완료 Statement 만들기
        let r2 = realTag.CreateRungForRealEnd(cv.Select(fun f->dicM[f]))

        //C1 Call 시작조건 Statement 만들기
        let c1 = cv.Select(fun c ->
                 let srcs = getSrcMemorys(c, graph, StartEdge)
                 dicM[c].CreateRungForCallStart(srcs, realTag))
        //C2 Call 작업완료 Statement 만들기
        let c2 = cv.Select(fun c ->
                 let srcs = getSrcMemorys(c, graph, StartEdge)
                 dicM[c].CreateRungForCallRelay(srcs, [PlcTag.Create(c.Name, false)], realTag))
        //C3 Call 시작출력 Statement 만들기
        let c3 = cv.Select(fun c ->
                 dicM[c].CreateRungForOutput(PlcTag.Create(c.Name, false)))

        //C4 Call Start to Api TX.Start Statement 만들기
        let c4 = cv.Select(fun c ->
                 dicM[c].CreateRungForLinkTx())  //구현필요
        //C5 Call End from  Api RX.End  Statement 만들기
        let c5 = cv.Select(fun c ->
                 dicM[c].CreateRungForLinkRx())  //구현필요


        [r1; r2] @ c1 @ c2 @ c3 @ c4 @ c5



    /// real 의 가상 부모 만들어서 가상 부모를 통한 제어 statement 생성
    let createRungsForRoot(real:Real, graph:DsGraph) =
        let parentTag = DsMemory($"{real.Name}(p)")

        //F1. child Real Start Statement 만들기
        let parentGraph = DsGraph()
        parentGraph.AddVertex(real) |> ignore
        let f1 = createRungsForReal(parentTag, parentGraph)

        //F2. Real 자신의 Reset going relay  Statement 만들기
        let srcs = getSrcMemorys(real, graph, ResetEdge)
        let goingRelays = srcs.Select(fun c -> c, DsMemory($"{c.Name}(gr)")) |> dict
        let f2 = srcs.Select(fun c ->
                 c.CreateRungForResetGoing(dicM[real], goingRelays[c]))

        //F3. Real 자신의    Reset Statement 만들기
        let f3 = dicM[real].TryGetRealResetStatement(goingRelays.Values)
        //F4. Real 부모의 시작조건 Statement 만들기
        let srcs = getSrcMemorys(real, graph, StartEdge)
        let f4 = parentTag.TryCreateRungForRealStart(srcs)
        //F5. Real 부모의 셀프리셋 Statement 만들기
        let f5 = parentTag.CreateRungForResetSelf()

        let statements = f1 @ f2 @ [f5] @ (f3 |> Option.toList) @ (f4 |> Option.toList)
        statements

    let ConvertSystem(sys:DsSystem) =
        dicM.Clear()

        //let aliasSet = ConcurrentDictionary<Alias, Vertex>() //Alias, target
        //let vertices = sys.GetVertices()

        ////모든 인과 대상 Node 메모리화
        //vertices.ForEach(fun v ->
        //    match v with
        //        | :? Real as r -> dicM.TryAdd(v, DsMemory($"{r.QualifiedName}")) |> verifyM $"Duplicated system name [{v.QualifiedName}]"
        //        | :? Call as c -> dicM.TryAdd(c, DsMemory($"{c.QualifiedName}")) |> verifyM $"Duplicated system name [{v.QualifiedName}]"
        //        | :? Alias as a ->
        //                    match a.Target with
        //                    | RealTarget r -> aliasSet.TryAdd(a, r) |> verifyM $"Duplicated system name [{v.QualifiedName}]"
        //                    | CallTarget c -> aliasSet.TryAdd(a, c) |> verifyM $"Duplicated system name [{v.QualifiedName}]"
        //        | _-> ()
        //)


        ////Alias 원본 메모리 매칭
        //aliasSet.ForEach(fun alias->
        //    let vertex = vertices.First(fun f->f  =alias.Value)
        //    dicM.TryAdd(alias.Key, dicM.[vertex]) |> ignore
        //)

        //sys.Flows.SelectMany(fun flow->
        //flow.Graph.Vertices
        //    .Where(fun w->w :? Real).Cast<Real>()
        //    .SelectMany(fun r->
        //        createReal(dicM[r], r.Graph)
        //        @@ createRoot(r, flow.Graph)
        //    )
        //)
