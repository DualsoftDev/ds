namespace Engine.Core

open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ModelFindModule =
    let nameComponentsEq (Fqdn(ys)) (xs:IQualifiedNamed) = xs.NameComponents = ys
    let nameEq (name:string) (x:INamed) = x.Name = name

    let findGraphVertex(system:DsSystem) (Fqdn(fqdn)) : obj =
        //let inline nameComponentsEq xs ys = (^T: (member NameComponents: Fqdn) xs) = (^T: (member NameComponents: Fqdn) ys)

        let findInLoadedSystem (device:LoadedSystem) (Fqdn(fqdn)) =
            failwith "Not yet implemented"
            null

        let rec findSystemInner (system:DsSystem) (xs:string list) : obj =
            match xs with
            | [] -> system
            | f::xs1 when system.Flows.Any(nameEq f) ->
                let flow = system.Flows.First(nameEq f)
                match xs1 with
                | [] -> flow
                | r::xs2 ->
                    let real = flow.Graph.FindVertex(r) |> box :?> Real
                    match xs2 with
                    | [] -> real
                    | remaining -> real.Graph.FindVertex(remaining.Combine())

            | dev::xs when system.Devices.Any(nameEq dev) ->
                let device = system.Devices.Find(nameEq dev)
                match xs with
                | [] -> device
                | _ -> findInLoadedSystem device (xs.ToArray())
            | _ -> failwith "ERROR"


        let fqdn = fqdn.ToFSharpList()
        match fqdn with
        | [] -> failwith "ERROR: name not given"
        | s::xs when s = system.Name -> findSystemInner system xs
        | _ -> findSystemInner system fqdn


    let findGraphVertexT<'V when 'V :> IVertex>(system:DsSystem) (Fqdn(fqdn)) =
        let v = findGraphVertex system fqdn
        if typedefof<'V>.IsAssignableFrom(v.GetType()) then
            v :?> 'V
        else
            failwith "ERROR"

    let tryFindLoadedSystem(system:DsSystem) (loadedSystemName:string) =
        system.Devices.TryFind(fun d -> d.Name = loadedSystemName)

    let rec findExportApiItem(system:DsSystem) (Fqdn(apiPath)) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        system.ApiItems4Export.FindWithName(apiKey)

    //and tryFindImportApiItem(system:DsSystem) (Fqdn(apiPath)) =
    //    let lSysName, lApiKey = apiPath[0], apiPath[1]
    //    let loadedSystem = tryFindLoadedSystem system lSysName
    //    match loadedSystem with
    //    | Some lsystem ->
    //        lsystem.ReferenceSystem.ApiItems4Export
    //            .TryFind(fun api -> api.Name = lApiKey)
    //    | None -> None

    and tryFindCallingApiItem (system:DsSystem) targetSystemName targetApiName =
        system.ApiItems.TryFind(nameComponentsEq [targetSystemName; targetApiName])

    let findVertexCall(system:DsSystem) (Fqdn(callPath)) =
        let x = findGraphVertex system callPath :?> Call
        x

    let tryFindFlow(system:DsSystem) (flowName:string) =
        system.Flows.TryFind(fun flow -> flow.Name = flowName)


    let tryFindReal system flowName realName =
        option {
            let! flow = tryFindFlow system flowName
            return! flow.Graph.TryFindVertex(realName).Map(fun x -> x:?>Real)
        }

    let tryFindCall (system:DsSystem) callName = system.Calls.TryFind(nameEq callName)
    let tryFindAliasTarget (flow:Flow) aliasMnemonic =
        flow.AliasDefs.Where(fun ad -> ad.Mnemonincs.Contains(aliasMnemonic)).Select(fun ad -> ad.AliasTarget).TryExactlyOne()
    let tryFindAliasDefWithMnemonic (flow:Flow) aliasMnemonic =
        flow.AliasDefs.TryFind(fun ad -> ad.Mnemonincs.Contains(aliasMnemonic))


    type DsSystem with
        member x.FindGraphVertex(Fqdn(fqdn)) = findGraphVertex x fqdn
        member x.FindGraphVertex<'V when 'V :> IVertex>(Fqdn(fqdn)) = findGraphVertexT<'V> x fqdn

        member x.FindExportApiItem(Fqdn(apiPath)) = findExportApiItem x apiPath
        //member x.TryFindExportApiItem(apiKey:string) = x.ApiItems4Export.FindWithName(apiKey)
        member x.FindVertexCall(Fqdn(callPath)) = findVertexCall x callPath

        member x.FindFlow(flowName:string) = tryFindFlow x flowName |> Option.get
        member x.TryFindFlow(flowName:string) = tryFindFlow x flowName

