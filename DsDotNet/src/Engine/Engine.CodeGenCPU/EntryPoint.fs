namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        let createTagManager (x:IQualifiedNamed) : ITagManager =
            match x with
            | :? Vertex as v->
                match v with
                | :? Real  -> new VertexMReal(v)
                | (:? RealEx | :? Call | :? Alias) -> new VertexMCoin(v)
                | _ -> failwithlog "ERROR createTagManager"

                  //LoadedSystem은 추후 부모 Storages로 다시생성
            | :? DsSystem as s   -> SystemManager(s, Storages())   
            | :? LoadedSystem as s-> 
                let child = s.ReferenceSystem
                let parent = s.ContainerSystem
                SystemManager(child, parent.TagManager.Storages)
                
            | :? ApiItem     as a-> ApiItemManager(a)
            | :? Flow     as f-> FlowManager(f)
            | _ -> failwithlog $"{x.Name} is not TagManager target"

        fwdCreateTagManager <- createTagManager

        fwdCreateBoolTag <-
            let createBoolTag name value =
                PlcTag<bool>(name, "FILL_ME_ADDRESS!!!!!!!!!!!!!!!!!!", value) :> TagBase<bool>
            createBoolTag


        fwdCreateUShortTag <-
            let createUShortTag name value =
                PlcTag<uint16>(name, "FILL_ME_ADDRESS!!!!!!!!!!!!!!!!!!", value) :> TagBase<uint16>
            createUShortTag
