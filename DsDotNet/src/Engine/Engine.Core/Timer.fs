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
module rec TimerModule =
    (*
        #r "nuget: System.Reactive"
        printfn "Current Time: %A" DateTime.Now
    *)

    // https://stackoverflow.com/questions/8771937/f-rx-using-a-timer

    /// Timer / Counter 의 number data type
    type CountUnitType = uint16

    let [<Literal>] MinTickInterval = 20us    //<ms>

    /// 20ms timer: 최소 주기.  Windows 상에서의 더 짧은 주기는 시스템에 무리가 있음!!!!
    let the20msTimer = Observable.Timer(TimeSpan.FromSeconds(0.0), TimeSpan.FromMilliseconds(int MinTickInterval))//.Timestamp()
    let the100msTimer = the20msTimer.Where(fun x -> x % 5L = 0)
    let the1secTimer = the20msTimer.Where(fun x -> x % 50L = 0)

    type TimerType = TON | TOF | RTO

    type internal TickAccumulator(timerType:TimerType, timerStruct:TimerStruct) =
        let ts = timerStruct
        let tt = timerType

        let accumulateTON() =
            if ts.TT.Value && not ts.DN.Value && ts.ACC.Value < ts.PRE.Value then
                ts.ACC.Value <- ts.ACC.Value + MinTickInterval
                if ts.ACC.Value >= ts.PRE.Value then
                    tracefn "Timer accumulator value reached"
                    ts.TT.Value <- false
                    ts.DN.Value <- true
                    ts.EN.Value <- true

        let accumulateTOF() =
            if ts.TT.Value && ts.DN.Value && not ts.EN.Value && ts.ACC.Value < ts.PRE.Value then
                ts.ACC.Value <- ts.ACC.Value + MinTickInterval
                if ts.ACC.Value >= ts.PRE.Value then
                    tracefn "Timer accumulator value reached"
                    ts.TT.Value <- false
                    ts.DN.Value <- false

        let accumulateRTO() =
            if ts.TT.Value && not ts.DN.Value && ts.EN.Value && ts.ACC.Value < ts.PRE.Value then
                ts.ACC.Value <- ts.ACC.Value + MinTickInterval
                if ts.ACC.Value >= ts.PRE.Value then
                    tracefn "Timer accumulator value reached"
                    ts.TT.Value <- false
                    ts.EN.Value <- false
                    ts.DN.Value <- true

        let accumulate() =
            tracefn "Accumutating from %A" ts.ACC.Value
            match tt with
            | TON -> accumulateTON()
            | TOF -> accumulateTOF()
            | RTO -> accumulateRTO()

        let disposables = new CompositeDisposable()

        do
            ts.Clear()

            tracefn "Timer subscribing to tick event"
            the20msTimer.Subscribe(fun _ -> accumulate()) |> disposables.Add

            ValueSubject
                .Where(fun storage -> storage = timerStruct.EN)
                .Subscribe(fun storage ->
                    if ts.ACC.Value < 0us || ts.PRE.Value < 0us then failwith "ERROR"
                    let rungInCondition = storage.Value :?> bool
                    tracefn "%A rung-condition-in=%b with DN=%b" tt rungInCondition ts.DN.Value
                    match tt, rungInCondition with
                    | TON, true ->
                        ts.TT.Value <- not ts.DN.Value
                    | TON, false -> ts.Clear()

                    | TOF, false ->
                        ts.EN.Value <- false
                        ts.TT.Value <- true
                        if not ts.DN.Value then
                            ts.ClearBits()

                    | TOF, true ->
                        ts.EN.Value <- true
                        ts.TT.Value <- false     // spec 상충함 : // https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 와 https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 설명이 다름
                        ts.DN.Value <- true
                        ts.ACC.Value <- 0us

                    | RTO, true ->
                        if ts.DN.Value then
                            ts.TT.Value <- false
                        else
                            ts.EN.Value <- true
                            ts.TT.Value <- true
                    | RTO, false ->
                        ts.EN.Value <- false
                        ts.TT.Value <- false
                ) |> disposables.Add

            ValueSubject
                .Where(fun storage -> storage = ts.RES)
                .Subscribe(fun storage ->
                    let resetCondition = storage.Value :?> bool
                    if resetCondition then
                        ts.ACC.Value <- 0us
                        ts.DN.Value <- false
                ) |> disposables.Add

        interface IDisposable with
            member this.Dispose() =
                for d in disposables do
                    d.Dispose()
                disposables.Clear()



    // 임시.  추후 수정 필요.  simualte Tag<bool>
    [<Obsolete("임시 구현")>]
    type BoolTag(name) =
        inherit Tag<bool>(name, false)
    [<Obsolete("임시 구현")>]
    type IntTag(name, init) =
        inherit Tag<CountUnitType>(name, init)

    [<AbstractClass>]
    type TimerCounterBaseStruct (name, preset, accum:CountUnitType) =
        member _.Name:string = name
        /// Done bit
        member val DN:Tag<bool> = fwdCreateBoolTag $"{name}.DN" false  // Done
        member val PRE:Tag<CountUnitType> = fwdCreateUShortTag $"{name}.PRE" preset
        member val ACC:Tag<CountUnitType> = fwdCreateUShortTag $"{name}.ACC" accum
        /// Reset bit.
        member val RES:Tag<bool> = fwdCreateBoolTag $"{name}.RES" false
    type TimerStruct internal(name, preset:CountUnitType, accum:CountUnitType) =
        inherit TimerCounterBaseStruct(name, preset, accum)
        /// Enable
        member val EN:Tag<bool> = fwdCreateBoolTag $"{name}.EN" false
        /// Timing
        member val TT:Tag<bool> = fwdCreateBoolTag $"{name}.TT" false


    type TimerStruct with
        /// Clear EN, TT, DN bits
        member x.ClearBits() =
            x.EN.Value <- false
            x.TT.Value <- false
            x.DN.Value <- false
        member x.Clear() =
            x.ClearBits()
            x.ACC.Value <- 0us

