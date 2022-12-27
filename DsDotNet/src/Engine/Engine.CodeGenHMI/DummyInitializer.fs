namespace Engine.CodeGenHMI

open Engine.Core

module ModuleInitializer =
    type VMM (v:IVertex) =
        interface IVertexManager with
            member x.Vertex = v
        
    let Initialize() =
        let createVertexManager (vertex:IVertex) : IVertexManager =
            new VMM(vertex)

        fwdCreateVertexManager <- createVertexManager
