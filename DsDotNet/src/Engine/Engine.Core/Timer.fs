namespace Engine.Core
open System
open System.Threading
open System.Reactive.Linq
open System.Reactive.Disposables
open Dual.Common.Core.FS
open System.Runtime.InteropServices


(*
 - Timer 설정을 위한 조건: expression 으로 받음.
 - Timer statement 는 expression 을 매 scan 마다 평가.  값이 변경되면(rising or falling) 해당 timer 에 반영
 - Timer 가 설정되고 나면, observable timer 에 의해서 counter 값이 하나씩 감소하고, 0 이 되면 target trigger
*)

module TimerModuleApi =

    // DllImport 바인딩을 정적 멤버로 정의합니다.
    [<DllImport("winmm.dll", SetLastError = true)>]
    extern uint timeBeginPeriod(uint uPeriod)

    [<DllImport("winmm.dll", SetLastError = true)>]
    extern uint timeEndPeriod(uint uPeriod)


[<AutoOpen>]
module rec TimerModule =
    (*
        #r "nuget: System.Reactive"
        printfn "Current Time: %A" DateTime.Now
    *)

    // https://stackoverflow.com/questions/8771937/f-rx-using-a-timer

    /// Timer / Counter 의 number data type
    type CountUnitType = uint32

    let [<Literal>] MinTickInterval = 10u    //<ms>
    let [<Literal>] TimerResolution  = 1u    //<ms> windows timer resolution (1~ 1000000)

    /// 10ms timer: 최소 주기.  Windows 상에서의 더 짧은 주기는 시스템에 무리가 있음!!!!
    let theMinTickTimer = Observable.Timer(TimeSpan.FromSeconds(0.0), TimeSpan.FromMilliseconds(int MinTickInterval))//.Timestamp()

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

        let timerCallback (_: obj) = accumulate()

        let disposables = new CompositeDisposable()

        do
            ts.ResetStruct()

            debugfn "Timer subscribing to tick event"
            //theMinTickTimer.Subscribe(fun _ -> accumulate()) |> disposables.Add

            TimerModuleApi.timeBeginPeriod(TimerResolution) |> ignore
            new Timer(TimerCallback(timerCallback), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(float MinTickInterval)) |> disposables.Add

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
                TimerModuleApi.timeEndPeriod(TimerResolution) |> ignore

                for d in disposables do
                    d.Dispose()
                disposables.Clear()


    [<AbstractClass>]
    type TimerCounterBaseStruct (isTimer:bool option, name, dn, pre, acc, res, sys) =
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
            member _.IsAutoGenerated with get() = true and set(_v) = noop()
            member val Comment = "" with get, set
            member x.BoxedValue with get() = x.This and set(_v) = unsupported()
            member x.ObjValue = x.This
            member x.ToText() = unsupported()
            member _.ToBoxedExpression() = unsupported()
            member x.CompareTo(other) = String.Compare(x.Name, (other:?>IStorage).Name)
            member _.MaintenanceInfo with get() = unsupported() and set(_v) = unsupported()

        member private x.This = x
        member _.Name:string = name
        /// Done bit
        member _.DN:VariableBase<bool> = dn
        /// Preset value
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
        /// XGK 에서 할당한 counter/timer 변수 이름 임시 저장 공간.  e.g "C0001"
        member x.XgkStructVariableName =
            match isTimer with
            | Some true -> sprintf "T%04d" x.XgkStructVariableDevicePos
            | Some false -> sprintf "C%04d" x.XgkStructVariableDevicePos
            | _ -> failwith "ERROR"

        member val XgkStructVariableDevicePos = -1 with get, set

    let addTagsToStorages (storages:Storages) (ts:IStorage seq) =
        for t in ts do
            if not (isItNull t) then
                storages.Add(t.Name, t)

    let createUShortTagKind name iniValue tagKind = fwdCreateUShortMemberVariable name iniValue tagKind
    let createUShort name iniValue  = createUShortTagKind name iniValue skipValueChangedForTagKind

    let createUInt32TagKind name iniValue tagKind = fwdCreateUInt32MemberVariable name iniValue tagKind
    let createUInt32 name iniValue  = createUInt32TagKind name iniValue skipValueChangedForTagKind

    let createBoolWithTagKind name iniValue tagKind = fwdCreateBoolMemberVariable name iniValue tagKind
    let createBool name iniValue  = createBoolWithTagKind name iniValue skipValueChangedForTagKind
    let xgkTimerCounterContactMarking = "$ON"
    type TimerStruct private(typ:TimerType, name, en, tt, dn, pre, acc, res, sys) =
        inherit TimerCounterBaseStruct(Some true, name, dn, pre, acc, res, sys)

        /// Enable
        member _.EN:VariableBase<bool> = en
        /// Timing
        member _.TT:VariableBase<bool> = tt
        member _.Type = typ

        static member Create(typ:TimerType, storages:Storages, name, preset:CountUnitType, accum:CountUnitType, sys, target:PlatformTarget) =
            let suffixes  =
                match target with
                | XGK -> [".IN"; ".TT"; xgkTimerCounterContactMarking; ".PT"; ".ET"; ".RST"] // XGK 이름에 . 있으면 걸러짐 storagesToXgxSymbol
                | XGI | WINDOWS -> [".IN"; "._TT"; ".Q"; ".PT"; ".ET"; ".RST"]
                | AB -> [".EN"; ".TT"; ".DN"; ".PRE"; ".ACC"; ".RES"]
                | _ -> failwith "NOT yet supported"

            let en, tt, dn, pre, acc, res =
                let names = suffixes |> Seq.map (fun suffix -> $"{name}{suffix}") |> Seq.toList
                match names with
                | [en; tt; dn; pre; acc; res] -> en, tt, dn, pre, acc, res
                | _ -> failwith "Unexpected number of suffixes"

            let en  = createBool              $"{en }" false
            let tt  = createBool              $"{tt }" false
            let dn  = createBoolWithTagKind   $"{dn }" false (VariableTag.PcSysVariable|>int) // Done
            let pre = createUInt32            $"{pre}" preset
            let acc = createUInt32            $"{acc}" accum
            let res = createBool              $"{res}" false

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

