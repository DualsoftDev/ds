namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Text.RegularExpressions

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        
        fwdCreateBoolTag <-
            let createBoolTag name value =
                PlcTag<bool>(name, "FILL_ME_ADDRESS!!!!!!!!!!!!!!!!!!", value) :> TagBase<bool>
            createBoolTag


        fwdCreateUShortTag <-
            let createUShortTag name value =
                PlcTag<uint16>(name, "FILL_ME_ADDRESS!!!!!!!!!!!!!!!!!!", value) :> TagBase<uint16>
            createUShortTag


        //let createTagManager (x:IQualifiedNamed) : ITagManager =
        //    match x with
        //    | :? Vertex as v->
        //        match v with
        //        | :? Real  -> new VertexMReal(v)
        //        | (:? RealExS | :? RealExF | :? Call | :? Alias) -> new VertexMCoin(v)
        //        | _ -> failwithlog "ERROR createTagManager"

        //          //LoadedSystem은 추후 부모인 ContainerSystem Storages로 다시생성
        //    | :? DsSystem as s   -> SystemManager(s, Storages())   
        //    | :? LoadedSystem as s-> 
        //        let cStg = s.ReferenceSystem.TagManager.Storages
        //        let pStg = s.ContainerSystem.TagManager.Storages
        //        //cStg
        //        //|> Seq.filter(fun t-> not <| t.Key.StartsWith("_"))  //시스템 TAG제외
        //        //|> Seq.iter(fun t-> 
        //        //    if pStg.ContainsKey t.Key
        //        //    then failwith "Err"

        //        //    //let uniqueName = getUniqueName t.Key pStg
        //        //    ////t.Value.Name <- uniqueName
        //        //    //pStg.Add(uniqueName, t.Value)
        //        //    )  //test ahn 하위 TAG 교체 필요

        //        SystemManager(s.ReferenceSystem, pStg)
                
        //    | :? ApiItem as a -> ApiItemManager(a)
        //    | :? Flow    as f -> FlowManager(f)
        //    | _ -> failwithlog $"{x.Name} is not TagManager target"

        //fwdCreateTagManager <- createTagManager
