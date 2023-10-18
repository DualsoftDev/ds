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
            |Some t -> t.OnChanged()
            |_ -> ()
        
    let  notifyPostExcute ( x:IStorage) =
            match x.GetTagInfo() with
            |Some t -> 
                match t with
                |EventVertex (tag,_,kind) ->
                        match kind with
                        | VertexTag.forceOn
                        | VertexTag.forceOff 
                        | VertexTag.forceStart 
                        | VertexTag.forceReset -> tag.BoxedValue <- false 
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

        
    ///사용자 autoStartTags HMI 대신 눌러주기
    let preAction(sys:DsSystem, mode:RuntimePackage, on:bool) =
        let autoStartTags =
            sys.TagManager.Storages
                .Where(fun w->
                            w.Value.TagKind = (int)SystemTag.auto
                            ||   w.Value.TagKind = (int)SystemTag.drive
                            ||   w.Value.TagKind = (int)SystemTag.ready
                            || ( w.Value.TagKind = (int)SystemTag.sim && mode = RuntimePackage.Simulation)
                    )
        autoStartTags.Iter(fun t -> t.Value.BoxedValue <-  on)


    ///시뮬레이션 비트 토글
    let cpuModeToggle(sys:DsSystem, mode:RuntimePackage) =
        sys.TagManager.Storages
            .Where(fun w->  w.Value.TagKind = (int)SystemTag.sim)
            .Iter(fun t -> t.Value.BoxedValue <- (mode = RuntimePackage.Simulation))


    ///HMI Reset
    let syncReset((*statements:Statement seq,*) systems:DsSystem seq, activeSys:bool) =
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

