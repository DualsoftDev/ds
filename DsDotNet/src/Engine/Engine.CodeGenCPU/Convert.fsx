//namespace Engine.CodeGenCPU

//open System.Linq
//open Engine.Core
//open Engine.Common.FS

//[<AutoOpen>]
//module CpuConvertModule =
//    let private getVertexManager(v:Vertex) = v.VertexManager :?> VertexManager

//    let getSrcMemorys(v:Vertex, graph:DsGraph, edgeType:ModelingEdgeType) =
//        graph.FindEdgeSources(v, edgeType). Select(getVertexManager)

//    let createRungsForReal(real:Real, graph:DsGraph) =
//        let realTag = real.VertexManager :?> VertexManager
//        let vs = graph.Vertices
//        let vms = vs.Select(getVertexManager).ToArray()

//        //R0. Real 행위의 Coin 상태수식 제공
//        let r0s =
//            [ for v in vs do
//                yield! (getVertexManager v).S1_Ready_Going_Finish_Homing() ]

//        //R1.Real 초기시작 Statement 만들기
//        let r1 = realTag.CreateInitStartRung()
//        //R2.Real 작업완료 Statement 만들기
//        let r2 = realTag.CreateRealEndRung(vms)

//        //C1 Call 시작조건 Statement 만들기
//        let c1s = vs.Select(fun coin ->
//                 let srcs = getSrcMemorys(coin, graph, StartEdge)
//                 (getVertexManager coin).CreateCallStartRung(srcs, realTag))


//        //C2 Call 작업완료 Statement 만들기
//        let c2s =
//            vs.Select(fun coin ->
//                let srcs = getSrcMemorys(coin, graph, StartEdge)
//                let vm = (getVertexManager coin)
//                vm.CreateCallRelayRung(srcs, coin.GetCoinTags(vm, true), realTag))

//        //C3 Call 시작출력 Statement 만들기
//        let c3s =
//            vs.SelectMany(fun coin ->
//            let vm = (getVertexManager coin)
//            vm.CreateOutputRungs(coin.GetCoinTags(vm, false)))

//        //C4 Call Start to Api TX.Start Statement 만들기
//        let c4s =
//            vs.SelectMany(fun coin ->
//                let vm = (getVertexManager coin)
//                let txTags = coin.GetTxRxTags(true, vm)
//                vm.CreateLinkTxRungs(txTags))

//        //C5 Call End from  Api RX.End  Statement 만들기
//        let c5s =
//            [   for coin in vs do
//                    let vm = (getVertexManager coin)
//                    let rxTags = coin.GetTxRxTags(false, vm)
//                    yield! vm.TryCreateLinkRxStatement(rxTags) |> Option.toList
//            ]
//        //C6 Call Tx ~ Rx 내용없을시 Coin Start-End 직접연결
//        let c6s =
//            if c4s.IsEmpty() && c5s.isEmpty() then
//                vs.Select(fun coin ->
//                    let vm = (getVertexManager coin)
//                    vm.CreateDirectLinkRung())
//            else
//                []

//        //description, statement List 출력
//        r0s.Select(fun s-> "Real-r0", s)
//            @  ["Real-r1",r1; "Real-r2",r2]
//            @  c1s.Select(fun s-> "Call-c1", s)
//            @  c2s.Select(fun s-> "Call-c2", s)
//            @  c3s.Select(fun s-> "Call-c3", s)
//            @  c4s.Select(fun s-> "Call-c4", s)
//            @  c5s.Select(fun s-> "Call-c5", s)
//            @  c6s.Select(fun s-> "Call-c6", s)


//    /// Real  Start/Reset 엣지 릴레이를 통한 제어 statement 생성
//    let createRungsForRoot(real:Real, graph:DsGraph) =
//        let vm = (getVertexManager real)

//        let f0s = vm.S1_Ready_Going_Finish_Homing()

//            //F1. Real 자신의    Start Statement 만들기
//        let srcs = getSrcMemorys(real, graph, StartEdge)
//        let f1 = vm.F1_RootStart()

//            //F2. Real 자신의 Reset going relay  Statement 만들기
//        //let srcs = getSrcMemorys(real, graph, ResetEdge)
//        //let goingRelays = srcs.Select(fun c -> c, DsTag($"{c.Name}(gr)", false) :> Tag<bool>) |> dict
//        //let f2s = srcs.Select(fun c ->
//        //            c.CreateResetGoingRung(vm, goingRelays[c]))

//        //    //F3. Real 자신의    Reset Statement 만들기
//        //let f3 = vm.TryCreateRealResetRung(goingRelays.Values)

//           //description, statement List 출력
//        f0s.Select(fun s -> "Flow-f0", s)
//        @ (f1 |> Option.toList).Select(fun s-> "Flow-f1", s)
//        //@ (f3 |> Option.toList).Select(fun s-> "Flow-f3", s)
//        //@ f2s.Select(fun s -> "Flow-f2", s)

