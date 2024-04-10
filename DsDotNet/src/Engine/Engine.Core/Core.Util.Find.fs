namespace Engine.Core

open System.Linq
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module internal ModelFindModule =
    let nameComponentsEq (Fqdn(ys)) (xs:IQualifiedNamed) = xs.NameComponents = ys
    let nameEq (name:string) (x:INamed) = x.Name = name
    
    //let tryFindInLoadedSystem (_device:LoadedSystem) (Fqdn(_fqdn)) =
    //    failwithlog "Not yet implemented"
    //    None

    let rec tryFindSystemInner (system:DsSystem) (xs:string list) : obj option =
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
            | _ -> None//tryFindInLoadedSystem device (xs.ToArray())
        | _ -> failwithlog "ERROR"

    let tryFindGraphVertex(system:DsSystem) (Fqdn(fqdn)) : obj option =
        //let inline nameComponentsEq xs ys = (^T: (member NameComponents: Fqdn) xs) = (^T: (member NameComponents: Fqdn) ys)
        let fqdn = fqdn.ToFSharpList()
        match fqdn with
        | [] -> failwithlog "ERROR: name not given"
        //| s::xs when s = system.Name -> tryFindSystemInner system xs
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
        if tryFindJob system (callPath.Last()) |> Option.isSome
        then match tryFindGraphVertex system callPath with
             |Some(v) ->
                match v with
                | :? Call -> Some(v)
                | _ -> None
             |None -> None
        else None

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

    let getVertexSharedReal(real:Real) =
        let sharedAlias =
            real.Flow.Graph.Vertices
                .GetAliasTypeReals()
                .Where(fun a -> a.TargetWrapper.RealTarget().Value = real)
                .Cast<Vertex>()

        let sharedRealExFlow =
            real.Flow.System.GetVertices()
                .OfType<RealExF>()
                .Where(fun w-> w.Real = real)
                .Cast<Vertex>()

        sharedAlias @ sharedRealExFlow

    let getVertexSharedCall(call:Call) =
        let sharedAlias =
            call.Parent.GetFlow().GetVerticesOfFlow()
              .GetAliasTypeCalls()
              .Where(fun a -> a.TargetWrapper.CallTarget().Value = call)
              .Cast<Vertex>()

        sharedAlias


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
    [<Extension>] static member GetVertexSharedReal (x:Real) = getVertexSharedReal x
    [<Extension>] static member GetVertexSharedCall (x:Call) = getVertexSharedCall x
    [<Extension>] static member GetPure (x:Vertex) = getPure x
                                 

