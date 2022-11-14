namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ModelFindModule =
    let findGraphVertex(system:DsSystem, fqdn:Fqdn) : obj =
        let nameEq (name:string) (x:INamed) = x.Name = name

        let findInDevice (device:Device) (fqdn:Fqdn) =
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
                | _ -> findInDevice device (xs.ToArray())
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

    let findApiItem(system:DsSystem, apiPath:Fqdn) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        system.ApiItems.FindWithName(apiKey)


    let findCall(system:DsSystem, callPath:Fqdn) =
        let x = findGraphVertex(system, callPath) :?> Call
        x

    let findFlow(system:DsSystem , flowName:string) =
        system.Flows.First(fun flow -> flow.Name = flowName)


[<Extension>]
type ModelFindHelper =
    [<Extension>] static member FindGraphVertex(system:DsSystem, fqdn:Fqdn) = findGraphVertex(system, fqdn)
    [<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(system:DsSystem, fqdn:Fqdn) = findGraphVertexT<'V>(system, fqdn)

    [<Extension>] static member FindApiItem(system:DsSystem, apiPath:Fqdn) = findApiItem(system, apiPath)
    [<Extension>] static member TryFindApiItem(system:DsSystem, apiKey:string) = system.ApiItems.FindWithName(apiKey)
    [<Extension>] static member FindCall(system:DsSystem, callPath:Fqdn) = findCall(system, callPath)

    [<Extension>] static member FindFlow(system:DsSystem, flowName:string) = findFlow(system, flowName)





