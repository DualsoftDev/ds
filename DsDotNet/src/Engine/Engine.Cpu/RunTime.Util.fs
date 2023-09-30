namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Reactive.Linq
open System.Collections.Generic

[<AutoOpen>]
module RunTimeUtil =


    
    let notifyPreExcute ( x:IStorage) = 
            match  x.GetTagInfo() with
            |Some t -> onTagDSChanged t
            |_ -> ()
        
    let  notifyPostExcute ( x:IStorage) =
            match x.GetTagInfo() with
            |Some t -> 
                match t with
                |EventVertex (tag,_,kind) ->
                        match kind with
                        | VertexTag.startForce 
                        | VertexTag.resetForce -> tag.BoxedValue <- false 
                        | _-> ()
                |_ -> ()
            |None -> ()


    let getTotalTags(statements:Statement seq) =
        [ for s in statements do
            yield! s.GetSourceStorages()
            yield! s.GetTargetStorages()
        ].Distinct()

    let getRungMap(statements:Statement seq) =

        let total = getTotalTags  statements
        let dicSource =
            statements
                .Select(fun s -> s, s.GetSourceStorages())
                |> dict |> Dictionary

        let map =
            total.Select(fun tag ->
                let sts = dicSource.Filter(fun f->f.Value.Contains(tag))
                tag, sts.Select(fun st -> st.Key)
            )
        map |> dict


    ///시뮬레이션 이전에 사용자 HMI 대신 눌러주기
    let preAction(sys:DsSystem, mode:RuntimePackage ) =
        let simTags =
            sys.TagManager.Storages
                .Where(fun w->
                            w.Value.TagKind = (int)SystemTag.auto
                            ||   w.Value.TagKind = (int)SystemTag.drive
                            ||   w.Value.TagKind = (int)SystemTag.ready
                            || ( w.Value.TagKind = (int)SystemTag.sim && mode = RuntimePackage.Simulation)
                    )
        simTags.Iter(fun t -> t.Value.BoxedValue <-  true)



    let singleScan (statements:Statement seq, systems:DsSystem seq) =
        for s in statements do s.Do()
        let total = getTotalTags  statements
        let chTags = total.ChangedTags()

        chTags.Iter(notifyPreExcute) 
        chTags.ChangedTagsClear(systems)
        chTags.Iter(notifyPostExcute) 
        
    ///HMI Reset
    let syncReset(statements:Statement seq, systems:DsSystem seq, activeSys:bool) =
        let stgs = systems.First().TagManager.Storages
        let systemOn =  stgs.First(fun w-> w.Value.TagKind = (int)SystemTag.on).Value
        let stgs =  stgs.Where(fun w-> w.Value <> systemOn)

        if activeSys
        then
            for tag in stgs do
                let stg = tag.Value
                match stg with
                | :? TimerCounterBaseStruct as tc ->
                    tc.ResetStruct()  // 타이머 카운터 리셋
                | _ ->
                    stg.BoxedValue <- textToDataType(stg.DataType.Name).DefaultValue()

        //조건 1번 평가 (for : Ready State 이벤트)
        singleScan (statements, systems)
