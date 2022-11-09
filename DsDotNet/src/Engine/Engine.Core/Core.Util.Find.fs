namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq

[<AutoOpen>]
module internal ModelFindModule =
    let findGraphVertex(model:Model, fqdn:Fqdn) : obj =
        let n = fqdn.Length
        match n with
        | 0 -> failwith "ERROR: name not given"
        | 1 -> model.Systems.First(fun sys -> sys.Name = fqdn[0])
        | 2 -> model.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1])
        | 3 -> model.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1]).Graph.FindVertex(fqdn[2])
        | 4 ->
            let seg = model.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1]).Graph.FindVertex(fqdn[2]) :?> Real
            seg.Graph.FindVertex(fqdn[3])
        | _ -> failwith "ERROR"
    let findGraphVertexT<'V when 'V :> IVertex>(model:Model, fqdn:Fqdn) =
        let v = findGraphVertex(model, fqdn)
        if typedefof<'V>.IsAssignableFrom(v.GetType()) then
            v :?> 'V
        else
            failwith "ERROR"

    let findApiItem(model:Model, apiPath:Fqdn) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        let sys = model.Systems.First(fun sys -> sys.Name = sysName)
        let x = sys.ApiItems.FindWithName(apiKey)
        x


    let findCall(model:Model, callPath:Fqdn) =
        let x = findGraphVertex(model, callPath) :?> Call
        x

    let findFlow(system:DsSystem , flowName:string) =
        system.Flows.First(fun flow -> flow.Name = flowName)


[<Extension>]
type ModelFindHelper =
    [<Extension>] static member FindGraphVertex(model:Model, fqdn:Fqdn) = findGraphVertex(model, fqdn)
    [<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(model:Model, fqdn:Fqdn) = findGraphVertexT<'V>(model, fqdn)
    [<Extension>] static member FindSystem(model:Model, systemName:string)    = model.Systems.First(fun sys -> sys.Name = systemName)
    [<Extension>] static member TryFindSystem(model:Model, systemName:string) = model.Systems.FirstOrDefault(fun sys -> sys.Name = systemName)
    [<Extension>] static member FindApiItem(model:Model, apiPath:Fqdn) = findApiItem(model, apiPath)
    [<Extension>] static member TryFindApiItem(system:DsSystem, apiKey:string) = system.ApiItems.FindWithName(apiKey)
    [<Extension>] static member FindCall(model:Model, callPath:Fqdn) = findCall(model, callPath)
    [<Extension>] static member FindFlow(system:DsSystem, flowName:string) = findFlow(system, flowName)





