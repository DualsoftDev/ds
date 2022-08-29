#r "nuget: System.Reactive" 
#r "nuget: Microsoft.Reactive.Testing"

open System
open System.Reactive
open System.Reactive.Linq
open System.Linq
open System.Threading
open System.Runtime.CompilerServices
open Microsoft.Reactive.Testing
open System.Reactive.Disposables

let dumpH hdr x = printfn "%s%A" hdr x
let dumpExnH hdr (exn:exn) = printfn "%s%O" hdr exn

let dump x = dumpH "" x
let dumpExn exn = dumpExnH "Caught Excetion: " exn


type ConsoleObserver<'T>(name) =
    new() = ConsoleObserver("")

    interface IObserver<'T> with
        member x.OnNext(value) =
            printfn "%s - OnNext(%A)" name value
        member x.OnError(error:Exception) =
            printfn "%s - OnError:\n\t%O" name error            
        member x.OnCompleted() =
            printfn "%s - OnCompleted()" name


[<Extension>] // type ConsoleObserverExt =
type ConsoleObserverExt =
    [<Extension>]
    static member SubscribeConsole(observable:IObservable<'T>, name) =
        observable.Subscribe(new ConsoleObserver<'T>(name))
    [<Extension>]
    static member SubscribeConsole(observable:IObservable<'T>) = 
        observable.Subscribe(new ConsoleObserver<'T>(""))


let testSubscribe() =
    Observable.Interval(TimeSpan.FromSeconds(1.0)).Subscribe(dump)

let testSubscribe2() =
    // subscribe 이후, 바로 dispose 되고 나서 생성되는 observable event 는 무시된다.
    Observable
        .Start(fun () ->
            Thread.Sleep(TimeSpan.FromSeconds(2.0))
            5)
        .Subscribe(dump)
        .Dispose()

let testException() =
    Observable
        .Range(1, 5)
        .Select(fun x -> x/(x - 3))
        .Subscribe(dump, dumpExn) // do something with the exception

let testCSharpStyleChaining() =
    Observable
        .Timer(DateTimeOffset.Now,TimeSpan.FromSeconds(1.0))
        .Select(fun _ -> DateTimeOffset.Now)
        .TakeUntil(DateTimeOffset.Now.AddSeconds(5.0))
        .Subscribe(printfn "TakeUntil(time):%A")


let testFSharpStylePipe() =
    Observable
        .Timer(DateTimeOffset.Now,TimeSpan.FromSeconds(1.0))
        |> Observable.map (fun _ -> DateTimeOffset.Now)
        |> fun x -> x.TakeUntil(DateTimeOffset.Now.AddSeconds(5.0))
        |> fun x -> x.Subscribe(printfn "TakeUntil(time):%A")


let testCreate() =
    Observable.Create(fun (observer:IObserver<int>) ->
        observer.OnNext(1)
        observer.OnNext(2)
        observer.OnNext(3)
        observer.OnCompleted()
        Disposable.Empty
        // or Disposable.Create(fun () -> printfn "Observer has unsubscribed")
    )


let testRefCount() =
    // pp. 181, Rx.NET in action.pdf
    let publishedObservable =
        Observable
            .Interval(TimeSpan.FromSeconds(1.0))
            .Do(dumpH "Generating")
            .Publish()
            .RefCount()

    let subscription1 = publishedObservable.Subscribe(dumpH "First\t");
    let subscription2 = publishedObservable.Subscribe(dumpH "Second\t");
    Thread.Sleep(3000)
    subscription1.Dispose()
    Thread.Sleep(3000)
    subscription2.Dispose()

let testCreateHotObservable() =
    let sched = new TestScheduler()    
    let input =
        sched.CreateHotObservable(
            ReactiveTest.OnNext(200L, 1)
            , ReactiveTest.OnNext(300L, 10)
            , ReactiveTest.OnNext(400L, 100)
            , ReactiveTest.OnCompleted<int>(1100L)
            )
    input
        .Do(dumpH "Side Effects: ")
        .Subscribe (dumpH "Hot: ") |> ignore
    sched.Start()

    input.Subscribe (dumpH "Hot Later: ") |> ignore



let testCreateColdObservable() =
    let sched = new TestScheduler()    
    let input =
        sched.CreateColdObservable(
            ReactiveTest.OnNext(200L, 1)
            , ReactiveTest.OnNext(300L, 10)
            , ReactiveTest.OnNext(400L, 100)
            , ReactiveTest.OnCompleted<int>(1100L)
            )
    input
        .Do(dumpH "Side Effects: ")
        .Subscribe (ConsoleObserver("Cold: ")) |> ignore
    sched.Start()

    input.Subscribe (dumpH "Cold Later: ") |> ignore

    //let published = input.Do(fun x -> Console.WriteLine("Effects!")).Publish()
    let published = input.Do(dumpH "Effects!").Publish()
    published.Subscribe(dump) |> ignore
    published.Subscribe(dump) |> ignore
    sched.Start()


