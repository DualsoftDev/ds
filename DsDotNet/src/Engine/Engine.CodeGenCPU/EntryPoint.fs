namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        let createVertexManager (vertex:IVertex) : IVertexManager =
            let v = vertex :?> Vertex
            match v with
            | :? Real  -> new VertexMReal(v)
            | (:? RealEx | :? Call | :? Alias) -> new VertexMCoin(v)
            | _ -> failwithlog "ERROR"

        fwdCreateVertexManager <- createVertexManager


        fwdCreateBoolTag <-
            let createBoolTag name value =
                PlcTag<bool>(name, "FILL_ME_ADDRESS!!!!!!!!!!!!!!!!!!", value) :> TagBase<bool>
            createBoolTag


        fwdCreateUShortTag <-
            let createUShortTag name value =
                PlcTag<uint16>(name, "FILL_ME_ADDRESS!!!!!!!!!!!!!!!!!!", value) :> TagBase<uint16>
            createUShortTag
