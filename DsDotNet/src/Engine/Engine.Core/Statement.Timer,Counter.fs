namespace Engine.Core
open System
open System.Linq
open System.Reactive.Linq
open Engine.Common.FS

[<AutoOpen>]
module TimerStatementModule =
    type TimerRTO internal(timerStruct:TimerRTOStruct) =
        inherit Timer(RTO, timerStruct)
        member _.RES = timerStruct.RES

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



[<AutoOpen>]
module CounterStatementModule =

    let private createCounter(typ:CounterType, name, countUpCondition:IExpression option, countDownCondition:IExpression option, preset) =
        let cs =    // counter structure
            match typ with
            | CTU -> new CTUStruct(name, preset) :> CounterBaseStruct
            | CTD -> new CTDStruct(name, preset)
            | CTUD -> new CTUDStruct(name, preset)
        let counter = new Counter(typ, cs)

        let assignStatements =
            match countUpCondition, countDownCondition with
            | Some up, Some down ->
                [   cs.CU, Assign (up, cs.CU)
                    cs.CD, Assign (down, cs.CD) ]
            | Some up, None -> [ cs.CU, Assign (up, cs.CU) ]
            | None, Some down -> [ cs.CD, Assign (down, cs.CD) ]
            | _ -> failwith "ERROR"

        for (tag, statemnt) in assignStatements do
            statemnt.Do()
            StorageValueChangedSubject.OnNext(tag)

        counter.InputEvaluateStatements <- assignStatements.Select(snd).ToFSharpList()
        counter

    let CreateCTU(name, rungConditionIn, target20msCounter) = createCounter(CTU, name, Some rungConditionIn, None, target20msCounter)
    let CreateCTD(name, rungConditionIn, target20msCounter) = createCounter(CTD, name, None, Some rungConditionIn, target20msCounter)
    let CreateCTUD(name, countUpCondition, countDownCondition, target20msCounter) = createCounter(CTUD, name, Some countUpCondition, Some countDownCondition, target20msCounter)
    ()