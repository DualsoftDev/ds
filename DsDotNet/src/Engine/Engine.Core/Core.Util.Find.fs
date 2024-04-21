namespace Engine.Core

open System.Linq
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module internal ModelFindModule =
    let nameComponentsEq (Fqdn(ys)) (xs:IQualifiedNamed) = xs.NameComponents = ys
    let nameEq (name:string) (x:INamed) = x.Name = name
   
    let tryFindSystemInner (system:DsSystem) (xs:string list) : obj option =
        match xs with
        | [] -> Some system
        | f::xs1 when system.Flows.Any(nameEq f) ->
            let flow = system.Flows.First(nameEq f)
            match xs1 with
            | [] -> Some flow
            | r::xs2 ->
                match flow.Graph.FindVertex(r) |> box with
                | :? Call as call-> Some call
                | :? Real as real->
                    match xs2 with
                    | [] -> Some real
                    | remaining ->
                        option {
                            let! v = real.Graph.TryFindVertex(remaining.Combine())
                            return box v
                        }
                | _ -> None

        | dev::xs when system.LoadedSystems.Any(nameEq dev) ->
            let device = system.LoadedSystems.Find(nameEq dev)
            match xs with
            | [] -> Some device
            | _ -> None
        | [x] -> failwithlog $"tryFindSystemInner error : single fqdn {x}"
        | _ -> failwithlog "ERROR"

    let tryFindGraphVertex(system:DsSystem) (Fqdn(fqdn)) : obj option =
        //let inline nameComponentsEq xs ys = (^T: (member NameComponents: Fqdn) xs) = (^T: (member NameComponents: Fqdn) ys)
        let fqdn = fqdn.ToFSharpList()
        match fqdn with
        | [] -> failwithlog "ERROR: name not given"
        | _ -> tryFindSystemInner system fqdn

    let tryFindGraphVertexT<'V when 'V :> IVertex>(system:DsSystem) (Fqdn(fqdn)) =
        option {
            let! v = tryFindGraphVertex system fqdn
            if typedefof<'V>.IsAssignableFrom(v.GetType()) then
                return v :?> 'V
            else
                failwithlog "ERROR"
        }

    let tryFindFlow(system:DsSystem) (name:string)    = system.Flows.TryFind(nameEq name)
    let tryFindJob (system:DsSystem) name             = system.Jobs.TryFind(nameEq name)
    let tryFindFunc (system:DsSystem) name            = system.Functions.TryFind(fun s->s.Name = name)
    let tryFindExternalSystem (system:DsSystem) name  = system.ExternalSystems.TryFind(nameEq name)
    let tryFindLoadedSystem (system:DsSystem) name    = system.LoadedSystems.TryFind(fun s->s.LoadedName = name)
    let tryFindReferenceSystem (system:DsSystem) name = system.LoadedSystems.Select(fun s->s.ReferenceSystem).TryFind(nameEq name)

    let rec tryFindExportApiItem(system:DsSystem) (Fqdn(apiPath)) =
        let _sysName, apiKey = apiPath[0], apiPath[1]
        system.ApiItems.TryFindWithName(apiKey)

    and tryFindCallingApiItem (system: DsSystem) (targetSystemName: string) (targetApiName: string) (allowAutoGenDevice: bool) =
        let findedLoadedSystem = tryFindLoadedSystem system targetSystemName

        match findedLoadedSystem with
        | Some loadedSystem ->
            let targetSystem = loadedSystem.ReferenceSystem
            system.ApiUsages.TryFind(nameComponentsEq [targetSystem.Name; targetApiName])
        | None ->
            if allowAutoGenDevice then None
            else  failwithf $"해당 디바이스를 Loading 해야 합니다. \n [device file= path] {targetSystemName}"

    //jobs 에 등록 안되있으면 Real로 처리 한다.
    let tryFindCall (system:DsSystem) (Fqdn(callPath))=
        //let job = tryFindJob system (callPath.Last())
        //let func = tryFindFunc system (callPath.Last())
        //if job.IsSome  || func.IsSome
        //then
        if callPath.Length = 1 then None
        else 
            match tryFindGraphVertex system callPath with
                 |Some(v) ->
                    match v with
                    | :? Call -> Some(v)
                    | _ -> None
                 |_ -> None
        //else None

    //let tryFindReal system flowName name =
    //    let flow = tryFindFlow system flowName |> Option.get
    //    match flow.Graph.TryFindVertex(name) with
    //    |Some(v) -> if v:? Real then Some(v :?> Real) else None
    //    |None -> None
    let tryFindReal (system:DsSystem) (path:string list) =
        match path with
        | f::_ when system.Flows.Any(nameEq f) ->
            let flow = tryFindFlow system f |> Option.get
            match flow.Graph.TryFindVertex(path.Last()) with
            |Some(v) -> if v:? Real then Some(v :?> Real) else None
            |None -> None
        | s::xs2 when system.Name = s ->
            let real = tryFindSystemInner system xs2
            match real with
            |Some(v) -> if v:? Real then Some(v :?> Real) else None
            |None -> None
        | _ -> None

    let tryFindAliasTarget (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values
            .Where(fun ad -> ad.Mnemonics.Contains(aliasMnemonic))
            .TryExactlyOne()
            .Bind(fun ad -> ad.AliasTarget)

    let tryFindAliasDefWithMnemonic (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values.TryFind(fun ad -> ad.Mnemonics.Contains(aliasMnemonic))

    let ofAliasForCallVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.CallTarget().IsSome)

    let ofAliasForRealVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.RealTarget().IsSome)

    let ofAliasForRealExVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.RealExFlowTarget().IsSome)


    
    let getVerticesOfSystem(system:DsSystem) =
        let realVertices = system.Flows.SelectMany(fun f ->
                                    f.Graph.Vertices.OfType<Real>()
                                        .SelectMany(fun r -> r.Graph.Vertices.Cast<Vertex>()))

        let flowVertices = system.Flows.SelectMany(fun f -> f.Graph.Vertices.Cast<Vertex>())
        realVertices @ flowVertices

    let getVerticesOfFlow(flow:Flow) =
        let realVertices =
            flow.Graph.Vertices.OfType<Real>()
                .SelectMany(fun r -> r.Graph.Vertices.Cast<Vertex>())

        let flowVertices =  flow.Graph.Vertices.Cast<Vertex>()
        realVertices @ flowVertices

    let getDevicesOfFlow(flow:Flow) =
        let devNames = getVerticesOfFlow(flow).OfType<Call>()   
                             .SelectMany(fun c->c.TargetJob.DeviceDefs.Select(fun d->d.DeviceName))

        flow.System.Devices.Where(fun d -> devNames.Contains d.Name)

    let getDistinctApis(x:DsSystem) =
        getVerticesOfSystem(x).OfType<Call>()   
                             .SelectMany(fun c-> c.TargetJob.ApiDefs)
                             .Distinct()

    let getVertexSharedReal(real:Real) =
        let vs = real.Flow.System |> getVerticesOfSystem
        let sharedAlias =
            let reals = (real.Flow.Graph.Vertices |> ofAliasForRealVertex)
            let realExs = (real.Flow.Graph.Vertices |> ofAliasForRealExVertex)
            (reals@realExs).Where(fun a -> a.TargetWrapper.RealTarget().Value = real)
                .Cast<Vertex>()

        let sharedRealExFlow =
                vs
                 .OfType<RealExF>()
                 .Where(fun w-> w.Real = real)
                 .Cast<Vertex>()

        sharedAlias @ sharedRealExFlow

    let getVertexSharedCall(call:Call) =
        let sharedAlias =
            (call.Parent.GetFlow() |> getVerticesOfFlow |> ofAliasForCallVertex)
              .Where(fun a -> a.TargetWrapper.CallTarget().Value = call)
              .Cast<Vertex>()

        sharedAlias

    ///Real 자신을 공용으로 사용하는 Vertex들
    let getSharedReal(v:Vertex) : Vertex seq =
            (v :?> Real) |> getVertexSharedReal

        ///Call 자신을 공용으로 사용하는 Vertex들
    let getSharedCall(v:Vertex) : Vertex seq =
            (v :?> Call) |> getVertexSharedCall

 

    type DsSystem with
        member x.TryFindGraphVertex(Fqdn(fqdn)) = tryFindGraphVertex x fqdn
        member x.TryFindGraphVertex<'V when 'V :> IVertex>(Fqdn(fqdn)) = tryFindGraphVertexT<'V> x fqdn
        member x.TryFindExportApiItem(Fqdn(apiPath)) = tryFindExportApiItem x apiPath
        member x.TryFindCall(callPath:Fqdn) = tryFindCall x callPath
        member x.TryFindFlow(flowName:string) = tryFindFlow x flowName
        member x.TryFindJob (jobName:string) =  tryFindJob  x jobName
        member x.TryFindReal (path:string list) =  tryFindReal x path
        member x.TryFindLoadedSystem     (system:DsSystem)  name = tryFindLoadedSystem system name
        member x.TryFindReferenceSystem  (system:DsSystem)  name = tryFindReferenceSystem system name

