namespace Engine.Core
open System
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Reactive.Disposables
open Engine.Common.FS


(*
 - Timer 설정을 위한 조건: expression 으로 받음.
 - Timer statement 는 expression 을 매 scan 마다 평가.  값이 변경되면(rising or falling) 해당 timer 에 반영
 - Timer 가 설정되고 나면, observable timer 에 의해서 counter 값이 하나씩 감소하고, 0 이 되면 target trigger
*)

[<AutoOpen>]
module rec CounterModule =
    type CounterType = CTU | CTD | CTUD

    [<AbstractClass>]
    type CounterBaseStruct(name, preset:CountUnitType, accum:CountUnitType) =
        inherit TimerCounterBaseStruct(name, preset, accum)

        member val internal CU:Tag<bool> = BoolTag($"{name}.CU")  // Count up enable bit
        member val internal CD:Tag<bool> = BoolTag($"{name}.CD")  // Count down enable bit
        member val OV:Tag<bool> = BoolTag($"{name}.OV")  // Overflow
        member val UN:Tag<bool> = BoolTag($"{name}.UN")  // Underflow

    type ICounter = interface end

    type ICTU =
        inherit ICounter
        abstract CU:Tag<bool>

    type ICTD =
        inherit ICounter
        abstract CD:Tag<bool>

    type ICTUD =
        inherit ICTU
        inherit ICTD


    type CTUStruct(name, preset:CountUnitType, accum:CountUnitType) =
        inherit CounterBaseStruct(name, preset, accum)
        member _.CU = base.CU
        interface ICTU with
            member x.CU = x.CU

    type CTDStruct(name, preset:CountUnitType, accum:CountUnitType) =
        inherit CounterBaseStruct(name, preset, accum)
        member _.CD = base.CD
        interface ICTD with
            member x.CD = x.CD

    type CTUDStruct(name, preset:CountUnitType, accum:CountUnitType) =
        inherit CounterBaseStruct(name, preset, accum)
        member _.CU = base.CU
        member _.CD = base.CD
        interface ICTUD with
            member x.CU = x.CU
            member x.CD = x.CD

    type internal CountAccumulator(counterType:CounterType, counterStruct:CounterBaseStruct) =
        let disposables = new CompositeDisposable()

        let cs = counterStruct
        let registerCTU() =
            let csu = box cs :?> ICTU
            StorageValueChangedSubject
                .Where(fun storage -> storage = csu.CU && csu.CU.Value)
                .Subscribe(fun storage ->
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- cs.ACC.Value + 1us
                    if cs.ACC.Value >= cs.PRE.Value then
                        tracefn "Counter accumulator value reached"
                        cs.DN.Value <- true
            ) |> disposables.Add
        let registerCTD() =
            let csd = box cs :?> ICTD
            StorageValueChangedSubject
                .Where(fun storage -> storage = csd.CD && csd.CD.Value)
                .Subscribe(fun storage ->
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- cs.ACC.Value - 1us
                    if cs.ACC.Value <= cs.PRE.Value then
                        tracefn "Counter accumulator value reached"
                        cs.DN.Value <- true
            ) |> disposables.Add

        let registerReset() =
            StorageValueChangedSubject
                .Where(fun storage -> storage = cs.RES && cs.RES.Value)
                .Subscribe(fun storage ->
                    tracefn "Counter reset requested"
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- 0us
                    cs.DN.Value <- false
                    cs.CU.Value <- false
                    cs.CD.Value <- false
                    cs.OV.Value <- false
                    cs.UN.Value <- false
            ) |> disposables.Add


        let clear() =
            cs.OV.Value <- false
            cs.UN.Value <- false
            cs.DN.Value <- false
            cs.CU.Value <- false
            cs.CD.Value <- false
            cs.ACC.Value <- 0us

        do
            clear()
            registerReset()
            match cs, counterType with
            | :? CTUStruct, CTU -> registerCTU()
            | :? CTDStruct, CTD -> registerCTD()
            | :? CTUDStruct, CTUD -> registerCTU(); registerCTD();
            | _ -> failwith "ERROR"

        interface IDisposable with
            member this.Dispose() =
                for d in disposables do
                    d.Dispose()
                disposables.Clear()


