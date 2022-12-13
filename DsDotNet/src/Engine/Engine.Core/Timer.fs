namespace Engine.Core
open System
open System.Reactive.Linq


[<AutoOpen>]

module TimerModule =
    #r "nuget: System.Reactive"

    printfn "Current Time: %A" DateTime.Now


    // https://stackoverflow.com/questions/8771937/f-rx-using-a-timer

    /// 20ms timer: 최소 주기.  Windows 상에서의 더 짧은 주기는 시스템에 무리가 있음!!!!
    let the20msTimer = Observable.Timer(TimeSpan.FromSeconds(0.0), TimeSpan.FromMilliseconds(20))//.Timestamp()
    let the100msTimer = the20msTimer.Where(fun x -> x % 5L = 0)
    let the1secTimer = the20msTimer.Where(fun x -> x % 50L = 0)
    //source.Add(fun x -> printfn "%A %A" x.Value x.Timestamp)
    let subscription = the1secTimer.Subscribe(printfn "%A")


    type Fire = unit -> unit
    type Firere(target20msCounter:uint16, fire:Fire) =
        let mutable counter = target20msCounter
        let mutable subscription:IDisposable = null
        let unsubscribe() =
            subscription.Dispose()
            subscription <- null

        member x.Start() =
            x.Reset()
            x.Resume()

        member x.Pause() = unsubscribe()

        member x.Resume() =
            if subscription <> null then
                failwith "ERROR"

            subscription <-
                the20msTimer.Subscribe(fun _ ->
                    counter <- counter - 1us
                    if counter = 0us then
                        x.Reset()
                        fire()
                )

        member x.Reset() =
            counter <- target20msCounter
            if subscription <> null then
                unsubscribe()

        /// 현재 진행 중 시간 / 남은 시간 / ....
        member _.Counter = counter
        member _.Target = target20msCounter
        //interface INamed with

    type TimerType = TON | TOFF | TMR | TMON
    type TimerVariable(time:uint16, goal:uint16) =
        let mutable timer = time

    type Timer private(name, set:IExpression, target20msCounter:uint16, fire:Fire) =
        inherit Firere(target20msCounter, fire)
        member _.Name:string = name
        /// Set 조건
        member _.Set = set

        static member CreateTON(name, set, target20msCounter) =
            //let mutable fired = Tag<bool>($"Timer_{name}", false)
            let mutable fired = false   // simualte Tag<bool>
            let fire() = fired <- true
            new Timer(name, set, target20msCounter, fire)

