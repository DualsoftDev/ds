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
        RungConditionIn=rungInCondition; ResetCondition=resetCondition;
    } : Statement =
        if preset < MinTickInterval then
            failwith <| sprintf "Timer Resolution Error: Preset value should be larger than %A" MinTickInterval

        let ts = TimerStruct.Create(typ, storages, name, preset, 0us)
        let timer = new Timer(typ, ts)

        let statements = ResizeArray<Statement>()
        match rungInCondition with
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
        DuTimer { Timer=timer; RungInCondition = rungInCondition; ResetCondition = resetCondition}


    /// Timer & Counter construction parameters
    type TCConstructionParams = {
        Storages:Storages
        Name: string
        Preset: CountUnitType
        RungInCondition:IExpression<bool>
    }

    type TimerStatement =
        static member CreateTON(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=None; }
            |> createTimerStatement storages

        static member CreateTOF(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=None; }
            |> createTimerStatement storages

        static member CreateRTO(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=None; }
            |> createTimerStatement storages

        static member CreateTON(tcParams:TCConstructionParams, resetCondition) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement storages

        static member CreateTOF(tcParams:TCConstructionParams, resetCondition) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement storages
        static member CreateRTO(tcParams:TCConstructionParams, resetCondition) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=Some resetCondition; }
            |> createTimerStatement storages



