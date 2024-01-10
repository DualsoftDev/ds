namespace Engine.Core
open System.Runtime.CompilerServices
open System.Linq
open Dual.Common.Core.FS
open GraphUtilImpl
[<AutoOpen>]
module OriginModule =
    // 초기화 유형을 정의합니다.
    type InitialType =
        | Off         // 행동 값이 반드시 0인 상태
        | On          // 행동 값이 반드시 1인 상태
        | NeedCheck   // 인터락 구성요소 중 하나가 1인지를 체크하는 상태
        | NotCare     // On 또는 Off의 상태를 따르지 않음

    // 기본 Origin 정보를 정의합니다.
    type OriginInfo = {
        Real: Real
        Tasks: (TaskDev * InitialType) seq
        ResetChains: string list seq
    }
    let defaultOriginInfo(real) = {
        Real  = real
        Tasks = [||]
        ResetChains = [||]
    }
 
    // 상호 정보를 얻습니다.
    let getMutualInfo (vertices: Vertex seq) =
        let coinNapi =
            vertices
            |> Seq.choose (fun x ->
                match x.GetPure() with
                | :? Call as c ->
                    let devs = c.TargetJob.DeviceDefs
                    Some (x, devs.First().ApiItem)
                |_ -> None)
            |> dict


        let findCoins api = 
            coinNapi.Where(fun dic -> dic.Value = api)
                    .Select(fun dic -> dic.Key)


        vertices
        |> Seq.map (fun x ->
            let call = x.GetPure() :?> Call
            let mutualCoins =
                let resets = coinNapi.[call].System.GetMutualResetApis(coinNapi.[call])
                resets |> Seq.collect (fun api -> findCoins api)
            x, mutualCoins)
        |> dict

    // 초기 유형을 얻습니다.
    let getInitialType (source: Vertex) (targets: Vertex seq) graphOrder =
        let getTypeForSingleTarget (v: bool option) =
            match v with//뒤에서 리셋이오면 항상온 
            | Some fwd -> if fwd then InitialType.Off else InitialType.On
            | None -> InitialType.NotCare

        let getTypeForMuiltTarget (vs:bool option seq)= 
                if vs.any(fun d-> d.IsNone)  //하나라도 순서없으면
                then InitialType.NeedCheck
                else
                    if vs.Choose(id).AllEqual(true)
                    then InitialType.Off 
                    elif vs.Choose(id).AllEqual(false)
                    then InitialType.On
                    else InitialType.NeedCheck

        match targets.Count() with
        | 0 -> InitialType.NotCare
        | 1 -> graphOrder source (targets.First()) |> getTypeForSingleTarget
        | _ -> targets |> Seq.map (fun t -> graphOrder source t) |> getTypeForMuiltTarget

   

    // Origin 정보를 얻습니다.
    let getOriginInfo (real: Real) =
        let graphOrder = real.Graph.BuildPairwiseComparer()
        let pureGroupAlias =
            real.Graph.Vertices.OfType<Alias>()
            |> Seq.groupBy (fun f -> f.GetPure())

        let addAlias =
            pureGroupAlias
            |> Seq.choose (fun (f, _) ->
                let pureAlias = real.Graph.Vertices |> Seq.filter (fun v -> v.GetPure() = f)
                findHeadVertex pureAlias graphOrder)

        //Alias 관련 Vertex는 일괄 걸러낸후 addAlias 로 다시 더함
        let verticesToCalculateOrigin = 
            real.Graph.Vertices.Where(fun w-> not <| pureGroupAlias.Select(fun (f, _)->f).Contains(w.GetPure()))
                               .Concat addAlias

        let mutualInfo = getMutualInfo (real.Graph.Vertices.OfType<Call>().Cast<Vertex>())
        let tasks =
            verticesToCalculateOrigin
            |> Seq.collect (fun v ->
                let initialType = getInitialType v (mutualInfo.[v]) graphOrder
                let call = v.GetPure() :?> Call
                let devs = call.TargetJob.DeviceDefs
                devs |> Seq.map (fun d -> d, initialType))

        { Real = real; Tasks = tasks; ResetChains = [||] }

// OriginHelper 타입의 확장 기능을 제공합니다.
[<Extension>]
type OriginHelper =
    // Origin 정보를 가져옵니다.
    [<Extension>]
    static member GetOriginInfo (real: Real) = getOriginInfo real

    // 태스크 이름별로 Origin 정보를 가져옵니다.
    [<Extension>]
    static member GetOriginInfoByTaskName (real: Real) =
        getOriginInfo real
        |> fun info -> info.Tasks |> Seq.map (fun (d, t) -> d.QualifiedName, t)
