namespace Engine.Core
open System
open System.Reactive.Linq

[<AutoOpen>]
module TimerStatementModule =

    let private createTimer(typ:TimerType, name, condition, target20msCounter) =
        let timer = new Timer(typ, name, condition, target20msCounter)
        let ts = timer.Struct
        let statement = Assign (condition, ts.EN)
        statement.Do()
        StorageValueChangedSubject.OnNext(ts.EN)

        timer.ConditionCheckStatement <- Some statement
        timer

    let CreateTON(name, condition, target20msCounter) = createTimer(TON, name, condition, target20msCounter)
    let CreateTOF(name, condition, target20msCounter) = createTimer(TOF, name, condition, target20msCounter)
