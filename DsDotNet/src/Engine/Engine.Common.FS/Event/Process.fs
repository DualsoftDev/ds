namespace Engine.Common.FS

open System
open System.Reactive.Subjects


[<AutoOpen>]
module ProcessEvent =

    type ProParam = |PRO of Time:DateTime * pro:int

    let mutable currProcess:int = 0
    let ProcessSubject = new Subject<ProParam>()
    let CurrProcess = currProcess

    let DoWork  (pro:int) =
        currProcess <- pro
        ProcessSubject.OnNext(ProParam.PRO (DateTime.Now, pro))

    let IsBusy()  = 0 < currProcess && currProcess < 100 
