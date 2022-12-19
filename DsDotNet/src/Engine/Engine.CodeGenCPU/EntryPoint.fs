namespace Engine.CodeGenCPU

open Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        let createVertexMemoryManager (vertex:IVertex) : IVertexMemoryManager =
            let v = vertex :?> Vertex
            match v with
            | (:? Real | :? Call) -> new VertexMemoryManager(v)
            //| :? Alias as a -> new VertexMemoryManager(a.TargetWrapper.GetTarget())
            | _ -> failwith "ERROR"

        fwdCreateVertexMemoryManager <- createVertexMemoryManager
