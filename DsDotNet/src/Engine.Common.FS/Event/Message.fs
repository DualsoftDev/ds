namespace Engine.Common.FS

open System
open System.Reactive.Subjects

[<AutoOpen>]
module MessageEvent = 
    
    type MSGLevel = |Info | Warn | Error
    type MSGParam = |MSG of Time:DateTime * Level:MSGLevel * Message:string

    let MSGSubject = new Subject<MSGParam>()
    /// Message 공지.
    let MSGInfo (text:string)  = MSGSubject.OnNext(MSGParam.MSG (DateTime.Now, Info, text))
    let MSGWarn (text:string)  = MSGSubject.OnNext(MSGParam.MSG (DateTime.Now, Warn, text))
    let MSGError (text:string) = MSGSubject.OnNext(MSGParam.MSG (DateTime.Now, Error, text))
   
