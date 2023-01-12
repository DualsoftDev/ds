namespace Engine.Core

open System.Linq
open Engine.Common.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module internal ModelFindModule =
    let nameComponentsEq (Fqdn(ys)) (xs:IQualifiedNamed) = xs.NameComponents = ys
    let nameEq (name:string) (x:INamed) = x.Name = name

    let tryFindGraphVertex(system:DsSystem) (Fqdn(fqdn)) : obj option =
        //let inline nameComponentsEq xs ys = (^T: (member NameComponents: Fqdn) xs) = (^T: (member NameComponents: Fqdn) ys)

        let tryFindInLoadedSystem (device:LoadedSystem) (Fqdn(fqdn)) =
            failwith "Not yet implemented"
            None

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
                | _ -> tryFindInLoadedSystem device (xs.ToArray())
            | _ -> failwith "ERROR"


        let fqdn = fqdn.ToFSharpList()
        match fqdn with
        | [] -> failwith "ERROR: name not given"
        | s::xs when s = system.Name -> tryFindSystemInner system xs
        | _ -> tryFindSystemInner system fqdn


    let tryFindGraphVertexT<'V when 'V :> IVertex>(system:DsSystem) (Fqdn(fqdn)) =
        option {
            let! v = tryFindGraphVertex system fqdn
            if typedefof<'V>.IsAssignableFrom(v.GetType()) then
                return v :?> 'V
            else
                failwith "ERROR"
        }
        
    let tryFindFlow(system:DsSystem) (name:string)   = system.Flows.TryFind(nameEq name)
    let tryFindJob (system:DsSystem) name            = system.Jobs.TryFind(nameEq name)

    let tryFindLoadedSystem (system:DsSystem) name   = system.LoadedSystems.TryFind(nameEq name)
    let tryFindReferenceSystem (system:DsSystem) name   =
                     system.LoadedSystems.Select(fun s->s.ReferenceSystem).TryFind(nameEq name)
    
   
    let rec tryFindExportApiItem(system:DsSystem) (Fqdn(apiPath)) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        system.ApiItems.TryFindWithName(apiKey)

    and tryFindCallingApiItem (system:DsSystem) targetSystemName targetApiName =
        let findedLoadedSystem = tryFindLoadedSystem system targetSystemName 
        let targetSystem = findedLoadedSystem.Value.ReferenceSystem
        system.ApiUsages.TryFind(nameComponentsEq [targetSystem.Name; targetApiName])

    
    //jobs 에 등록 안되있으면 Real로 처리 한다.
    let tryFindCall (system:DsSystem) (Fqdn(callPath))=
        if tryFindJob system (callPath.Last()) |> Option.isSome
        then match tryFindGraphVertex system callPath with
             |Some(v) -> Some(v :?> Call)
             |None -> None
        else None

    let tryFindReal system flowName name =
        let flow = tryFindFlow system flowName |> Option.get
        match flow.Graph.TryFindVertex(name) with
        |Some(v) -> if v:? Real then Some(v :?> Real) else None
        |None -> None
     
    let tryFindAliasTarget (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values
            .Where(fun ad -> ad.Mnemonincs.Contains(aliasMnemonic))
            .TryExactlyOne()
            .Bind(fun ad -> ad.AliasTarget)

    let tryFindAliasDefWithMnemonic (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values.TryFind(fun ad -> ad.Mnemonincs.Contains(aliasMnemonic))
    
    let getVertexSharedReal(real:Real) = 
        let sheredAlias = 
            real.Flow.Graph.Vertices
                .GetAliasTypeReals()
                .Where(fun a -> a.TargetWrapper.RealTarget().Value = real)
                .Cast<Vertex>()

        let sheredRealEx = 
            real.Flow.System.GetVertices()
                .OfType<RealEx>()
                .Where(fun w-> w.Real = real)
                .Cast<Vertex>()

        sheredAlias @ sheredRealEx

    let getVertexSharedCall(call:Call) = 
        let sheredAlias = 
            call.Parent.GetFlow().GetVerticesWithInReal()
              .GetAliasTypeCalls()
              .Where(fun a -> a.TargetWrapper.CallTarget().Value = call)
              .Cast<Vertex>()
       
        sheredAlias 

    type DsSystem with
        member x.TryFindGraphVertex(Fqdn(fqdn)) = tryFindGraphVertex x fqdn
        member x.TryFindGraphVertex<'V when 'V :> IVertex>(Fqdn(fqdn)) = tryFindGraphVertexT<'V> x fqdn

        member x.TryFindExportApiItem(Fqdn(apiPath)) = tryFindExportApiItem x apiPath
        member x.TryFindCall(callPath:Fqdn) = tryFindCall x callPath
        member x.TryFindFlow(flowName:string) = tryFindFlow x flowName
        member x.TryFindJob (jobName:string) =  tryFindJob  x jobName
        member x.TryFindReal(system) flowName realName =  tryFindReal  system flowName realName 
        member x.TryFindLoadedSystem     (system:DsSystem)  name = tryFindLoadedSystem system name  
        member x.TryFindReferenceSystem  (system:DsSystem)  name = tryFindReferenceSystem system name  
      
[<Extension>]
type FindExtension =  
    // 전체 사용된 시스템을 이름으로 찾기
    [<Extension>] static member TryFindLoadedSystem (system:DsSystem, name) = tryFindLoadedSystem system name  
    // 전체 사용된 시스템에서의 찾는 이름 대상 DsSystem  
    [<Extension>] static member TryFindReferenceSystem (system:DsSystem, name) = tryFindReferenceSystem system name  

    [<Extension>] static member TryFindExportApiItem(x:DsSystem, Fqdn(apiPath)) = tryFindExportApiItem x apiPath
    [<Extension>] static member TryFindGraphVertex  (x:DsSystem, Fqdn(fqdn)) = tryFindGraphVertex x fqdn
    [<Extension>] static member TryFindGraphVertex<'V when 'V :> IVertex>(x:DsSystem, Fqdn(fqdn)) = tryFindGraphVertexT<'V> x fqdn
    
    [<Extension>] static member GetVertexSharedReal (x:Real) = getVertexSharedReal x
    [<Extension>] static member GetVertexSharedCall (x:Call) = getVertexSharedCall x

