namespace Engine.Core
open System
open System.Reactive.Linq
open System.Reactive.Subjects
open Engine.Common.FS


(*
 - Timer 설정을 위한 조건: expression 으로 받음.
 - Timer statement 는 expression 을 매 scan 마다 평가.  값이 변경되면(rising or falling) 해당 timer 에 반영
 - Timer 가 설정되고 나면, observable timer 에 의해서 counter 값이 하나씩 감소하고, 0 이 되면 target trigger
*)

[<AutoOpen>]
module rec TimerModule =
    #r "nuget: System.Reactive"

    printfn "Current Time: %A" DateTime.Now


    // https://stackoverflow.com/questions/8771937/f-rx-using-a-timer

    /// 20ms timer: 최소 주기.  Windows 상에서의 더 짧은 주기는 시스템에 무리가 있음!!!!
    let the20msTimer = Observable.Timer(TimeSpan.FromSeconds(0.0), TimeSpan.FromMilliseconds(20))//.Timestamp()
    let the100msTimer = the20msTimer.Where(fun x -> x % 5L = 0)
    let the1secTimer = the20msTimer.Where(fun x -> x % 50L = 0)
    //source.Add(fun x -> printfn "%A %A" x.Value x.Timestamp)
    let subscription = the1secTimer.Subscribe(printfn "%A")

    type TimerType = TON | TOF | RTO

    type Fire = unit -> unit
    type Firere(timerType:TimerType, timerStruct:TimerStruct) as this =
        let ts = timerStruct
        let tt = timerType

        let mutable clockSubscription:IDisposable = null
        let unsubscribe() =
            clockSubscription.Dispose()
            clockSubscription <- null

        let tonPreScan() =
            ts.EN.Value <- false
            ts.TT.Value <- false
            ts.DN.Value <- false
            ts.ACC.Value <- 0us
        let tonRungConditionInFalse() = tonPreScan()
        let tonPostScan() = tonPreScan()

        let tofPreScan() =
            ts.EN.Value <- false
            ts.TT.Value <- false
            ts.DN.Value <- true
            ts.ACC.Value <- ts.PRE.Value
        //let tofRungConditionInTrue() =
        //    ts.EN.Value <- true
        //    ts.TT.Value <- true     // https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 와 https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 설명이 다름
        //    ts.DN.Value <- true
        //    ts.ACC.Value <- 0us
        //let tofPostScan() = tofPreScan()

        let rtoPreScan() = failwith "ERROR"
        let rtoConditionTrue() = failwith "ERROR"
        let rtoPostScan() = failwith "ERROR"

        let preScan() =
            match tt with
            | TON -> tonPreScan()
            | TOF -> tofPreScan()
            | RTO -> rtoPreScan()

        let clear() =
            if clockSubscription <> null then
                unsubscribe()
            preScan()

        let resume() =
            if clockSubscription <> null then
                failwith "ERROR"

            let accumulate() =
                if ts.TT.Value && not ts.DN.Value then
                    ts.ACC.Value <- ts.ACC.Value + 1us
                    tracefn "Accumutated to %A" ts.ACC.Value
                    if ts.ACC.Value >= ts.PRE.Value then
                        //clear()
                        tracefn "Timer accumulator value reached"
                        ts.TT.Value <- false
                        match tt with
                        | TON ->
                            ts.DN.Value <- true
                            ts.EN.Value <- true
                        | TOF ->
                            ts.DN.Value <- false
                            ts.EN.Value <- false

            tracefn "Timer subscribing to tick event"
            clockSubscription <-
                the20msTimer.Subscribe(fun _ ->
                    let xxx = ts
                    match tt, ts.TT.Value with
                    | (TON|TOF), true when not ts.DN.Value -> accumulate()        // When enabled, timing can be paused by setting the .DN bit to true and resumed by clearing the .DN
                    | _, true -> accumulate()
                    | _, _ -> ()
                )

        do
            preScan()
            StorageValueChangedSubject
                .Where(fun storage -> storage = timerStruct.EN)
                .Subscribe(fun storage ->
                    if ts.ACC.Value < 0us || ts.PRE.Value < 0us then failwith "ERROR"
                    let enabled = storage.Value :?> bool
                    match tt, enabled with
                    | TON, true ->
                        tracefn "TON enabled with DN=%b" ts.DN.Value
                        ts.TT.Value <- not ts.DN.Value
                        if ts.TT.Value then
                            resume()
                    | TON, false -> preScan()

                    | TOF, false ->
                        tracefn "TOF enabled with DN=%b" ts.DN.Value
                        ts.EN.Value <- false
                        ts.TT.Value <- true
                        ts.ACC.Value <- 0us // When the TOF instruction is disabled, the ACC value is cleared       https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf  pp121
                        if not ts.EN.Value then
                            resume()

                    | TOF, true ->
                        ts.EN.Value <- true
                        ts.TT.Value <- true     // spec 상충함 : // https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 와 https://edisciplinas.usp.br/pluginfile.php/184942/mod_resource/content/1/Logix5000%20-%20Manual%20de%20Referencias.pdf 설명이 다름
                        ts.DN.Value <- true
                        ts.ACC.Value <- 0us
                        //preScan()
                ) |> ignore

        member x.Pause() = unsubscribe()


        member val internal Fire:Fire option = None with get, set



    // 임시.  추후 수정 필요.  simualte Tag<bool>
    type BoolTag(name) =
        inherit Tag<bool>(name, false)
    type IntTag(name, init) =
        inherit Tag<uint16>(name, init)

    type TimerStruct internal(name, preset20msCounter:uint16) =
        member _.Name:string = name
        /// Set 조건 : 멤버에서 삭제 가능?

        member val EN:BoolTag = BoolTag($"{name}.EN")  // Enable
        member val TT:BoolTag = BoolTag($"{name}.TT")  // Timing
        member val DN:BoolTag = BoolTag($"{name}.DN")  // Done
        member val PRE:IntTag = IntTag( $"{name}.PRE", preset20msCounter)
        member val ACC:IntTag = IntTag( $"{name}.ACC", 0us)



