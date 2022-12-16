namespace Engine.Core

open Engine.Common.FS

[<AutoOpen>]
module TimerStatementModule =
    type internal TimerCreateParams = {
        Type: TimerType
        Name: string
        Preset: CountUnitType
        RungConditionIn: IExpression<bool> option
        ResetCondition: IExpression<bool> option
    }

    let private createTimerStatement {
        Type=typ; Name=name; Preset=preset;
        RungConditionIn=rungConditionIn; ResetCondition=resetCondition;
    } : TimerStatement =
        let ts = TimerStruct(name, preset, 0us)
        let timer = new Timer(typ, ts)

        let statements = ResizeArray<Statement>()
        match rungConditionIn with
        | Some cond ->
            let rungInStatement = Assign (cond, ts.EN)
            rungInStatement.Do()
            statements.Add rungInStatement
        | None -> ()

        match resetCondition with
        | Some cond ->
            let resetStatement = Assign (cond, ts.RES)
            resetStatement.Do()
            statements.Add resetStatement
        | None -> ()

        match resetCondition with
        | Some reset ->
            let statement = Assign (reset, ts.RES)
            statement.Do()
            statements.Add statement
        | None -> ()

        timer.InputEvaluateStatements <- statements.ToFSharpList()
        { Timer=timer; RungInCondition = rungConditionIn; ResetCondition = resetCondition}

    type TimerStatement =
        static member CreateTON(name, preset, rungConditionIn) =
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimerStatement

        static member CreateTOF(name, preset, rungConditionIn) =
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimerStatement

        static member CreateRTO(name, rungConditionIn, preset) =
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimerStatement

        static member CreateTON(name, preset, rungConditionIn, resetCondition) =
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement

        static member CreateTOF(name, preset, rungConditionIn, resetCondition) =
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement
        static member CreateRTO(name, preset, rungConditionIn, resetCondition) =
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement



