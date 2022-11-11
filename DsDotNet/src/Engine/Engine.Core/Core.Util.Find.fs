namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ModelFindModule =
    let findGraphVertex(system:DsSystem, fqdn:Fqdn) : obj =
        let xxx = system.Spit()
        let nameEq (name:string) (x:INamed) = x.Name = name

        let rec findSystemInner (system:DsSystem) (xs:string list) : obj =
            match xs with
            | [] -> system
            | f::xs1 when system.Flows.Any(nameEq f) ->
                let flow = system.Flows.First(nameEq f)
                match xs1 with
                | [] -> flow
                | r::xs2 ->
                    if r = "R.X" then
                        noop()
                    let real = flow.Graph.FindVertex(r) |> box :?> Real
                    match xs2 with
                    | [] -> real
                    | remaining -> real.Graph.FindVertex(remaining.Combine())

            | subsys::xs when system.Systems.Any(nameEq subsys) ->
                let subSystem = system.Systems.Find(nameEq subsys)
                match xs with
                | [] -> subSystem
                | _ -> findSystemInner subSystem xs
            | _ -> failwith "ERROR"


        let fqdn = fqdn.ToFSharpList()
        match fqdn with
        | [] -> failwith "ERROR: name not given"
        | s::xs when s = system.Name -> findSystemInner system xs
        | _ -> findSystemInner system fqdn

    let getTheSystem(model:Model) = model.TheSystem.Value


    let findGraphVertexT<'V when 'V :> IVertex>(system:DsSystem, fqdn:Fqdn) =
        let v = findGraphVertex(system, fqdn)
        if typedefof<'V>.IsAssignableFrom(v.GetType()) then
            v :?> 'V
        else
            failwith "ERROR"

    let findApiItem(model:Model, apiPath:Fqdn) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        let sys = model.TheSystem.Value.Systems.First(fun sys -> sys.Name = sysName)
        let x = sys.ApiItems.FindWithName(apiKey)
        x


    let findCall(system:DsSystem, callPath:Fqdn) =
        let x = findGraphVertex(system, callPath) :?> Call
        x

    let findFlow(system:DsSystem , flowName:string) =
        system.Flows.First(fun flow -> flow.Name = flowName)


[<Extension>]
type ModelFindHelper =
    [<Extension>] static member FindGraphVertex(model:Model, fqdn:Fqdn) = findGraphVertex(getTheSystem(model), fqdn)
    [<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(model:Model, fqdn:Fqdn) = findGraphVertexT<'V>(getTheSystem(model), fqdn)

    [<Extension>] static member FindApiItem(model:Model, apiPath:Fqdn) = findApiItem(model, apiPath)
    [<Extension>] static member TryFindApiItem(system:DsSystem, apiKey:string) = system.ApiItems.FindWithName(apiKey)
    [<Extension>] static member FindCall(model:Model, callPath:Fqdn) = findCall(getTheSystem(model), callPath)

    [<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(system:DsSystem, fqdn:Fqdn) = findGraphVertexT<'V>(system, fqdn)
    [<Extension>] static member FindCall(system:DsSystem, callPath:Fqdn) = findCall(system, callPath)
    [<Extension>] static member FindFlow(system:DsSystem, flowName:string) = findFlow(system, flowName)