[<Extension>]
type FindExtension =
    // 전체 사용된 시스템을 이름으로 찾기
    [<Extension>] static member TryFindLoadedSystem (system:DsSystem, name) = tryFindLoadedSystem system name
    [<Extension>] static member TryFindExternalSystem (system:DsSystem, name) = tryFindExternalSystem system name
    // 전체 사용된 시스템에서의 찾는 이름 대상 DsSystem
    [<Extension>] static member TryFindReferenceSystem (system:DsSystem, name) = tryFindReferenceSystem system name

    [<Extension>] static member TryFindExportApiItem(x:DsSystem, Fqdn(apiPath)) = tryFindExportApiItem x apiPath
    [<Extension>] static member TryFindGraphVertex  (x:DsSystem, Fqdn(fqdn)) = tryFindGraphVertex x fqdn
    [<Extension>] static member TryFindGraphVertex<'V when 'V :> IVertex>(x:DsSystem, Fqdn(fqdn)) = tryFindGraphVertexT<'V> x fqdn
    [<Extension>] static member TryFindRealVertex (x:DsSystem, flowName, realName) =  tryFindReal x [ flowName; realName ]
    [<Extension>] static member GetSharedReal (x:Real) = getVertexSharedReal x
    [<Extension>] static member GetSharedCall (x:Call) = getVertexSharedCall x
    
    [<Extension>] static member GetPureReal  (v:Vertex) = v |> getPureReal
    [<Extension>] static member GetPureCall  (v:Vertex) = v |> getPureCall
    [<Extension>] static member GetPure (x:Vertex) = getPure x
      
    [<Extension>] static member GetAliasTypeReals(xs:Vertex seq)   = ofAliasForRealVertex xs
    [<Extension>] static member GetAliasTypeRealExs(xs:Vertex seq) = ofAliasForRealExVertex xs
    [<Extension>] static member GetAliasTypeCalls(xs:Vertex seq)   = ofAliasForCallVertex xs


    [<Extension>] static member GetVertices(edges:IEdge<'V> seq) = edges.Collect(fun e -> e.GetVertices())
    [<Extension>] static member GetVertices(x:DsSystem) =  getVerticesOfSystem x
    [<Extension>] static member GetVerticesOfFlow(x:Flow) =  getVerticesOfFlow x
    [<Extension>] static member GetVerticesOfCoins(x:DsSystem) = 
                    let vs = x.GetVertices()
                    let calls = vs.OfType<Call>().Cast<Vertex>()
                    let aliases = vs.OfType<Alias>().Cast<Vertex>()
                    (calls@aliases)
                        .Where(fun c->c.Parent.GetCore() :? Real)     

    [<Extension>] static member GetVerticesOfCoinCalls(x:DsSystem) = 
                    x.GetVertices().OfType<Call>().Where(fun c->c.Parent.GetCore() :? Real)    
    [<Extension>] static member GetDevicesOfFlow(x:Flow) =  getDevicesOfFlow x
    [<Extension>] static member GetDistinctApis(x:DsSystem) =  getDistinctApis x

    [<Extension>] static member GetSharedReal(v:Vertex) = v |> getSharedReal
    [<Extension>] static member GetSharedCall(v:Vertex) = v |> getSharedCall

