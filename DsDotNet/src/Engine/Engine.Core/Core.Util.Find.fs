namespace rec Engine.Core

open System.Linq
open System.Runtime.CompilerServices

open Dual.Common.Core.FS
open Dual.Common.Base.FS
open Engine.Common

[<AutoOpen>]
module internal ModelFindModule =
    let nameComponentsEq (Fqdn(ys)) (xs:IQualifiedNamed) = xs.NameComponents = ys
    let nameEq (name:string) (x:INamed) = x.Name = name
    let getVerticesOfFlow(flow:Flow) =
        let realVertices =
            flow.Graph.Vertices.OfType<Real>()
                .SelectMany(fun r -> r.Graph.Vertices.Cast<Vertex>())

        let flowVertices =  flow.Graph.Vertices.Cast<Vertex>()
        realVertices @ flowVertices



    // [NOTE] GraphVertex
    let tryFindSystemInner (system:DsSystem) (xs:string list) : IVertex option =
        let getOuter() =
            match xs with
            | dev::xs when system.LoadedSystems.Any(nameEq dev) ->
                let device = system.LoadedSystems.Find(nameEq dev)
                assert(device.ReferenceSystem <> system)
                match xs with
                | [] -> Some (device :> IVertex)
                | _ -> device.ReferenceSystem.TryFindFqdnVertex(xs.JoinWith(".")).Cast<IVertex>()
            | _ -> None


        let fqdn = (system.Name :: xs).JoinWith(".")
        let inner = system.TryFindFqdnVertex(fqdn).Cast<IVertex>()
        inner.OrElseWith( fun () -> getOuter())



    let tryFindGraphVertex(system:DsSystem) (Fqdn(fqdn)) : IVertex option =
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
    let tryFindJob (system:DsSystem) name             = system.Jobs.TryFind(fun j->j.DequotedQualifiedName = name)
    let tryFindFunc (system:DsSystem) name            = system.Functions.TryFind(nameEq name)
    let tryFindExternalSystem (system:DsSystem) name  = system.ExternalSystems.TryFind(nameEq name)
    let tryFindLoadedSystem (system:DsSystem) name    = system.LoadedSystems.TryFind(fun s->s.LoadedName = name)
    let tryFindReferenceSystem (system:DsSystem) name = system.LoadedSystems.Select(fun s->s.ReferenceSystem).TryFind(nameEq name)

    let rec tryFindExportApiItem(system:DsSystem) (Fqdn(apiPath)) =
        let _sysName, apiKey = apiPath[0], apiPath[1]
        system.ApiItems.TryFindWithName(apiKey)

    and tryFindCallingApiItem (system: DsSystem) (targetSystemName: string) (targetApiName: string) (allowAutoGenDevice: bool) =
        let findedLoadedSystem = tryFindLoadedSystem system (targetSystemName)

        match findedLoadedSystem with
        | Some loadedSystem ->
            let targetSystem = loadedSystem.ReferenceSystem
            system.ApiUsages.TryFind(nameComponentsEq [targetSystem.Name; targetApiName])
        | None ->
            if allowAutoGenDevice then
                None
            else
                failwithf $"해당 디바이스를 Loading 해야 합니다.\n [device file= path] {targetSystemName}"

    //jobs 에 등록 안되있으면 Real로 처리 한다.
    let tryFindCall (system:DsSystem) (Fqdn(callPath)) : Vertex option=
        //let job = tryFindJob system (callPath.Last())
        //let func = tryFindFunc system (callPath.Last())
        //if job.IsSome  || func.IsSome
        //then
        if callPath.Length = 1 then
            None
        else
            match tryFindGraphVertex system callPath with
            | Some(:? Call as c) -> Some(c)
            | _ -> None
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
            | Some(:? Real as r) -> Some r
            | _ -> None
        | s::xs2 when system.Name = s ->
            let real = tryFindSystemInner system xs2
            match real with
            | Some(:? Real as r) -> Some r
            | _ -> None
        | _ -> None

    let tryFindAliasTarget (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values
            .Where(fun ad -> ad.AliasTexts.Contains(aliasMnemonic))
            .TryExactlyOne()
            .Bind(fun ad -> ad.AliasTarget)

    let tryFindAliasDefWithMnemonic (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values.TryFind(fun ad -> ad.AliasTexts.Contains(aliasMnemonic))

    let ofCallForOperator (xs:Vertex seq) =
        xs.OfType<Call>().Where(fun f -> f.Parent.GetCore() :? Flow).Cast<Vertex>()

    let ofAliasForRealVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.RealTarget().IsSome)

    let ofAliasForCallVertex (xs:Vertex seq) =
        xs.OfType<Alias>()
        |> Seq.filter(fun a -> a.TargetWrapper.CallTarget().IsSome)


    let getVerticesOfSystem(system:DsSystem) =
        [|
            for f in system.Flows do
            for v in f.Graph.Vertices do
                yield v
                match v with
                | :? Real as r -> yield! r.Graph.Vertices.Cast<Vertex>()
                | _ -> ()
        |]

    let getDevicesOfFlow(flow:Flow) =
        let devNames =
            getVerticesOfFlow(flow)
                .OfType<Call>()
                .SelectMany(fun c->c.TaskDefs.Select(fun d->d.DeviceName))

        flow.System.Devices.Where(fun d -> devNames.Contains d.Name)

    let getVerticesOfJobCalls x =
        getVerticesOfSystem(x)
            .OfType<Call>()
            .Where(fun c->c.IsJob)

    let getDistinctApis(x:DsSystem) =
        getVerticesOfJobCalls(x)
            .SelectMany(fun c-> c.ApiDefs)
            .Distinct()

    let getVertexSharedReal(real:Real) =
        let vs = real.Parent.GetSystem() |> getVerticesOfSystem
        vs
            .OfType<Alias>()
            .Where(fun a->a.TargetWrapper.RealTarget().IsSome && a.TargetWrapper.RealTarget().Value = real)

    let getVertexSharedCall(call:Call) =
        let sharedAlias =
            (call.Parent.GetFlow() |> getVerticesOfFlow |> ofAliasForCallVertex)
              .Where(fun a -> a.TargetWrapper.CallTarget().Value = call)

        sharedAlias

    ///Real 자신을 공용으로 사용하는 Vertex들
    let getSharedReal(v:Vertex) : Alias seq =
            (v :?> Real) |> getVertexSharedReal

    ///Call 자신을 공용으로 사용하는 Vertex들
    let getSharedCall(v:Vertex) : Alias seq =
            (v :?> Call) |> getVertexSharedCall

    let getVerticesHasJob(x:DsSystem)=
            getVerticesOfSystem(x)
                .Choose(fun v -> v|> tryGetPureCall)
                .Where(fun v -> v.IsJob)


    let getVerticesOfAliasNCalls x =
        let vs = getVerticesOfSystem(x)
        let calls = vs.OfType<Call>().Cast<Vertex>()
        let aliases = vs.OfType<Alias>().Cast<Vertex>()
        (calls @ aliases)

    let getVerticesOfCoins x =
        getVerticesOfAliasNCalls(x)
            .Where(fun c->c.Parent.GetCore() :? Real)


    let getSkipInfo(dev:TaskDev, job:Job) =
        let devIndex =
            let lastPart = dev.DeviceName.Split("_").Last()
            match System.Int32.TryParse(lastPart) with
            | (true, value) -> value
            | (false, _) -> 1

        let inSkip = job.AddressInCount < devIndex
        let outSkip = job.AddressOutCount < devIndex

        inSkip, outSkip

    let updateDeviceSkipAddress (x: DsSystem) =
        let calls = x|>getVerticesHasJob
        calls
            |> collect(fun c-> c.TaskDefs.Select(fun dev-> dev, c.TargetJob))
            |> iter(fun (dev, job) ->
                let inSkip, outSkip = getSkipInfo(dev, job)
                if inSkip then
                    dev.InAddress <- TextNotUsed
                if outSkip then
                    dev.OutAddress <- TextNotUsed )

    let getTaskDevs(x: DsSystem, onlyCoin:bool) =
        let calls = getVerticesHasJob(x)
        let tds = calls
                    .Where(fun c-> not(onlyCoin) || not(c.IsFlowCall))
                    .DistinctBy(fun v-> v.TargetJob)
                    .SelectMany(fun c-> c.TaskDefs.Select(fun dev-> dev, c))
        tds
        |> Seq.sortBy (fun (td, _c) ->
            (td.DeviceName, td.ApiItem.Name))


    let getTaskDevCalls(x:DsSystem) =
        let taskDevs = getTaskDevs(x, false)
                        .DistinctBy(fun (td, _c) -> td)

        let callAll = getVerticesOfAliasNCalls(x)
        taskDevs
        |> Seq.map(fun (td, _call) ->
            td,
                callAll.Filter(fun v->
                    match tryGetPureCall(v) with
                    | Some v when v.IsJob -> v.TaskDefs.Contains td
                    | _->false)
            )

    type DsSystem with
        member x.TryFindGraphVertex<'V when 'V :> IVertex>(Fqdn(fqdn)) = tryFindGraphVertexT<'V> x fqdn
        member x.TryFindGraphVertex(Fqdn(fqdn))      = tryFindGraphVertex x fqdn
        member x.TryFindExportApiItem(Fqdn(apiPath)) = tryFindExportApiItem x apiPath
        member x.TryFindCall(callPath:Fqdn)          = tryFindCall x callPath
        member x.TryFindFlow(flowName:string)        = tryFindFlow x flowName
        member x.TryFindJob (jobName:string)         = tryFindJob  x jobName
        member x.TryFindReal (path:string list)      = tryFindReal x path

type FindExtension =
    // 전체 사용된 시스템을 이름으로 찾기
    [<Extension>] static member TryFindLoadedSystem (system:DsSystem, name) = tryFindLoadedSystem system name
    [<Extension>] static member TryFindExternalSystem (system:DsSystem, name) = tryFindExternalSystem system name
    // 전체 사용된 시스템에서의 찾는 이름 대상 DsSystem
    [<Extension>] static member TryFindReferenceSystem (system:DsSystem, name) = tryFindReferenceSystem system name

    [<Extension>] static member TryFindExportApiItem(x:DsSystem, Fqdn(apiPath)) = tryFindExportApiItem x apiPath
    [<Extension>] static member TryFindGraphVertex  (x:DsSystem, Fqdn(fqdn)) = tryFindGraphVertex x fqdn
    [<Extension>] static member FindGraphVertex (x:DsSystem, Fqdn(fqdn)):IVertex = match tryFindGraphVertex x fqdn with | Some o -> o | None -> null

    [<Extension>] static member TryFindGraphVertex<'V when 'V :> IVertex>(x:DsSystem, Fqdn(fqdn)) = tryFindGraphVertexT<'V> x fqdn
    [<Extension>] static member TryFindRealVertex (x:DsSystem, flowName, realName) =  tryFindReal x [ flowName; realName ]
    [<Extension>] static member GetSharedReal (x:Real) = getVertexSharedReal x
    [<Extension>] static member GetSharedCall (x:Call) = getVertexSharedCall x
    [<Extension>] static member GetSharedReal(v:Vertex) = v |> getSharedReal
    [<Extension>] static member GetSharedCall(v:Vertex) = v |> getSharedCall

    [<Extension>] static member GetPureReal  (v:Vertex) = v |> tryGetPureReal |> Option.get
    [<Extension>] static member GetPureCall  (v:Vertex) = v |> tryGetPureCall |> Option.get
    [<Extension>] static member TryGetPureReal  (v:Vertex) = v |> tryGetPureReal
    [<Extension>] static member TryGetPureCall  (v:Vertex) = v |> tryGetPureCall
    [<Extension>] static member GetPure (x:Vertex) = getPure x

    [<Extension>] static member GetPureReals (xs:Vertex seq) =
                    let reals = xs.OfType<Real>()
                    let aliasTargetReals = xs.OfType<Alias>().Choose(fun s->s.TryGetPureReal())
                    reals@aliasTargetReals

    [<Extension>] static member GetPureCalls (xs:Vertex seq) =
                    let calls = xs.OfType<Call>()
                    let aliasTargetCalls = xs.OfType<Alias>().Choose(fun s->s.TryGetPureCall())
                    calls@aliasTargetCalls

    [<Extension>] static member GetAliasTypeReals(xs:Vertex seq)   = ofAliasForRealVertex xs
    [<Extension>] static member GetAliasTypeCalls(xs:Vertex seq)   = ofAliasForCallVertex xs
    [<Extension>] static member GetFlowEdges(x:DsSystem) = x.Flows.Collect(fun f-> f.Graph.Edges)


    [<Extension>] static member GetVertices(edges:IEdge<'V> seq) = edges.Collect(fun e -> [e.Source; e.Target])
    [<Extension>] static member GetVertices(x:DsSystem) =  getVerticesOfSystem x
    [<Extension>] static member GetRealVertices(x:DsSystem) =  (getVerticesOfSystem x).OfType<Real>()
    [<Extension>] static member GetCallVertices(x:DsSystem) =  (getVerticesOfSystem x).OfType<Call>()

    [<Extension>] static member GetVerticesCallOperator(xs:Vertex seq)   = ofCallForOperator xs
    [<Extension>] static member GetVerticesCallOperator(x:DsSystem) =
                    getVerticesOfSystem(x) |> ofCallForOperator

    [<Extension>] static member GetVerticesOfRealOrderByName(x:DsSystem) =
                    x.GetRealVertices().OrderBy(fun r-> $"{r.Flow.Name}_{r.Name}" )
    [<Extension>] static member GetFlowsOrderByName(x:DsSystem) =
                    x.Flows.OrderBy(fun f-> f.Name )

    [<Extension>] static member GetVerticesOfFlow(x:Flow) =  getVerticesOfFlow x
    [<Extension>] static member GetVerticesOfCoins(x:DsSystem) = getVerticesOfCoins x


    [<Extension>] static member GetVerticesHasJob(x:DsSystem) =   getVerticesHasJob x

    [<Extension>]
        static member GetVerticesHasJobOfFlow(x:Flow) =
            getVerticesOfFlow(x)
                .Choose(fun v -> v.TryGetPureCall())
                .Where(fun v -> v.IsJob)

    [<Extension>]
        static member GetVerticesHasJobInReal(x:DsSystem) =
            x.GetVerticesOfCoins()
                .Choose(fun v -> v.TryGetPureCall())
                .Where(fun v -> v.IsJob)


    [<Extension>] static member GetTaskDevCalls(x:DsSystem) =   getTaskDevCalls x


    [<Extension>] static member GetDevicesOfFlow(x:Flow) =  getDevicesOfFlow x
    [<Extension>] static member GetDistinctApis(x:DsSystem) =  getDistinctApis x

    [<Extension>] static member GetSkipInfo(dev:TaskDev, job:Job) =  getSkipInfo (dev, job)
    [<Extension>] static member GetVerticesOfJobCalls(x:DsSystem) =  getVerticesOfJobCalls x
    [<Extension>]
        static member GetAlarmCalls(x:DsSystem) =
            x.GetVerticesOfJobCalls()
                //.Where(fun w->w.TargetJob.ActionType <> JobActionType.NoneTRx)
                .Where(fun w->w.Parent.GetCore() :? Real)
                .OrderBy(fun c->c.Name)

    [<Extension>]
        static member GetVerticesOfJobCoins(xs:Vertex seq, job:Job) =
            xs.Where(fun v->
                    match v.TryGetPureCall() with
                    | Some c -> c.IsJob && c.TargetJob = job
                    | _ -> false)

    [<Extension>]
        static member GetSourceTokenCount(x:DsSystem) =
            x.GetVertices().Where(fun v->v.TokenSourceOrder.IsSome).Count()

    [<Extension>] static member GetTaskDevsCoin(x:DsSystem) = getTaskDevs(x, true)

    [<Extension>] static member GetTaskDevsCall(x:DsSystem) = getTaskDevs(x, false)

    [<Extension>]
    static member GetDistinctApisWithDeviceCall(x:DsSystem) =
        getTaskDevCalls(x)
            .Select(fun (td, calls)-> (td.ApiItem , td, calls))
            .DistinctBy(fun (api, _td, _c)-> api)

    [<Extension>]
    static member GetDevicesHasOutput(x:DsSystem) =
        //출력있는건 무조건  Coin
        x.GetTaskDevsCoin()
            .DistinctBy(fun (td, c) -> (td, c.TargetJob))
            .Where(fun (dev,_) -> dev.OutAddress <> TextNotUsed)

    [<Extension>]
        static member GetDevicesForHMI(x:DsSystem) =
            x.GetTaskDevsCoin()
                .DistinctBy(fun (td, _c) -> td)
                .Where(fun (dev, call) -> call.TargetJob.TaskDevCount > 1 || not(dev.IsOutAddressSkipOrEmpty))
                .Where(fun (dev,c) -> c.TaskDefs.First() = dev)


    [<Extension>]
        static member GetTaskDevs(x:DsSystem) =
            x.Jobs
                .SelectMany(fun j->j.TaskDefs.Select(fun td->(td, j))).DistinctBy(fun (td, _j) -> td)
                .OrderBy(fun (td, _c) ->
                        (td.DeviceName, td.ApiItem.Name))

                        
    [<Extension>]
        static member GetTaskDevsWithoutSkipAddress(x:DsSystem) =
            x.GetTaskDevs()
             .Where(fun (td, _j) -> not(td.OutAddress = TextNotUsed && td.InAddress= TextNotUsed))

    [<Extension>]
    static member GetQualifiedName(vertex:IVertex) =
        match vertex with
        | :? IQualifiedNamed as q -> q.QualifiedName
        | _ -> failwithlog "ERROR"

    [<Extension>]
    static member GetParentName(vertex:IVertex) =
        match vertex with
        | :? DsSystem as s -> s.Name
        | :? Flow as f -> f.System.QualifiedName
        | :? Real as r -> r.Flow.QualifiedName
        | :? Call as c -> c.Parent.GetCore().QualifiedName
        | :? Alias as a -> a.Parent.GetCore().QualifiedName
        | _ -> failwithlog "ERROR"
