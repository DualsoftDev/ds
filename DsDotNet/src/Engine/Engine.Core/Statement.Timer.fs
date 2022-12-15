namespace Engine.Core
open System
open System.Reactive.Linq

[<AutoOpen>]
module TimerStatementModule =

    let private createTimer(typ:TimerType, name, rungConditionIn, preset20msCounter) =
        let ts = TimerStruct(name, preset20msCounter)
        let timer = new Timer(typ, ts)

        let statement = Assign (rungConditionIn, ts.EN)
        statement.Do()
        StorageValueChangedSubject.OnNext(ts.EN)

        timer.InputEvaluateStatements <- [ statement ]
        timer

    let private createTimerRTO(name, rungConditionIn, resetCondition, preset20msCounter) =
        let ts = TimerRTOStruct(name, preset20msCounter)
        let timer = new TimerRTO(ts)

        let rungInStatement = Assign (rungConditionIn, ts.EN)
        rungInStatement.Do()
        StorageValueChangedSubject.OnNext(ts.EN)

        let resetStatement = Assign (resetCondition, ts.RES)
        resetStatement.Do()
        StorageValueChangedSubject.OnNext(ts.RES)

        timer.InputEvaluateStatements <- [ rungInStatement; resetStatement ]
        timer


    let CreateTON(name, rungConditionIn, target20msCounter) = createTimer(TON, name, rungConditionIn, target20msCounter)
    let CreateTOF(name, rungConditionIn, target20msCounter) = createTimer(TOF, name, rungConditionIn, target20msCounter)
    let CreateRTO(name, rungConditionIn, resetCondition, target20msCounter) = createTimerRTO(name, rungConditionIn, resetCondition, target20msCounter)
