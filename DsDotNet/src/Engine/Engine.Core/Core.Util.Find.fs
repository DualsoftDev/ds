namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ModelFindModule =
    let findGraphVertex(system:DsSystem, fqdn:Fqdn) : obj =
        let xxx = system.Spit()
        let fqdn = fqdn.ToFSharpList()
        let nameEq (name:string) (x:INamed) = x.Name = name
        match fqdn with
        | [] -> failwith "ERROR: name not given"
        | s::xs when s = system.Name ->
            match xs with
            | [] -> system
            | f::xs when system.Flows.Any(nameEq f) ->
                let flow = system.Flows.First(nameEq f)
                match xs with
                | [] -> flow
                | r::xs ->
                    let real = flow.Graph.FindVertex(r) |> box :?> Real
                    match xs with
                    | [] -> real
                    | remaining -> real.Graph.FindVertex(remaining.Combine())

            | subsys::[] when system.Systems.Any(nameEq subsys) -> system.Systems.First(nameEq subsys)
            | _ -> failwith "ERROR"
        | _ -> failwith "ERROR"




        //let n = fqdn.Length
        //match n with
        //| 0 -> failwith "ERROR: name not given"
        //| _ when n > 1 ->
        //    assert(system.Name = fqdn[0])
        //    match n with
        //    | 1 -> system       //system.Systems.First(fun sys -> sys.Name = fqdn[0])
        //    | 2 -> system.Systems.First(fun sys -> sys.Name = fqdn[0])
        //    | 3 -> system.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1])
        //    | 4 -> system.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1]).Graph.FindVertex(fqdn[2])
        //    | 5 ->
        //        let seg = system.Systems.First(fun sys -> sys.Name = fqdn[0]).Flows.First(fun f -> f.Name = fqdn[1]).Graph.FindVertex(fqdn[2]) :?> Real
        //        seg.Graph.FindVertex(fqdn[3])
        //    | _ -> failwith "ERROR"
        //| _ -> failwith "ERROR"


    let findGraphVertexT<'V when 'V :> IVertex>(system:DsSystem, fqdn:Fqdn) =
        let v = findGraphVertex(system, fqdn)
        if typedefof<'V>.IsAssignableFrom(v.GetType()) then
            v :?> 'V
        else
            failwith "ERROR"

    let findApiItem(model:Model, apiPath:Fqdn) =
        let sysName, apiKey = apiPath[0], apiPath[1]
        let sys = model.Systems.First(fun sys -> sys.Name = sysName)
        let x = sys.ApiItems.FindWithName(apiKey)
        x


    let findCall(system:DsSystem, callPath:Fqdn) =
        let x = findGraphVertex(system, callPath) :?> Call
        x

    let findFlow(system:DsSystem , flowName:string) =
        system.Flows.First(fun flow -> flow.Name = flowName)


[<Extension>]
type ModelFindHelper =
    [<Extension>] static member FindGraphVertex(system:DsSystem, fqdn:Fqdn) = findGraphVertex(system, fqdn)
    //[<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(model:Model, fqdn:Fqdn) = findGraphVertexT<'V>(model, fqdn)
    [<Extension>] static member FindGraphVertex<'V when 'V :> IVertex>(system:DsSystem, fqdn:Fqdn) = findGraphVertexT<'V>(system, fqdn)
    [<Extension>] static member FindApiItem(model:Model, apiPath:Fqdn) = findApiItem(model, apiPath)
    [<Extension>] static member FindSystem(model:Model, systemName:string)    = model.Systems.First(fun sys -> sys.Name = systemName)
    [<Extension>] static member TryFindSystem(model:Model, systemName:string) = model.Systems.FirstOrDefault(fun sys -> sys.Name = systemName)
    [<Extension>] static member FindCall(system:DsSystem, callPath:Fqdn) = findCall(system, callPath)
    [<Extension>] static member FindFlow(system:DsSystem, flowName:string) = findFlow(system, flowName)





