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
    type CounterType =
        /// UP Counter
        CTU
        /// DOWN Counter
        | CTD
        /// UP/DOWN Counter
        | CTUD
        /// Ring Counter
        | CTR

    [<AbstractClass>]
    type CounterBaseStruct(typ:CounterType, storages:Storages, name, preset:CountUnitType, accum:CountUnitType) =
        inherit TimerCounterBaseStruct(storages, name, preset, accum)

        let cu = fwdCreateBoolTag $"{name}.CU" false  // Count up enable bit
        let cd = fwdCreateBoolTag $"{name}.CD" false  // Count down enable bit
        let ov = fwdCreateBoolTag $"{name}.OV" false  // Overflow
        let un = fwdCreateBoolTag $"{name}.UN" false  // Underflow
        do
            storages.Add($"{name}.CU", cu)
            storages.Add($"{name}.CD", cd)
            storages.Add($"{name}.OV", ov)
            storages.Add($"{name}.UN", un)

        member _.CU:TagBase<bool> = cu  // Count up enable bit
        member _.CD:TagBase<bool> = cd  // Count down enable bit
        member _.OV:TagBase<bool> = ov  // Overflow
        member _.UN:TagBase<bool> = un  // Underflow
        member _.Type = typ

    type ICounter = interface end

    type ICTU =
        inherit ICounter
        abstract CU:TagBase<bool>

    type ICTD =
        inherit ICounter
        abstract CD:TagBase<bool>

    type ICTUD =
        inherit ICTU
        inherit ICTD

    type ICTR =
        inherit ICounter
        abstract CU:TagBase<bool>


    type CTUStruct(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
        inherit CounterBaseStruct(typ, storages, name, preset, accum)
        member _.CU = base.CU
        interface ICTU with
            member x.CU = x.CU

    type CTDStruct(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
        inherit CounterBaseStruct(typ, storages, name, preset, accum)
        member _.CD = base.CD
        interface ICTD with
            member x.CD = x.CD

    type CTUDStruct(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
        inherit CounterBaseStruct(typ, storages, name, preset, accum)
        member _.CU = base.CU
        member _.CD = base.CD
        interface ICTUD with
            member x.CU = x.CU
            member x.CD = x.CD

    type CTRStruct(typ:CounterType, storages, name, preset:CountUnitType, accum:CountUnitType) =
        inherit CounterBaseStruct(typ, storages, name, preset, accum)
        member _.CU = base.CU
        interface ICTR with
            member x.CU = x.CU

    type internal CountAccumulator(counterType:CounterType, counterStruct:CounterBaseStruct) =
        let disposables = new CompositeDisposable()

        let cs = counterStruct
        let registerCTU() =
            let csu = box cs :?> ICTU
            ValueSubject
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
            ValueSubject
                .Where(fun storage -> storage = csd.CD && csd.CD.Value)
                .Subscribe(fun storage ->
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- cs.ACC.Value - 1us
                    if cs.ACC.Value <= cs.PRE.Value then
                        tracefn "Counter accumulator value reached"
                        cs.DN.Value <- true
            ) |> disposables.Add

        let registerCTR() =
            let csr = box cs :?> ICTR
            ValueSubject
                .Where(fun storage -> storage = csr.CU && csr.CU.Value)
                .Subscribe(fun storage ->
                    if cs.ACC.Value < 0us || cs.PRE.Value < 0us then failwith "ERROR"
                    cs.ACC.Value <- cs.ACC.Value + 1us
                    if cs.ACC.Value = cs.PRE.Value then
                        tracefn "Counter accumulator value reached"
                        cs.DN.Value <- true
                    if cs.ACC.Value > cs.PRE.Value then
                        cs.ACC.Value <- 1us
                        cs.DN.Value <- false
            ) |> disposables.Add

        let registerReset() =
            ValueSubject
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
            | :? CTRStruct, CTR -> registerCTR()
            | :? CTDStruct, CTD -> registerCTD()
            | :? CTUDStruct, CTUD -> registerCTU(); registerCTD();
            | _ -> failwith "ERROR"

        interface IDisposable with
            member this.Dispose() =
                for d in disposables do
                    d.Dispose()
                disposables.Clear()


