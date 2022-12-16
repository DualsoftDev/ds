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

    let private createTimerStatement (storages:Storages) {
        Type=typ; Name=name; Preset=preset;
        RungConditionIn=rungConditionIn; ResetCondition=resetCondition;
    } : Statement =
        let ts = TimerStruct(storages, name, preset, 0us)
        let timer = new Timer(typ, ts)

        let statements = ResizeArray<Statement>()
        match rungConditionIn with
        | Some cond ->
            let rungInStatement = DuAssign (cond, ts.EN)
            rungInStatement.Do()
            statements.Add rungInStatement
        | None -> ()

        match resetCondition with
        | Some cond ->
            let resetStatement = DuAssign (cond, ts.RES)
            resetStatement.Do()
            statements.Add resetStatement
        | None -> ()

        match resetCondition with
        | Some reset ->
            let statement = DuAssign (reset, ts.RES)
            statement.Do()
            statements.Add statement
        | None -> ()

        timer.InputEvaluateStatements <- statements.ToFSharpList()
        DuTimer { Timer=timer; RungInCondition = rungConditionIn; ResetCondition = resetCondition}

    type TimerStatement =
        static member CreateTON(storages:Storages, name, preset, rungConditionIn) =
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimerStatement storages

        static member CreateTOF(storages:Storages, name, preset, rungConditionIn) =
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimerStatement storages

        static member CreateRTO(storages:Storages, name, preset, rungConditionIn) =
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimerStatement storages

        static member CreateTON(storages:Storages, name, preset, rungConditionIn, resetCondition) =
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement storages

        static member CreateTOF(storages:Storages, name, preset, rungConditionIn, resetCondition) =
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement storages
        static member CreateRTO(storages:Storages, name, preset, rungConditionIn, resetCondition) =
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement storages



