namespace Engine.Common.FS

open System
open System.Reactive.Subjects


[<AutoOpen>]
module ProcessEvent = 
    
    type ProParam = |PRO of Time:DateTime * pro:int

    let ProcessSubject = new Subject<ProParam>()

    let DoWork  (pro:int) = ProcessSubject.OnNext(ProParam.PRO (DateTime.Now, pro))
   
