namespace Engine.Core

open System.Linq
open Engine.Common.FS

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
                    let real = flow.Graph.FindVertex(r) |> box :?> Real
                    match xs2 with
                    | [] -> Some real
                    | remaining ->
                        option {
                            let! v = real.Graph.TryFindVertex(remaining.Combine())
                            return box v
                        }

            | dev::xs when system.Devices.Any(nameEq dev) ->
                let device = system.Devices.Find(nameEq dev)
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

    let tryFindLoadedSystem(system:DsSystem) (loadedSystemName:string) =
        system.Devices.TryFind(nameEq loadedSystemName)

    let rec tryFindExportApiItem(system:DsSystem) (Fqdn(apiPath)) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        system.ApiItems4Export.TryFindWithName(apiKey)

    and tryFindCallingApiItem (system:DsSystem) targetSystemName targetApiName =
        system.ApiItems.TryFind(nameComponentsEq [targetSystemName; targetApiName])

    let tryFindVertexCall(system:DsSystem) (Fqdn(callPath)) =
        tryFindGraphVertex system callPath |> Option.map(forceCast<Call>)

    let tryFindFlow(system:DsSystem) (flowName:string) =
        system.Flows.TryFind(nameEq flowName)


    let tryFindReal system flowName realName =
        option {
            let! flow = tryFindFlow system flowName
            return! flow.Graph.TryFindVertex(realName).Map(fun x -> x:?>Real)
        }

    let tryFindCall (system:DsSystem) callName =
        system.Calls.TryFind(nameEq callName)

    let tryFindAliasTarget (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values
            .Where(fun ad -> ad.Mnemonincs.Contains(aliasMnemonic))
            .TryExactlyOne()
            .Bind(fun ad -> ad.AliasTarget)

    let tryFindAliasDefWithMnemonic (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Values.TryFind(fun ad -> ad.Mnemonincs.Contains(aliasMnemonic))


    type DsSystem with
        member x.TryFindGraphVertex(Fqdn(fqdn)) = tryFindGraphVertex x fqdn
        member x.TryFindGraphVertex<'V when 'V :> IVertex>(Fqdn(fqdn)) = tryFindGraphVertexT<'V> x fqdn

        member x.TryFindExportApiItem(Fqdn(apiPath)) = tryFindExportApiItem x apiPath
        member x.TryFindVertexCall(Fqdn(callPath)) = tryFindVertexCall x callPath

        member x.TryFindFlow(flowName:string) = tryFindFlow x flowName
        member x.TryFindCall(callName:string) = tryFindCall x callName

