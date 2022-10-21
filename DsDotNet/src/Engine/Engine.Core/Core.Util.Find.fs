namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq

[<AutoOpen>]
module internal ModelFindModule =
    let findGraphVertex(model:Model, fqdn:NameComponents) : obj =
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
    let findGraphVertexT<'V when 'V :> IVertex>(model:Model, fqdn:NameComponents) =
        let v = findGraphVertex(model, fqdn)
        if typedefof<'V>.IsAssignableFrom(v.GetType()) then
            v :?> 'V
        else
            failwith "ERROR"

    let findApiItem(model:Model, apiPath:NameComponents) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        let sys = model.Systems.First(fun sys -> sys.Name = sysName)
        let x = sys.ApiItems.FindWithName(apiKey)
        x

    let findSystem(model:Model, systemName:string) =
        model.Systems.First(fun sys -> sys.Name = systemName)

[<Extension>]
type ModelFindHelper =
    [<Extension>] static member FindGraphVertex(model:Model, fqdn:NameComponents) = findGraphVertex(model, fqdn)
    [<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(model:Model, fqdn:NameComponents) = findGraphVertexT<'V>(model, fqdn)
    [<Extension>] static member FindApiItem(model:Model, apiPath:NameComponents) = findApiItem(model, apiPath)
    [<Extension>] static member FindSystem(model:Model, systemName:string) = findSystem(model, systemName)





