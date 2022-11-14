namespace Engine.Core

open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ModelFindModule =
    let findGraphVertex(system:DsSystem, fqdn:Fqdn) : obj =
        let nameEq (name:string) (x:INamed) = x.Name = name

        let findInLoadedSystem (device:LoadedSystem) (fqdn:Fqdn) =
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


    let findGraphVertexT<'V when 'V :> IVertex>(system:DsSystem, fqdn:Fqdn) =
        let v = findGraphVertex(system, fqdn)
        if typedefof<'V>.IsAssignableFrom(v.GetType()) then
            v :?> 'V
        else
            failwith "ERROR"

    let findExportApiItem(system:DsSystem, apiPath:Fqdn) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        system.ApiItems.FindWithName(apiKey)

    let tryFindLoadedSystem(system:DsSystem, loadedSystemName:string) = system.Devices.TryFind(fun d -> d.Name = loadedSystemName)
    let tryFindImportApiItem(system:DsSystem, apiPath:Fqdn) =
        let lSysName, lApiKey = apiPath[0], apiPath[1]
        let loadedSystem = tryFindLoadedSystem(system, lSysName)
        match loadedSystem with
        | Some lsystem ->
            lsystem.ReferenceSystem.ApiItems
                .TryFind(fun api -> api.Name = lApiKey)
        | None -> None

    let findCall(system:DsSystem, callPath:Fqdn) =
        let x = findGraphVertex(system, callPath) :?> Call
        x

    let findFlow(system:DsSystem , flowName:string) =
        system.Flows.First(fun flow -> flow.Name = flowName)


    type DsSystem with
        member x.FindGraphVertex(fqdn:Fqdn) = findGraphVertex(x, fqdn)
        member x.FindGraphVertex<'V when 'V :> IVertex>(fqdn:Fqdn) = findGraphVertexT<'V>(x, fqdn)

        member x.FindExportApiItem(apiPath:Fqdn) = findExportApiItem(x, apiPath)
        member x.TryFindExportApiItem(apiKey:string) = x.ApiItems.FindWithName(apiKey)
        member x.FindCall(callPath:Fqdn) = findCall(x, callPath)

        member x.FindFlow(flowName:string) = findFlow(x, flowName)

