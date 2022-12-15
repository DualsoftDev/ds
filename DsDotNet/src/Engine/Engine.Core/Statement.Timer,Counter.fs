namespace Engine.Core
open System
open System.Linq
open System.Reactive.Linq
open Engine.Common.FS
open System.ComponentModel

[<AutoOpen>]
module TimerStatementModule =
    let private createTimer(typ:TimerType, name, rungConditionIn:IExpression option, resetCondition:IExpression option, preset20msCounter) =
        let ts = TimerStruct(name, preset20msCounter)
        let timer = new Timer(typ, ts)

        let statements = ResizeArray<Statement>()
        match rungConditionIn with
        | Some cond ->
            let rungInStatement = Assign (cond, ts.EN)
            rungInStatement.Do()
            statements.Add rungInStatement
            StorageValueChangedSubject.OnNext(ts.EN)
        | None -> ()

        match resetCondition with
        | Some cond ->
            let resetStatement = Assign (cond, ts.RES)
            resetStatement.Do()
            statements.Add resetStatement
            StorageValueChangedSubject.OnNext(ts.RES)
        | None -> ()

        timer.InputEvaluateStatements <- statements.ToFSharpList()
        timer

    type Timer =
        static member CreateTON(name, rungConditionIn, target20msCounter) =
            createTimer(TON, name, Some rungConditionIn, None, target20msCounter)
        static member CreateTOF(name, rungConditionIn, target20msCounter) =
            createTimer(TOF, name, Some rungConditionIn, None, target20msCounter)
        static member CreateRTO(name, rungConditionIn, target20msCounter) =
            createTimer(RTO, name, Some rungConditionIn, None, target20msCounter)

        static member CreateTON(name, rungConditionIn, resetCondition, target20msCounter) =
            createTimer(TON, name, Some rungConditionIn, Some resetCondition, target20msCounter)
        static member CreateTOF(name, rungConditionIn, resetCondition, target20msCounter) =
            createTimer(TOF, name, Some rungConditionIn, Some resetCondition, target20msCounter)
        static member CreateRTO(name, rungConditionIn, resetCondition, target20msCounter) =
            createTimer(RTO, name, Some rungConditionIn, Some resetCondition, target20msCounter)



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