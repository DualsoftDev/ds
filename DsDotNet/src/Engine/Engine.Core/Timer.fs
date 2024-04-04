namespace Engine.Core
open System
open System.Reactive.Linq
open System.Reactive.Disposables
open Dual.Common.Core.FS


(*
 - Timer 설정을 위한 조건: expression 으로 받음.
 - Timer statement 는 expression 을 매 scan 마다 평가.  값이 변경되면(rising or falling) 해당 timer 에 반영
 - Timer 가 설정되고 나면, observable timer 에 의해서 counter 값이 하나씩 감소하고, 0 이 되면 target trigger
*)

[<AutoOpen>]
module rec TimerModule =
    (*
        #r "nuget: System.Reactive"
        printfn "Current Time: %A" DateTime.Now
    *)

    // https://stackoverflow.com/questions/8771937/f-rx-using-a-timer

    /// Timer / Counter 의 number data type
    type CountUnitType = uint32

    let [<Literal>] MinTickInterval = 20u    //<ms>

    /// 20ms timer: 최소 주기.  Windows 상에서의 더 짧은 주기는 시스템에 무리가 있음!!!!
    let the20msTimer = Observable.Timer(TimeSpan.FromSeconds(0.0), TimeSpan.FromMilliseconds(int MinTickInterval))//.Timestamp()
    let the100msTimer = the20msTimer.Where(fun x -> x % 5L = 0)
    let the1secTimer = the20msTimer.Where(fun x -> x % 50L = 0)

    type TimerType = TON | TOF | TMR        // AB 에서 TMR 은 RTO 에 해당

    type internal TickAccumulator(timerType:TimerType, timerStruct:TimerStruct) =
        let ts = timerStruct
        let tt = timerType

        let accumulateTON() =
            if ts.TT.Value && not ts.DN.Value && ts.ACC.Value < ts.PRE.Value then
                ts.ACC.Value <- ts.ACC.Value + MinTickInterval
                if ts.ACC.Value >= ts.PRE.Value then
                    debugfn "Timer accumulator value reached"
                    ts.TT.Value <- false
                    ts.DN.Value <- true
                    ts.EN.Value <- true

        let accumulateTOF() =
            if ts.TT.Value && ts.DN.Value && not ts.EN.Value && ts.ACC.Value < ts.PRE.Value then
                ts.ACC.Value <- ts.ACC.Value + MinTickInterval
                if ts.ACC.Value >= ts.PRE.Value then
                    debugfn "Timer accumulator value reached"
                    ts.TT.Value <- false
                    ts.DN.Value <- false

        let accumulateRTO() =
            if ts.TT.Value && not ts.DN.Value && ts.EN.Value && ts.ACC.Value < ts.PRE.Value then
                ts.ACC.Value <- ts.ACC.Value + MinTickInterval
                if ts.ACC.Value >= ts.PRE.Value then
                    debugfn "Timer accumulator value reached"
                    ts.TT.Value <- false
                    ts.EN.Value <- false
                    ts.DN.Value <- true

        let accumulate() =
            //debugfn "Accumulating from %A" ts.ACC.Value
            match tt with
            | TON -> accumulateTON()
            | TOF -> accumulateTOF()
            | TMR -> accumulateRTO()

        let disposables = new CompositeDisposable()

        do
            ts.ResetStruct()

            //debugfn "Timer subscribing to tick event"
            the20msTimer.Subscribe(fun _ -> accumulate()) |> disposables.Add

            CpusEvent.ValueSubject.Where(fun (system, _storage, _value) -> system = (timerStruct:>IStorage).DsSystem)
                .Where(fun (_system, storage, _newValue) -> storage = timerStruct.EN)
                .Subscribe(fun (_system, _storage, newValue) ->
                    if ts.ACC.Value < 0u || ts.PRE.Value < 0u then failwithlog "ERROR"
                    let rungInCondition = newValue :?> bool
                    //debugfn "%A rung-condition-in=%b with DN=%b" tt rungInCondition ts.DN.Value
                    match tt, rungInCondition with
                    | TON, true ->
                        ts.TT.Value <- not ts.DN.Value
                    | TON, false -> ts.ResetStruct()

                    | TOF, false ->
                        ts.EN.Value <- false
                        ts.TT.Value <- true
                        if not ts.DN.Value then
                            ts.ClearBits()

                    | TOF, true ->
                        ts.EN.Value <- true
                        ts.TT.Value <- false     // spec 상충함 : // https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 와 https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 설명이 다름
                        ts.DN.Value <- true
                        ts.ACC.Value <- 0u

                    | TMR, true ->
                        if ts.DN.Value then
                            ts.TT.Value <- false
                        else
                            ts.EN.Value <- true
                            ts.TT.Value <- true
                    | TMR, false ->
                        ts.EN.Value <- false
                        ts.TT.Value <- false
                ) |> disposables.Add

            CpusEvent.ValueSubject.Where(fun (system, _storage, _value) -> system = (timerStruct:>IStorage).DsSystem)
                .Where(fun (_system, storage, _newValue) -> storage = ts.RES)
                .Subscribe(fun (_system, _storage, newValue) ->
                    let resetCondition = newValue :?> bool
                    if resetCondition then
                        ts.ACC.Value <- 0u
                        ts.DN.Value <- false
                ) |> disposables.Add

        interface IDisposable with
            member this.Dispose() =
                for d in disposables do
                    d.Dispose()
                disposables.Clear()


    [<AbstractClass>]
    type TimerCounterBaseStruct (name, dn, pre, acc, res, sys) =
        let unsupported() = failwithlog "ERROR: not supported"
        let mutable tagChanged = false
        interface IStorage with
            member _.DsSystem = sys
            member x.Target = None
            member x.TagKind = skipValueChangedForTagKind //CpusEvent.ValueSubject TagKind  -1 필터링 용도로 사용
            member x.TagChanged  with get() = tagChanged and set(v) = tagChanged <- v
            member x.Name with get() = x.Name and set(_v) = unsupported()
            member _.Address with get() = unsupported() and set(_v) = unsupported()
            member _.DataType = typedefof<TimerCounterBaseStruct>
            member _.IsGlobal with get() = true and set(_v) = noop()
            member val Comment = "" with get, set
            member x.BoxedValue with get() = x.This and set(_v) = unsupported()
            member x.ObjValue = x.This
            member x.ToText() = unsupported()
            member _.ToBoxedExpression() = unsupported()
            member x.CompareTo(other) = String.Compare(x.Name, (other:?>IStorage).Name) 

        member private x.This = x
        member _.Name:string = name
        /// Done bit
        member _.DN:VariableBase<bool> = dn
        member _.PRE:VariableBase<CountUnitType> = pre
        member _.ACC:VariableBase<CountUnitType> = acc
        /// Reset bit.
        member _.RES:VariableBase<bool> = res
        /// XGI load
        member _.LD:VariableBase<bool> = res
        abstract member ResetStruct:unit -> unit
        default x.ResetStruct() =
            let clearBool(b:VariableBase<bool>) =
                if b |> isItNull |> not then
                    b.Value <- false
            // -- preset 은 clear 대상이 아님: x.PRE,     reset 도 clear 해야 하는가? -- ???  x.RES
            clearVarBoolsOnDemand( [x.DN; x.LD;] )
            clearBool(x.LD)
            if x.ACC |> isItNull |> not then
                x.ACC.Value <- 0u

    let addTagsToStorages (storages:Storages) (ts:IStorage seq) =
        for t in ts do
            if not (isItNull t) then
                storages.Add(t.Name, t)

    let createUShortTagKind name iniValue tagKind = fwdCreateUShortMemberVariable name iniValue tagKind
    let createUShort name iniValue  = createUShortTagKind name iniValue -1

    let createUInt32TagKind name iniValue tagKind = fwdCreateUInt32MemberVariable name iniValue tagKind
    let createUInt32 name iniValue  = createUInt32TagKind name iniValue -1

    let createBoolWithTagKind name iniValue tagKind = fwdCreateBoolMemberVariable name iniValue tagKind
    let createBool name iniValue  = createBoolWithTagKind name iniValue -1

    type TimerStruct private(typ:TimerType, name, en, tt, dn, pre, acc, res, sys) =
        inherit TimerCounterBaseStruct(name, dn, pre, acc, res, sys)

        /// Enable
        member _.EN:VariableBase<bool> = en
        /// Timing
        member _.TT:VariableBase<bool> = tt
        member _.Type = typ

        static member Create(typ:TimerType, storages:Storages, name, preset:CountUnitType, accum:CountUnitType, sys) =
            let en, tt, dn, pre, acc, res =
                match RuntimeDS.Target with
                | ( XGI | WINDOWS ) -> "IN", "_TT", "Q", "PT", "ET", "RST"
                | AB -> "EN", "TT", "DN", "PRE", "ACC", "RES"
                | _ -> failwithlog "NOT yet supported"

            let en  = createBool              $"{name}.{en }" false
            let tt  = createBool              $"{name}.{tt }" false
            let dn  = createBoolWithTagKind   $"{name}.{dn }" false (VariableTag.PcSysVariable|>int) // Done
            let pre = createUInt32            $"{name}.{pre}" preset
            let acc = createUInt32            $"{name}.{acc}" accum
            let res = createBool              $"{name}.{res}" false
            
            storages.Add(en.Name, en)
            storages.Add(tt.Name, tt)
            storages.Add(dn.Name, dn)
            storages.Add(pre.Name, pre)
            storages.Add(acc.Name, acc)
            storages.Add(res.Name, res)

            let ts = new TimerStruct(typ, name, en, tt, dn, pre, acc, res, sys)
            storages.Add(name, ts)
            ts

        /// Clear EN, TT, DN bits
        member x.ClearBits() =
            clearVarBoolsOnDemand( [x.EN; x.TT; x.DN;] )

        override x.ResetStruct() =
            base.ResetStruct()
            x.ClearBits()
            x.ACC.Value <- 0u
            // x.PRE.Value <- 0us       // preset 도 clear 해야 하는가?
            ()

