namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Reactive.Linq
open System.Collections.Generic
open Engine.CodeGenCPU

[<AutoOpen>]
module internal RunTimeUtil =


    
    let notifyPreExcute ( x:IStorage) =
        x.GetTagInfo() |> Option.iter(fun t -> t.OnChanged())
        
    let  notifyPostExcute ( x:IStorage) =
        x.GetTagInfo() |> Option.iter(fun t -> 
                match t with
                | EventVertex (tag,_,kind) ->
                        match kind with
                        | VertexTag.forceOn
                        | VertexTag.forceOff 
                        | VertexTag.forceStart 
                        | VertexTag.forceReset -> tag.BoxedValue <- false 
                        | _-> ()
                | _ -> ())
        


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
    let preAction(sys:DsSystem, on:bool) =
        let autoStartStorageKeyValues =
            sys.TagManager.Storages
                .Where(fun w->
                            w.Value.TagKind = (int)SystemTag.auto_btn
                            ||   w.Value.TagKind = (int)SystemTag.drive_btn
                            ||   w.Value.TagKind = (int)SystemTag.ready_btn
                    )
        autoStartStorageKeyValues.Iter(fun t -> t.Value.BoxedValue <-  on)


    ///시뮬레이션 비트 ON
    let cpuSimOn(sys:DsSystem) =
        let simTag = (sys.TagManager :?> SystemManager).GetSystemTag(SystemTag.sim) 
        simTag.BoxedValue <- true
   
    ///HMI Reset
    let syncReset(system:DsSystem ) =
        let stgs = system.TagManager.Storages
        let stgs = stgs.Where(fun w-> w.Value.TagKind <> (int)SystemTag.on)

        for tag in stgs do
            let stg = tag.Value
            match stg with
            | :? TimerCounterBaseStruct as tc ->
                tc.ResetStruct()  // 타이머 카운터 리셋
            | _ ->
                stg.BoxedValue <- textToDataType(stg.DataType.Name).DefaultValue()

