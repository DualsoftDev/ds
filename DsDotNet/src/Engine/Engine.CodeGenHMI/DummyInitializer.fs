namespace Engine.CodeGenHMI

open Engine.Core

module ModuleInitializer =
    type VMM (v:IVertex) =
        interface IVertexMemoryManager with
            member x.Vertex = v
        
    let Initialize() =
        let createVertexMemoryManager (vertex:IVertex) : IVertexMemoryManager =
            new VMM(vertex)

        fwdCreateVertexMemoryManager <- createVertexMemoryManager
