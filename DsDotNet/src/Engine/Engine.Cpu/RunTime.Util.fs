namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Reactive.Linq
open System.Collections.Generic

[<AutoOpen>]
module RunTimeUtil =


    let getTotalTags(statements:Statement seq) =
        [ for s in statements do
            yield! s.GetSourceStorages()
            yield! s.GetTargetStorages()
        ].Distinct()

    ///statements 전체에 대하여 target tag가
    ///다른 Statement에 조건으로 사용된 map-Rungs 만들기
    let updateRungMap(statements:Statement seq, mapRungs:Dictionary<IStorage, HashSet<Statement>>) =
        let total = getTotalTags  statements
        for item in total do
            mapRungs.TryAdd (item, HashSet<Statement>())|> verifyM $"Duplicated [{item.ToText()}]"
        let dicSource =
            statements
                .Select(fun s -> s, s.GetSourceStorages())
                |> dict |> Dictionary

        for rung in mapRungs do
            let sts = dicSource.Filter(fun f->f.Value.Contains(rung.Key))
            for st in sts do
                rung.Value.Add(st.Key) |> verifyM $"Duplicated [{ st.Key.ToText()}]"

    let getRungMap(statements:Statement seq) =
        let total = getTotalTags  statements
        let dicSource =
            statements
                .Select(fun s -> s, s.GetSourceStorages())
                |> dict |> Dictionary

        let map =
            total.Select(fun item ->
                let sts = dicSource.Filter(fun f->f.Value.Contains(item))
                item, sts.Select(fun st -> st.Key)
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

        chTags.Iter(fun f->  f.DsSystem.NotifyPreExcute(f)) 
        chTags.ChangedTagsClear(systems)

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

    /////Status4 상태보고 및 cpuRunMode.Event 처리
    //let runSubscribe(mapRungs:Dictionary<IStorage, HashSet<Statement>>, sys:DsSystem, cpuMode:CpuRunMode) =
    //    let subscribe =
    //        CpusEvent.ValueSubject
    //        //자신 CPU와 같은 시스템 또는 참조시스템만 연산처리
    //            .Where(fun (system, _storage, _value) -> system = sys || sys.ReferenceSystems.Contains(system:?> DsSystem))
    //            .Subscribe(fun (system, storage, _value) ->
    //            //Step 1 상태 UI 업데이트
    //            system.NotifyStatus(storage);
    //            //Step 2 관련수식 연산
    //            if cpuMode = ControlPc && mapRungs.ContainsKey storage
    //            then
    //                //for f in mapRungs[storage]
    //                //    do async{ f.Do()} |> Async.StartImmediate

    //                mapRungs[storage]
    //                |> Seq.map (fun f-> async { f.Do() } )
    //                |> Async.Parallel
    //                |> Async.Ignore
    //                |> Async.RunSynchronously
    //            )
    //    subscribe

